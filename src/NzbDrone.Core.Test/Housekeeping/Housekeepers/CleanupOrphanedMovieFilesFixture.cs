using System;
using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Housekeeping.Housekeepers;
using NzbDrone.Core.Languages;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Movies;
using NzbDrone.Core.Movies.Editions;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Housekeeping.Housekeepers
{
    [TestFixture]
    public class CleanupOrphanedMovieFilesFixture : DbTest<CleanupOrphanedMovieFiles, MovieFile>
    {
        private MovieFile BuildFile(int movieId, int movieEditionId)
        {
            return Builder<MovieFile>.CreateNew()
                                     .With(h => h.MovieId = movieId)
                                     .With(h => h.MovieEditionId = movieEditionId)
                                     .With(h => h.RelativePath = Guid.NewGuid().ToString())
                                     .With(h => h.Quality = new QualityModel())
                                     .With(h => h.Languages = new List<Language> { Language.English })
                                     .BuildNew();
        }

        private Movie GivenMovie(int movieFileId = 0)
        {
            var movie = Builder<Movie>.CreateNew()
                                      .With(e => e.MovieFileId = movieFileId)
                                      .BuildNew();

            Db.Insert(movie);

            return movie;
        }

        private MovieEdition GivenEdition(int movieId)
        {
            var edition = new MovieEdition { MovieId = movieId, Name = "Extended", Monitored = true, Added = DateTime.UtcNow };

            Db.Insert(edition);

            return edition;
        }

        [Test]
        public void should_delete_orphaned_episode_files()
        {
            var movieFile = BuildFile(1, 0);

            Db.Insert(movieFile);
            Subject.Clean();
            AllStoredModels.Should().BeEmpty();
        }

        [Test]
        public void should_not_delete_unorphaned_movie_files()
        {
            var movieFiles = new List<MovieFile> { BuildFile(1, 0), BuildFile(1, 0) };

            Db.InsertMany(movieFiles);

            GivenMovie(movieFiles.First().Id);

            Subject.Clean();
            AllStoredModels.Should().HaveCount(1);
            Db.All<Movie>().Should().Contain(e => e.MovieFileId == AllStoredModels.First().Id);
        }

        [Test]
        public void should_keep_edition_files_alongside_standard_file()
        {
            var standardFile = Db.Insert(BuildFile(1, 0));
            var movie = GivenMovie(standardFile.Id);

            var edition = GivenEdition(movie.Id);
            var extendedFile = BuildFile(movie.Id, edition.Id);
            Db.Insert(extendedFile);

            standardFile.MovieId = movie.Id;
            Db.Update(standardFile);

            Subject.Clean();

            AllStoredModels.Select(f => f.Id).Should().BeEquivalentTo(new[] { standardFile.Id, extendedFile.Id });
        }

        [Test]
        public void should_delete_edition_file_whose_edition_no_longer_exists()
        {
            var movie = GivenMovie();
            Db.Insert(BuildFile(movie.Id, 999));

            Subject.Clean();

            AllStoredModels.Should().BeEmpty();
        }

        [Test]
        public void should_delete_edition_file_whose_movie_no_longer_exists()
        {
            var edition = GivenEdition(12345);
            Db.Insert(BuildFile(12345, edition.Id));

            Subject.Clean();

            AllStoredModels.Should().BeEmpty();
        }

        [Test]
        public void should_keep_only_newest_file_in_an_edition()
        {
            var movie = GivenMovie();
            var edition = GivenEdition(movie.Id);

            var older = Db.Insert(BuildFile(movie.Id, edition.Id));
            var newer = Db.Insert(BuildFile(movie.Id, edition.Id));

            Subject.Clean();

            AllStoredModels.Should().ContainSingle(f => f.Id == newer.Id);
            AllStoredModels.Should().NotContain(f => f.Id == older.Id);
        }
    }
}
