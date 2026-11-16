using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Download.Aggregation.Aggregators;
using NzbDrone.Core.Movies;
using NzbDrone.Core.Movies.Editions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Download.Aggregation.Aggregators
{
    [TestFixture]
    public class AggregateEditionSlotFixture : CoreTest<AggregateEditionSlot>
    {
        private const string Title = "Movie.2000.Redux.720p.WEB-DL.x264-GRP";

        private RemoteMovie _remoteMovie;

        [SetUp]
        public void Setup()
        {
            _remoteMovie = new RemoteMovie
            {
                Movie = new Movie { Id = 1 },
                Release = new ReleaseInfo { Title = Title },
                ParsedMovieInfo = new ParsedMovieInfo { ReleaseTitle = Title, Edition = "" }
            };
        }

        private void GivenSlot(EditionSlot slot)
        {
            Mocker.GetMock<IMovieEditionService>()
                  .Setup(s => s.Resolve(_remoteMovie.Movie, It.IsAny<string>(), It.IsAny<string[]>()))
                  .Returns(slot);
        }

        [Test]
        public void should_pass_release_title_when_resolving_edition()
        {
            GivenSlot(EditionSlot.Standard(_remoteMovie.Movie, null));

            Subject.Aggregate(_remoteMovie);

            Mocker.GetMock<IMovieEditionService>()
                  .Verify(s => s.Resolve(_remoteMovie.Movie, "", It.Is<string[]>(t => t.Length > 0 && t[0] == Title)), Times.Once());
        }

        [Test]
        public void should_record_edition_found_in_title()
        {
            GivenSlot(new EditionSlot { MovieEditionId = 4, Name = "Redux", Tracked = true });

            Subject.Aggregate(_remoteMovie);

            _remoteMovie.EditionSlot.MovieEditionId.Should().Be(4);
            _remoteMovie.ParsedMovieInfo.Edition.Should().Be("Redux");
        }

        [Test]
        public void should_keep_parsed_edition_text()
        {
            _remoteMovie.ParsedMovieInfo.Edition = "Directors Cut";
            GivenSlot(new EditionSlot { MovieEditionId = 4, Name = "Director's Cut", Tracked = true });

            Subject.Aggregate(_remoteMovie);

            _remoteMovie.ParsedMovieInfo.Edition.Should().Be("Directors Cut");
        }

        [Test]
        public void should_leave_standard_edition_text_empty()
        {
            GivenSlot(EditionSlot.Standard(_remoteMovie.Movie, null));

            Subject.Aggregate(_remoteMovie);

            _remoteMovie.ParsedMovieInfo.Edition.Should().BeEmpty();
        }
    }
}
