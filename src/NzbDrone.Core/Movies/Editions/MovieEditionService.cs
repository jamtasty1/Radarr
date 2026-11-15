using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Movies.Events;

namespace NzbDrone.Core.Movies.Editions
{
    public interface IMovieEditionService
    {
        MovieEdition Get(int id);
        List<MovieEdition> GetByMovieId(int movieId);
        bool HasEditions(int movieId);
        MovieEdition Add(MovieEdition edition);
        MovieEdition Update(MovieEdition edition);
        void Delete(int id);

        // Works out which slot (standard or tracked edition) an edition string belongs to for a movie.
        EditionSlot Resolve(Movie movie, string edition);
        EditionSlot ResolveForTitle(Movie movie, string releaseTitle);
        EditionSlot GetSlot(Movie movie, int movieEditionId);

        // Slot id only (no file lookup): 0 = standard, -1 = an edition the movie doesn't track.
        int ResolveSlotIdForTitle(Movie movie, string releaseTitle);

        // Moves an existing file into a different slot of its movie.
        MovieFile AssignFile(MovieFile movieFile, int movieEditionId);
    }

    public class MovieEditionService : IMovieEditionService, IHandle<MoviesDeletedEvent>
    {
        private const string CacheKey = "all";

        private readonly IMovieEditionRepository _repository;
        private readonly IMediaFileService _mediaFileService;
        private readonly IMovieService _movieService;
        private readonly ICached<Dictionary<int, List<MovieEdition>>> _cache;
        private readonly Logger _logger;

        public MovieEditionService(IMovieEditionRepository repository,
                                   IMediaFileService mediaFileService,
                                   IMovieService movieService,
                                   ICacheManager cacheManager,
                                   Logger logger)
        {
            _repository = repository;
            _mediaFileService = mediaFileService;
            _movieService = movieService;
            _cache = cacheManager.GetCache<Dictionary<int, List<MovieEdition>>>(GetType());
            _logger = logger;
        }

        public MovieEdition Get(int id)
        {
            return _repository.Get(id);
        }

        public List<MovieEdition> GetByMovieId(int movieId)
        {
            var all = _cache.Get(CacheKey, () => _repository.All()
                .GroupBy(e => e.MovieId)
                .ToDictionary(g => g.Key, g => g.OrderBy(e => e.Id).ToList()));

            return all.TryGetValue(movieId, out var editions) ? editions.ToList() : new List<MovieEdition>();
        }

        public bool HasEditions(int movieId)
        {
            return GetByMovieId(movieId).Any();
        }

        public MovieEdition Add(MovieEdition edition)
        {
            var movie = _movieService.GetMovie(edition.MovieId);

            edition.Name = edition.Name?.Trim();
            Validate(edition);

            edition.Added = DateTime.UtcNow;
            edition = _repository.Insert(edition);
            _cache.Clear();

            _logger.Info("Added edition '{0}' to movie {1}", edition.Name, movie);

            AdoptMatchingStandardFiles(movie, edition);

            return edition;
        }

        public MovieEdition Update(MovieEdition edition)
        {
            var existing = _repository.Get(edition.Id);

            edition.MovieId = existing.MovieId;
            edition.Added = existing.Added;
            edition.Name = edition.Name?.Trim();
            Validate(edition);

            edition = _repository.Update(edition);
            _cache.Clear();

            return edition;
        }

        public void Delete(int id)
        {
            var edition = _repository.Get(id);
            var file = _mediaFileService.GetFilesByMovie(edition.MovieId).FirstOrDefault(f => f.MovieEditionId == id);

            if (file != null)
            {
                throw new NzbDroneClientException(HttpStatusCode.Conflict,
                    "Edition '{0}' still has a file ({1}). Delete the file or move it to another edition first.",
                    edition.Name,
                    file.RelativePath);
            }

            _repository.Delete(id);
            _cache.Clear();

            _logger.Info("Removed edition '{0}' from movie {1}", edition.Name, edition.MovieId);
        }

        public EditionSlot Resolve(Movie movie, string edition)
        {
            var editions = GetByMovieId(movie.Id);

            if (editions.Empty() || EditionNormalizer.IsStandard(edition))
            {
                return EditionSlot.Standard(movie, GetStandardFile(movie));
            }

            var match = EditionNormalizer.FindMatch(editions, edition);

            if (match == null)
            {
                return EditionSlot.Untracked(edition);
            }

            return BuildSlot(movie, match);
        }

        public EditionSlot ResolveForTitle(Movie movie, string releaseTitle)
        {
            var edition = releaseTitle.IsNullOrWhiteSpace() ? null : Parser.Parser.ParseEdition(releaseTitle);

            return Resolve(movie, edition);
        }

        public int ResolveSlotIdForTitle(Movie movie, string releaseTitle)
        {
            var editions = GetByMovieId(movie.Id);

            if (editions.Empty() || releaseTitle.IsNullOrWhiteSpace())
            {
                return EditionSlot.StandardEditionId;
            }

            var edition = Parser.Parser.ParseEdition(releaseTitle);

            if (EditionNormalizer.IsStandard(edition))
            {
                return EditionSlot.StandardEditionId;
            }

            return EditionNormalizer.FindMatch(editions, edition)?.Id ?? -1;
        }

