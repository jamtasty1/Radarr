using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Languages;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Movies;
using NzbDrone.Core.Movies.Editions;
using NzbDrone.Core.MovieStats;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MovieStatsTests
{
    [TestFixture]
    public class MovieStatisticsFixture : DbTest<MovieStatisticsRepository, Movie>
    {
        private Movie _movie;
        private MovieFile _movieFile;

        [SetUp]
        public void Setup()
        {
            var movieMetadata = Builder<MovieMetadata>.CreateNew()
                .With(h => h.TmdbId = 123456)
                .With(m => m.Runtime = 90)
                .BuildNew();
            Db.Insert(movieMetadata);

            _movie = Builder<Movie>.CreateNew()
                .With(m => m.MovieMetadataId = movieMetadata.Id)
                .With(e => e.MovieFileId = 0)
                .With(e => e.Monitored = false)
                .BuildNew();

            _movie.Id = Db.Insert(_movie).Id;

            _movieFile = Builder<MovieFile>.CreateNew()
                .With(e => e.MovieId = _movie.Id)
                .With(e => e.Quality = new QualityModel(Quality.Bluray720p))
                .With(e => e.Languages = new List<Language> { Language.English })
                .BuildNew();
        }

        private void GivenMovieWithFile()
        {
            _movie.MovieFileId = 1;
        }

        private void GivenMonitoredMovie()
        {
            _movie.Monitored = true;
        }

        private void GivenMovieFile()
        {
            Db.Insert(_movieFile);
        }

        [Test]
        public void should_get_stats_for_movie()
        {
            GivenMonitoredMovie();

            var stats = Subject.MovieStatistics();

            stats.Should().HaveCount(1);
        }

        [Test]
        public void should_have_size_on_disk_of_zero_when_no_movie_file()
        {
            var stats = Subject.MovieStatistics();

            stats.Should().HaveCount(1);
            stats.First().SizeOnDisk.Should().Be(0);
        }

        [Test]
        public void should_count_files_of_every_edition()
        {
            GivenMovieWithFile();
            GivenMovieFile();

            var editionFile = Builder<MovieFile>.CreateNew()
                .With(e => e.Id = 0)
                .With(e => e.MovieId = _movie.Id)
                .With(e => e.MovieEditionId = 7)
                .With(e => e.Quality = new QualityModel(Quality.Bluray1080p))
                .With(e => e.Languages = new List<Language> { Language.English })
                .BuildNew();
            Db.Insert(editionFile);

            var stats = Subject.MovieStatistics();

            stats.Should().HaveCount(1);
            stats.First().MovieFileCount.Should().Be(2);
            stats.First().SizeOnDisk.Should().Be(_movieFile.Size + editionFile.Size);
        }

        [Test]
        public void should_count_edition_file_without_standard_file()
        {
            _movieFile.MovieEditionId = 7;
            GivenMovieFile();

            var stats = Subject.MovieStatistics();

            stats.First().MovieFileCount.Should().Be(1);
        }

        private void GivenStandardEdition(bool wanted, int movieFileId = 0)
        {
            _movie.StandardEditionMonitored = wanted;
            _movie.MovieFileId = movieFileId;
            Db.Update(_movie);
        }

        private void GivenEditions(int movieId, params string[] names)
        {
            foreach (var name in names)
            {
                Db.Insert(new MovieEdition { MovieId = movieId, Name = name, Monitored = true, Added = System.DateTime.UtcNow });
            }
        }

        [Test]
        public void should_count_standard_edition_and_each_extra_edition()
        {
            GivenStandardEdition(true);
            GivenEditions(_movie.Id, "Extended", "IMAX");
            GivenEditions(_movie.Id + 100, "Director's Cut");

            Subject.MovieStatistics().Single().EditionCount.Should().Be(3);
            Subject.MovieStatistics(_movie.Id).Single().EditionCount.Should().Be(3);
        }

        [Test]
        public void should_count_one_edition_for_movie_without_extra_editions()
        {
            GivenStandardEdition(true);

            Subject.MovieStatistics().Single().EditionCount.Should().Be(1);
        }

        [Test]
        public void should_not_count_unwanted_standard_edition_without_file()
        {
            GivenStandardEdition(false);
            GivenEditions(_movie.Id, "Extended");

            Subject.MovieStatistics().Single().EditionCount.Should().Be(1);
        }

        [Test]
        public void should_count_unwanted_standard_edition_with_file()
        {
            GivenStandardEdition(false, movieFileId: 1);
            GivenEditions(_movie.Id, "Extended");

            Subject.MovieStatistics().Single().EditionCount.Should().Be(2);
        }

        [Test]
        public void should_have_size_on_disk_when_movie_file_exists()
        {
            GivenMovieWithFile();
            GivenMovieFile();

            var stats = Subject.MovieStatistics();

            stats.Should().HaveCount(1);
            stats.First().SizeOnDisk.Should().Be(_movieFile.Size);
        }

        // [Test]
        // public void should_not_duplicate_size_for_multi_movie_files()
        // {
        //     GivenMovieWithFile();
        //     GivenMovieFile();
        //
        //     var movie2 = _movie.JsonClone();
        //
        //     var movieMetadata = Builder<MovieMetadata>.CreateNew().With(h => h.TmdbId = 234567).BuildNew();
        //     Db.Insert(movieMetadata);
        //
        //     movie2.Id = 0;
        //     movie2.MovieMetadataId = movieMetadata.Id;
        //
        //     Db.Insert(movie2);
        //
        //     var stats = Subject.MovieStatistics();
        //
        //     stats.Should().HaveCount(1);
        //     stats.First().SizeOnDisk.Should().Be(_movieFile.Size);
        // }
    }
}
