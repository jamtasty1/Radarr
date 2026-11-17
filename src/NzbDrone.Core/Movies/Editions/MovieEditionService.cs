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
using NzbDrone.Core.Profiles.Qualities;

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
        // Titles (release, folder or file names) are searched for tracked edition names the parser
        // doesn't recognise when the edition string alone doesn't name a tracked edition.
        EditionSlot Resolve(Movie movie, string edition, params string[] titles);
        EditionSlot ResolveForTitle(Movie movie, string releaseTitle);
        EditionSlot GetSlot(Movie movie, int movieEditionId);

        // The standard edition followed by each tracked edition, with their files and profiles.
        List<EditionSlot> GetSlots(Movie movie);

        // The profile files in an edition are judged by (the edition's own, else the movie's).
        QualityProfile GetQualityProfile(Movie movie, int movieEditionId);

        // Slot id only (no file lookup): 0 = standard, -1 = an edition the movie doesn't track.
        int ResolveSlotIdForTitle(Movie movie, string releaseTitle);

        // Moves an existing file into a different slot of its movie.
        MovieFile AssignFile(MovieFile movieFile, int movieEditionId);

        // Names the standard edition for the cut the movie's main copy is (e.g. "Extended"); blank resets it.
        void RenameStandardEdition(int movieId, string name);
    }

    public class MovieEditionService : IMovieEditionService, IHandle<MoviesDeletedEvent>
    {
        private const string CacheKey = "all";

        private readonly IMovieEditionRepository _repository;
        private readonly IMediaFileService _mediaFileService;
        private readonly IMovieService _movieService;
        private readonly IQualityProfileService _qualityProfileService;
        private readonly IEventAggregator _eventAggregator;
        private readonly ICached<Dictionary<int, List<MovieEdition>>> _cache;
        private readonly Logger _logger;

        public MovieEditionService(IMovieEditionRepository repository,
                                   IMediaFileService mediaFileService,
                                   IMovieService movieService,
                                   IQualityProfileService qualityProfileService,
                                   IEventAggregator eventAggregator,
                                   ICacheManager cacheManager,
                                   Logger logger)
        {
            _repository = repository;
            _mediaFileService = mediaFileService;
            _movieService = movieService;
            _qualityProfileService = qualityProfileService;
            _eventAggregator = eventAggregator;
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
            Validate(edition, movie);

            edition.Added = DateTime.UtcNow;
            edition = _repository.Insert(edition);
            _cache.Clear();

            _logger.Info("Added edition '{0}' to movie {1}", edition.Name, movie);

            AdoptMatchingStandardFiles(movie, edition);

            _eventAggregator.PublishEvent(new MovieEditionsChangedEvent(edition.MovieId));

            return edition;
        }

        public MovieEdition Update(MovieEdition edition)
        {
            var existing = _repository.Get(edition.Id);

            edition.MovieId = existing.MovieId;
            edition.Added = existing.Added;
            edition.Name = edition.Name?.Trim();
            Validate(edition, _movieService.GetMovie(edition.MovieId));

            edition = _repository.Update(edition);
            _cache.Clear();

            _eventAggregator.PublishEvent(new MovieEditionsChangedEvent(edition.MovieId));

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

            _eventAggregator.PublishEvent(new MovieEditionsChangedEvent(edition.MovieId));
        }

        public EditionSlot Resolve(Movie movie, string edition, params string[] titles)
        {
            var editions = GetByMovieId(movie.Id);
            var id = ResolveEditionId(movie, editions, edition, titles);

            if (id == EditionSlot.StandardEditionId)
            {
                return BuildStandardSlot(movie);
            }

            return id > 0 ? BuildSlot(movie, editions.First(e => e.Id == id)) : EditionSlot.Untracked(edition);
        }

        public EditionSlot ResolveForTitle(Movie movie, string releaseTitle)
        {
            var edition = releaseTitle.IsNullOrWhiteSpace() ? null : Parser.Parser.ParseEdition(releaseTitle);

            return Resolve(movie, edition, releaseTitle);
        }

        public int ResolveSlotIdForTitle(Movie movie, string releaseTitle)
        {
            var edition = releaseTitle.IsNullOrWhiteSpace() ? null : Parser.Parser.ParseEdition(releaseTitle);

            return ResolveEditionId(movie, GetByMovieId(movie.Id), edition, new[] { releaseTitle });
        }

        public void RenameStandardEdition(int movieId, string name)
        {
            var movie = _movieService.GetMovie(movieId);

            name = name?.Trim();

            if (name.IsNullOrWhiteSpace() || name.Equals(EditionSlot.StandardEditionName, StringComparison.OrdinalIgnoreCase))
            {
                name = null;
            }

            var editions = GetByMovieId(movieId);

            if (EditionNormalizer.IsStandard(name))
            {
                // Untagged releases can only belong to one edition.
                var standardNamed = editions.FirstOrDefault(e => EditionNormalizer.IsStandard(e.Name));

                if (standardNamed != null)
                {
                    throw new NzbDroneClientException(HttpStatusCode.Conflict,
                        "'{0}' is tracked as its own edition, so the standard edition needs the name of a cut such as Extended. Remove '{0}' first.",
                        standardNamed.Name);
                }
            }
            else
            {
                var duplicate = editions.FirstOrDefault(e => e.Key == EditionNormalizer.Normalize(name));

                if (duplicate != null)
                {
                    throw new NzbDroneClientException(HttpStatusCode.Conflict,
                        "Movie already tracks edition '{0}'",
                        duplicate.Name);
                }
            }

            movie.StandardEditionName = name;
            _movieService.UpdateMovie(movie);

            // Keep the standard file's edition text in line with its cut so naming and later matching agree.
            var file = GetStandardFile(movie);

            if (file != null && !EditionNormalizer.IsStandard(name) &&
                EditionNormalizer.FindMatch(new[] { new MovieEdition { Name = name } }, file.Edition) == null)
            {
                file.Edition = name;
                _mediaFileService.Update(file);
            }

            _logger.Info("Named the standard edition of {0} '{1}'", movie, EditionSlot.GetStandardName(movie));

            _eventAggregator.PublishEvent(new MovieEditionsChangedEvent(movieId));
        }

        public EditionSlot GetSlot(Movie movie, int movieEditionId)
        {
            if (movieEditionId == EditionSlot.StandardEditionId)
            {
                return BuildStandardSlot(movie);
            }

            var edition = GetByMovieId(movie.Id).FirstOrDefault(e => e.Id == movieEditionId);

            if (edition == null)
            {
                throw new NzbDroneClientException(HttpStatusCode.BadRequest, "Edition {0} does not belong to movie {1}", movieEditionId, movie.Id);
            }

            return BuildSlot(movie, edition);
        }

        public List<EditionSlot> GetSlots(Movie movie)
        {
            var slots = new List<EditionSlot> { BuildStandardSlot(movie) };

            slots.AddRange(GetByMovieId(movie.Id).Select(e => BuildSlot(movie, e)));

            return slots;
        }

        public QualityProfile GetQualityProfile(Movie movie, int movieEditionId)
        {
            var edition = movieEditionId == EditionSlot.StandardEditionId
                ? null
                : GetByMovieId(movie.Id).FirstOrDefault(e => e.Id == movieEditionId);

            return edition == null ? GetMovieQualityProfile(movie) : GetQualityProfile(movie, edition);
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
            if (target.IsStandard && !target.IsNamedCut)
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

        private void Validate(MovieEdition edition, Movie movie)
        {
            if (edition.Name.IsNullOrWhiteSpace())
            {
                throw new NzbDroneClientException(HttpStatusCode.BadRequest, "Edition name is required");
            }

            // Only once the standard edition is named for a cut can untagged/theatrical releases have an edition of their own.
            if (EditionNormalizer.IsStandard(edition.Name) && !EditionSlot.HasNamedStandard(movie))
            {
                throw new NzbDroneClientException(HttpStatusCode.BadRequest,
                    "'{0}' is the standard edition, which every movie already tracks. Rename the standard edition to the cut it is (e.g. Extended) to track '{0}' separately.",
                    edition.Name);
            }

            if (EditionSlot.HasNamedStandard(movie) && edition.Key == EditionNormalizer.Normalize(movie.StandardEditionName))
            {
                throw new NzbDroneClientException(HttpStatusCode.Conflict,
                    "The standard edition is already '{0}'",
                    movie.StandardEditionName);
            }

            if (edition.QualityProfileId < 0 || (edition.QualityProfileId > 0 && !_qualityProfileService.Exists(edition.QualityProfileId)))
            {
                throw new NzbDroneClientException(HttpStatusCode.BadRequest, "Quality profile {0} does not exist", edition.QualityProfileId);
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

        // 0 = standard, an edition's id, or -1 for an edition the movie doesn't track.
        private static int ResolveEditionId(Movie movie, List<MovieEdition> editions, string edition, IEnumerable<string> titles)
        {
            var namedStandard = EditionSlot.HasNamedStandard(movie);

            if (editions.Empty() && !namedStandard)
            {
                return EditionSlot.StandardEditionId;
            }

            // A standard edition named for a cut is matched like any other edition.
            var candidates = namedStandard
                ? editions.Append(new MovieEdition { Id = EditionSlot.StandardEditionId, Name = movie.StandardEditionName }).ToList()
                : editions;

            var match = FindEdition(movie, candidates, edition, titles);

            if (match != null)
            {
                return match.Id;
            }

            if (!EditionNormalizer.IsStandard(edition))
            {
                return -1;
            }

            if (!namedStandard)
            {
                return EditionSlot.StandardEditionId;
            }

            // The standard edition is a particular cut, so untagged/theatrical releases only belong
            // to an edition tracked for them (e.g. "Theatrical").
            return editions.FirstOrDefault(e => EditionNormalizer.IsStandard(e.Name))?.Id ?? -1;
        }

        // The parsed edition text decides first; failing that, a tracked edition named in one of the
        // titles (the first title that names one wins), which covers names the parser doesn't know.
        private static MovieEdition FindEdition(Movie movie, List<MovieEdition> editions, string edition, IEnumerable<string> titles)
        {
            var match = EditionNormalizer.FindMatch(editions, edition);

            if (match != null || titles == null)
            {
                return match;
            }

            var metadata = movie.MovieMetadata?.Value;
            var movieTitles = new[] { metadata?.Title, metadata?.OriginalTitle }.Where(t => t.IsNotNullOrWhiteSpace()).ToList();

            return titles.Where(t => t.IsNotNullOrWhiteSpace())
                .Select(t => EditionNormalizer.FindInTitle(editions, t, movieTitles))
                .FirstOrDefault(m => m != null);
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
                EditionMonitored = edition.Monitored,
                MovieFile = file,
                QualityProfile = GetQualityProfile(movie, edition)
            };
        }

        private EditionSlot BuildStandardSlot(Movie movie)
        {
            var slot = EditionSlot.Standard(movie, GetStandardFile(movie));
            slot.QualityProfile = GetMovieQualityProfile(movie);

            return slot;
        }

        // An edition uses its own profile when it has one, otherwise the movie's. A profile that
        // has since been removed falls back to the movie's too.
        private QualityProfile GetQualityProfile(Movie movie, MovieEdition edition)
        {
            if (edition.QualityProfileId > 0 && edition.QualityProfileId != movie.QualityProfileId && _qualityProfileService.Exists(edition.QualityProfileId))
            {
                return _qualityProfileService.Get(edition.QualityProfileId);
            }

            return GetMovieQualityProfile(movie);
        }

        private QualityProfile GetMovieQualityProfile(Movie movie)
        {
            if (movie.QualityProfile != null || movie.QualityProfileId == 0)
            {
                return movie.QualityProfile;
            }

            return _qualityProfileService.Exists(movie.QualityProfileId) ? _qualityProfileService.Get(movie.QualityProfileId) : null;
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