        public EditionSlot GetSlot(Movie movie, int movieEditionId)
        {
            if (movieEditionId == EditionSlot.StandardEditionId)
            {
                return EditionSlot.Standard(movie, GetStandardFile(movie));
            }

            var edition = GetByMovieId(movie.Id).FirstOrDefault(e => e.Id == movieEditionId);

            if (edition == null)
            {
                throw new NzbDroneClientException(HttpStatusCode.BadRequest, "Edition {0} does not belong to movie {1}", movieEditionId, movie.Id);
            }

            return BuildSlot(movie, edition);
        }

        public MovieFile AssignFile(MovieFile movieFile, int movieEditionId)
        {
            if (movieFile.MovieEditionId == movieEditionId)
            {
                return movieFile;
            }

            var movie = _movieService.GetMovie(movieFile.MovieId);
            var target = GetSlot(movie, movieEditionId);

            if (target.MovieFile != null && target.MovieFile.Id != movieFile.Id)
            {
                throw new NzbDroneClientException(HttpStatusCode.Conflict,
                    "Edition '{0}' already has a file ({1})",
                    target.Name,
                    target.MovieFile.RelativePath);
            }

            var wasStandard = movieFile.MovieEditionId == EditionSlot.StandardEditionId;

            // Keep the file's edition text in line with its slot so naming and later matching agree.
            if (target.IsStandard)
            {
                if (!EditionNormalizer.IsStandard(movieFile.Edition))
                {
                    movieFile.Edition = null;
                }
            }
            else if (EditionNormalizer.FindMatch(new[] { new MovieEdition { Name = target.Name } }, movieFile.Edition) == null)
            {
                movieFile.Edition = target.Name;
            }

            movieFile.MovieEditionId = movieEditionId;
            _mediaFileService.Update(movieFile);

            if (target.IsStandard)
            {
                movie.MovieFileId = movieFile.Id;
                _movieService.UpdateMovie(movie);
            }
            else if (wasStandard && movie.MovieFileId == movieFile.Id)
            {
                movie.MovieFileId = 0;
                _movieService.UpdateMovie(movie);
            }

            _logger.Info("Assigned file {0} to edition '{1}' of movie {2}", movieFile.RelativePath, target.Name, movie);

            return movieFile;
        }

        public void Handle(MoviesDeletedEvent message)
        {
            _repository.DeleteForMovies(message.Movies.Select(m => m.Id).ToList());
            _cache.Clear();
        }

        private void Validate(MovieEdition edition)
        {
            if (edition.Name.IsNullOrWhiteSpace())
            {
                throw new NzbDroneClientException(HttpStatusCode.BadRequest, "Edition name is required");
            }

            if (EditionNormalizer.IsStandard(edition.Name))
            {
                throw new NzbDroneClientException(HttpStatusCode.BadRequest,
                    "'{0}' is the standard edition, which every movie already tracks",
                    edition.Name);
            }

            var duplicate = GetByMovieId(edition.MovieId)
                .FirstOrDefault(e => e.Id != edition.Id && e.Key == edition.Key);

            if (duplicate != null)
            {
                throw new NzbDroneClientException(HttpStatusCode.Conflict,
                    "Movie already tracks edition '{0}'",
                    duplicate.Name);
            }
        }

        private EditionSlot BuildSlot(Movie movie, MovieEdition edition)
        {
            // Newest wins if a rescan briefly left two files in one slot (housekeeping removes the older row).
            var file = _mediaFileService.GetFilesByMovie(movie.Id)
                .Where(f => f.MovieEditionId == edition.Id)
                .MaxBy(f => f.Id);

            return new EditionSlot
            {
                MovieEditionId = edition.Id,
                Name = edition.Name,
                Tracked = true,
                Monitored = movie.Monitored && edition.Monitored,
                MovieFile = file
            };
        }

        // Movie.MovieFileId always points at the standard edition's file.
        private MovieFile GetStandardFile(Movie movie)
        {
            if (movie.MovieFileId == 0)
            {
                return null;
            }

            // Fall back to a lookup when the movie was loaded without its file joined.
            return movie.MovieFile ?? _mediaFileService.GetMovies(new[] { movie.MovieFileId }).FirstOrDefault();
        }

        // When an edition is added, files already sitting in the standard slot that were
        // parsed as that edition move into it (e.g. an existing "Extended" file).
        private void AdoptMatchingStandardFiles(Movie movie, MovieEdition edition)
        {
            var candidate = _mediaFileService.GetFilesByMovie(movie.Id)
                .Where(f => f.MovieEditionId == EditionSlot.StandardEditionId)
                .FirstOrDefault(f => EditionNormalizer.FindMatch(new[] { edition }, f.Edition) != null);

            if (candidate != null)
            {
                AssignFile(candidate, edition.Id);
            }
        }
    }
}
