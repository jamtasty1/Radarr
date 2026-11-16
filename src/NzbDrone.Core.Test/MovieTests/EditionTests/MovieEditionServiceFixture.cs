using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Cache;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Movies;
using NzbDrone.Core.Movies.Editions;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MovieTests.EditionTests
{
    [TestFixture]
    public class MovieEditionServiceFixture : CoreTest<MovieEditionService>
    {
        private Movie _movie;
        private MovieFile _standardFile;
        private List<MovieEdition> _editions;
        private List<MovieFile> _files;

        [SetUp]
        public void Setup()
        {
            Mocker.SetConstant<ICacheManager>(Mocker.Resolve<CacheManager>());

            _standardFile = new MovieFile { Id = 10, MovieId = 1, MovieEditionId = 0, RelativePath = "Movie (2000).mkv" };

            _movie = new Movie
            {
                Id = 1,
                Monitored = true,
                MovieFileId = _standardFile.Id,
                MovieFile = _standardFile
            };

            _editions = new List<MovieEdition>();
            _files = new List<MovieFile> { _standardFile };

            Mocker.GetMock<IMovieEditionRepository>()
                  .Setup(s => s.All())
                  .Returns(() => _editions);

            Mocker.GetMock<IMovieEditionRepository>()
                  .Setup(s => s.Get(It.IsAny<int>()))
                  .Returns<int>(id => _editions.Single(e => e.Id == id));

            Mocker.GetMock<IMovieEditionRepository>()
                  .Setup(s => s.Insert(It.IsAny<MovieEdition>()))
                  .Returns<MovieEdition>(e =>
                  {
                      e.Id = _editions.Count + 100;
                      _editions.Add(e);
                      return e;
                  });

            Mocker.GetMock<IMediaFileService>()
                  .Setup(s => s.GetFilesByMovie(_movie.Id))
                  .Returns(() => _files);

            Mocker.GetMock<IMovieService>()
                  .Setup(s => s.GetMovie(_movie.Id))
                  .Returns(_movie);
        }

        private MovieEdition GivenEdition(int id, string name, bool monitored = true)
        {
            var edition = new MovieEdition { Id = id, MovieId = _movie.Id, Name = name, Monitored = monitored };
            _editions.Add(edition);

            return edition;
        }

        private MovieFile GivenEditionFile(int id, int editionId, string edition)
        {
            var file = new MovieFile { Id = id, MovieId = _movie.Id, MovieEditionId = editionId, Edition = edition };
            _files.Add(file);

            return file;
        }

        [TestCase(null)]
        [TestCase("Extended")]
        [TestCase("Director's Cut")]
        public void should_resolve_everything_to_standard_slot_when_movie_has_no_editions(string edition)
        {
            var slot = Subject.Resolve(_movie, edition);

            slot.IsStandard.Should().BeTrue();
            slot.Tracked.Should().BeTrue();
            slot.MovieFile.Should().Be(_standardFile);
        }

        [Test]
        public void should_resolve_untagged_release_to_standard_slot()
        {
            GivenEdition(1, "Extended");

            var slot = Subject.Resolve(_movie, null);

            slot.IsStandard.Should().BeTrue();
            slot.MovieFile.Should().Be(_standardFile);
        }

        [Test]
        public void should_resolve_tracked_edition_with_its_own_file()
        {
            GivenEdition(1, "Extended");
            var extendedFile = GivenEditionFile(11, 1, "Extended");

            var slot = Subject.Resolve(_movie, "Extended Edition");

            slot.MovieEditionId.Should().Be(1);
            slot.Tracked.Should().BeTrue();
            slot.MovieFile.Should().Be(extendedFile);
        }

        [Test]
        public void should_resolve_tracked_edition_without_file_as_empty()
        {
            GivenEdition(1, "Extended");

            var slot = Subject.Resolve(_movie, "Extended");

            slot.MovieEditionId.Should().Be(1);
            slot.MovieFile.Should().BeNull();
        }

        [Test]
        public void should_resolve_untracked_edition_when_movie_tracks_editions()
        {
            GivenEdition(1, "Extended");

            var slot = Subject.Resolve(_movie, "IMAX");

            slot.Tracked.Should().BeFalse();
        }

        [Test]
        public void should_not_be_monitored_when_edition_is_unmonitored()
        {
            GivenEdition(1, "Extended", monitored: false);

            Subject.Resolve(_movie, "Extended").Monitored.Should().BeFalse();
        }

        [Test]
        public void should_resolve_slot_id_from_release_title()
        {
            GivenEdition(1, "Director's Cut");

            Subject.ResolveSlotIdForTitle(_movie, "Movie.2000.Directors.Cut.1080p.BluRay.x264-GRP").Should().Be(1);
            Subject.ResolveSlotIdForTitle(_movie, "Movie.2000.1080p.BluRay.x264-GRP").Should().Be(0);
            Subject.ResolveSlotIdForTitle(_movie, "Movie.2000.IMAX.1080p.BluRay.x264-GRP").Should().Be(-1);
        }

        [Test]
        public void should_reject_standard_edition_name()
        {
            Assert.Throws<NzbDroneClientException>(() => Subject.Add(new MovieEdition { MovieId = _movie.Id, Name = "Theatrical Cut" }));
        }

        [Test]
        public void should_reject_unknown_quality_profile()
        {
            Assert.Throws<NzbDroneClientException>(() => Subject.Add(new MovieEdition { MovieId = _movie.Id, Name = "Extended", QualityProfileId = 99 }));
        }

        [Test]
        public void should_use_edition_quality_profile_for_its_slot()
        {
            var movieProfile = new QualityProfile { Id = 1, Name = "Movie" };
            var editionProfile = new QualityProfile { Id = 2, Name = "4K" };
            _movie.QualityProfileId = 1;
            _movie.QualityProfile = movieProfile;

            Mocker.GetMock<IQualityProfileService>().Setup(s => s.Exists(2)).Returns(true);
            Mocker.GetMock<IQualityProfileService>().Setup(s => s.Get(2)).Returns(editionProfile);

            var edition = GivenEdition(1, "IMAX");
            edition.QualityProfileId = 2;
            GivenEdition(2, "Extended");

            Subject.Resolve(_movie, "IMAX").QualityProfile.Should().Be(editionProfile);
            Subject.Resolve(_movie, "Extended").QualityProfile.Should().Be(movieProfile);
            Subject.Resolve(_movie, null).QualityProfile.Should().Be(movieProfile);
        }

        [Test]
        public void should_fall_back_to_movie_profile_when_edition_profile_was_removed()
        {
            var movieProfile = new QualityProfile { Id = 1, Name = "Movie" };
            _movie.QualityProfileId = 1;
            _movie.QualityProfile = movieProfile;

            var edition = GivenEdition(1, "IMAX");
            edition.QualityProfileId = 2;

            Subject.Resolve(_movie, "IMAX").QualityProfile.Should().Be(movieProfile);
        }

        [Test]
        public void should_not_want_standard_slot_when_movie_only_wants_extra_editions()
        {
            _movie.StandardEditionMonitored = false;
            GivenEdition(1, "Extended");

            Subject.Resolve(_movie, null).Monitored.Should().BeFalse();
            Subject.Resolve(_movie, "Extended").Monitored.Should().BeTrue();
        }

        [Test]
        public void should_reject_duplicate_edition()
        {
            GivenEdition(1, "Director's Cut");

            Assert.Throws<NzbDroneClientException>(() => Subject.Add(new MovieEdition { MovieId = _movie.Id, Name = "Directors Cut" }));
        }

        [Test]
        public void should_move_matching_standard_file_into_new_edition()
        {
            _standardFile.Edition = "Extended";

            var edition = Subject.Add(new MovieEdition { MovieId = _movie.Id, Name = "Extended", Monitored = true });

            _standardFile.MovieEditionId.Should().Be(edition.Id);
            _movie.MovieFileId.Should().Be(0);

            Mocker.GetMock<IMediaFileService>().Verify(v => v.Update(_standardFile), Times.Once());
            Mocker.GetMock<IMovieService>().Verify(v => v.UpdateMovie(_movie), Times.Once());
        }

        [Test]
        public void should_leave_untagged_standard_file_when_edition_added()
        {
            Subject.Add(new MovieEdition { MovieId = _movie.Id, Name = "Extended", Monitored = true });

            _standardFile.MovieEditionId.Should().Be(0);
            _movie.MovieFileId.Should().Be(_standardFile.Id);
        }

        [Test]
        public void should_not_delete_edition_that_has_a_file()
        {
            GivenEdition(1, "Extended");
            GivenEditionFile(11, 1, "Extended");

            Assert.Throws<NzbDroneClientException>(() => Subject.Delete(1));

            Mocker.GetMock<IMovieEditionRepository>().Verify(v => v.Delete(It.IsAny<int>()), Times.Never());
        }

        [Test]
        public void should_not_assign_file_to_edition_that_already_has_one()
        {
            GivenEdition(1, "Extended");
            GivenEditionFile(11, 1, "Extended");

            Assert.Throws<NzbDroneClientException>(() => Subject.AssignFile(_standardFile, 1));
        }

        [Test]
        public void should_assign_file_to_standard_slot_and_clear_edition_text()
        {
            GivenEdition(1, "Extended");
            var extendedFile = GivenEditionFile(11, 1, "Extended");
            _movie.MovieFileId = 0;
            _movie.MovieFile = null;

            Subject.AssignFile(extendedFile, 0);

            extendedFile.MovieEditionId.Should().Be(0);
            extendedFile.Edition.Should().BeNull();
            _movie.MovieFileId.Should().Be(extendedFile.Id);
        }

        [Test]
        public void should_set_edition_text_when_assigning_untagged_file_to_edition()
        {
            GivenEdition(1, "IMAX");

            Subject.AssignFile(_standardFile, 1);

            _standardFile.MovieEditionId.Should().Be(1);
            _standardFile.Edition.Should().Be("IMAX");
            _movie.MovieFileId.Should().Be(0);
        }
    }
}
