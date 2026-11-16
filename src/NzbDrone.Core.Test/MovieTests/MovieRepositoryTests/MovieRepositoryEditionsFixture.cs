using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Languages;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Movies;
using NzbDrone.Core.Movies.Editions;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.CustomFormats;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MovieTests.MovieRepositoryTests
{
    [TestFixture]
    public class MovieRepositoryEditionsFixture : DbTest<MovieRepository, Movie>
    {
        private QualityProfile _profile;
        private QualityProfile _hdtvProfile;
        private Movie _movie;

        [SetUp]
        public void Setup()
        {
            var profileRepository = Mocker.Resolve<QualityProfileRepository>();
            Mocker.SetConstant<IQualityProfileRepository>(profileRepository);

            Mocker.GetMock<ICustomFormatService>()
                .Setup(x => x.All())
                .Returns(new List<CustomFormat>());

            _profile = profileRepository.Insert(BuildProfile("Bluray", Quality.Bluray1080p));
            _hdtvProfile = profileRepository.Insert(BuildProfile("HDTV", Quality.HDTV720p));

            var metadata = Builder<MovieMetadata>.CreateNew()
                .With(m => m.Id = 0)
                .With(m => m.Year = 2001)
                .BuildNew();

            Db.Insert(metadata);

            _movie = Builder<Movie>.CreateNew()
                .With(m => m.Id = 0)
                .With(m => m.MovieMetadataId = metadata.Id)
                .With(m => m.QualityProfileId = _profile.Id)
                .With(m => m.Monitored = true)
                .With(m => m.StandardEditionMonitored = true)
                .With(m => m.MovieFileId = 0)
                .BuildNew();

            Subject.Insert(_movie);
        }

        private static QualityProfile BuildProfile(string name, Quality cutoff)
        {
            return new QualityProfile
            {
                Name = name,
                Items = Qualities.QualityFixture.GetDefaultQualities(Quality.HDTV720p, Quality.Bluray1080p),
                FormatItems = CustomFormatsTestHelpers.GetDefaultFormatItems(),
                MinFormatScore = 0,
                Cutoff = cutoff.Id,
                UpgradeAllowed = true
            };
        }

        private MovieFile GivenFile(int movieEditionId, Quality quality)
        {
            var file = Builder<MovieFile>.CreateNew()
                .With(f => f.Id = 0)
                .With(f => f.MovieId = _movie.Id)
                .With(f => f.MovieEditionId = movieEditionId)
                .With(f => f.Quality = new QualityModel(quality))
                .With(f => f.Languages = new List<Language> { Language.English })
                .BuildNew();

            Db.Insert(file);

            if (movieEditionId == EditionSlot.StandardEditionId)
            {
                _movie.MovieFileId = file.Id;
                Subject.Update(_movie);
            }

            return file;
        }

        private MovieEdition GivenEdition(bool monitored, int qualityProfileId = 0)
        {
            var edition = new MovieEdition
            {
                MovieId = _movie.Id,
                Name = "Extended",
                Monitored = monitored,
                QualityProfileId = qualityProfileId,
                Added = System.DateTime.UtcNow
            };

            Db.Insert(edition);

            return edition;
        }

        private List<Movie> Missing()
        {
            var spec = new PagingSpec<Movie> { Page = 1, PageSize = 10 };
            spec.FilterExpressions.Add(m => m.Monitored == true);

            return Subject.MoviesWithoutFiles(spec).Records;
        }

        private List<Movie> CutoffUnmet()
        {
            var spec = new PagingSpec<Movie> { Page = 1, PageSize = 10 };
            spec.FilterExpressions.Add(m => m.Monitored == true);

            var belowCutoff = new List<QualitiesBelowCutoff>
            {
                new QualitiesBelowCutoff(_profile.Id, new[] { Quality.HDTV720p.Id })
            };

            return Subject.MoviesWhereCutoffUnmet(spec, belowCutoff).Records;
        }

        [Test]
        public void should_be_missing_without_standard_file()
        {
            Missing().Should().ContainSingle();
        }

        [Test]
        public void should_not_be_missing_when_standard_file_exists_and_no_editions()
        {
            GivenFile(EditionSlot.StandardEditionId, Quality.Bluray1080p);

            Missing().Should().BeEmpty();
        }

        [Test]
        public void should_be_missing_when_monitored_edition_has_no_file()
        {
            GivenFile(EditionSlot.StandardEditionId, Quality.Bluray1080p);
            GivenEdition(monitored: true);

            Missing().Should().ContainSingle();
        }

        [Test]
        public void should_not_be_missing_when_every_edition_has_a_file()
        {
            GivenFile(EditionSlot.StandardEditionId, Quality.Bluray1080p);
            var edition = GivenEdition(monitored: true);
            GivenFile(edition.Id, Quality.Bluray1080p);

            Missing().Should().BeEmpty();
        }

        [Test]
        public void should_not_be_missing_when_only_unmonitored_edition_lacks_file()
        {
            GivenFile(EditionSlot.StandardEditionId, Quality.Bluray1080p);
            GivenEdition(monitored: false);

            Missing().Should().BeEmpty();
        }

        [Test]
        public void should_not_be_missing_when_standard_edition_is_not_wanted()
        {
            _movie.StandardEditionMonitored = false;
            Subject.Update(_movie);

            var edition = GivenEdition(monitored: true);
            GivenFile(edition.Id, Quality.Bluray1080p);

            Missing().Should().BeEmpty();
        }

        [Test]
        public void should_be_cutoff_unmet_when_edition_file_is_below_cutoff()
        {
            GivenFile(EditionSlot.StandardEditionId, Quality.Bluray1080p);
            var edition = GivenEdition(monitored: true);
            GivenFile(edition.Id, Quality.HDTV720p);

            CutoffUnmet().Should().ContainSingle();
        }

        [Test]
        public void should_not_be_cutoff_unmet_when_edition_profile_cutoff_is_met()
        {
            GivenFile(EditionSlot.StandardEditionId, Quality.Bluray1080p);
            var edition = GivenEdition(monitored: true, qualityProfileId: _hdtvProfile.Id);
            GivenFile(edition.Id, Quality.HDTV720p);

            CutoffUnmet().Should().BeEmpty();
        }

        [Test]
        public void should_be_cutoff_unmet_for_standard_file_below_cutoff()
        {
            GivenFile(EditionSlot.StandardEditionId, Quality.HDTV720p);

            CutoffUnmet().Should().ContainSingle();
        }

        [Test]
        public void should_not_be_cutoff_unmet_for_unwanted_standard_edition()
        {
            _movie.StandardEditionMonitored = false;
            Subject.Update(_movie);
            GivenFile(EditionSlot.StandardEditionId, Quality.HDTV720p);

            CutoffUnmet().Should().BeEmpty();
        }
    }
}
