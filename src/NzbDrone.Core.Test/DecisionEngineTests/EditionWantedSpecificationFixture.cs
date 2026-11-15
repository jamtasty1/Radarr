using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Movies;
using NzbDrone.Core.Movies.Editions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]
    public class EditionWantedSpecificationFixture : CoreTest<EditionWantedSpecification>
    {
        private RemoteMovie _remoteMovie;

        [SetUp]
        public void Setup()
        {
            _remoteMovie = new RemoteMovie
            {
                Movie = new Movie { Id = 1, Monitored = true },
                ParsedMovieInfo = new ParsedMovieInfo()
            };
        }

        private void GivenSlot(bool tracked, bool monitored, int editionId = 5)
        {
            _remoteMovie.EditionSlot = new EditionSlot
            {
                MovieEditionId = editionId,
                Name = "Extended",
                Tracked = tracked,
                Monitored = monitored
            };
        }

        private DownloadSpecDecision Evaluate(bool userInvokedSearch = false)
        {
            var criteria = userInvokedSearch ? new MovieSearchCriteria { UserInvokedSearch = true } : null;

            return Subject.IsSatisfiedBy(_remoteMovie, new ReleaseDecisionInformation(false, criteria));
        }

        [Test]
        public void should_accept_when_no_slot_resolved()
        {
            Evaluate().Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_standard_slot()
        {
            GivenSlot(tracked: true, monitored: true, editionId: 0);

            Evaluate().Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_monitored_tracked_edition()
        {
            GivenSlot(tracked: true, monitored: true);

            Evaluate().Accepted.Should().BeTrue();
        }

        [Test]
        public void should_reject_untracked_edition()
        {
            GivenSlot(tracked: false, monitored: false, editionId: -1);

            var decision = Evaluate();

            decision.Accepted.Should().BeFalse();
            decision.Reason.Should().Be(DownloadRejectionReason.EditionNotTracked);
        }

        [Test]
        public void should_reject_untracked_edition_even_in_user_search()
        {
            GivenSlot(tracked: false, monitored: false, editionId: -1);

            Evaluate(userInvokedSearch: true).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_reject_unmonitored_edition()
        {
            GivenSlot(tracked: true, monitored: false);

            var decision = Evaluate();

            decision.Accepted.Should().BeFalse();
            decision.Reason.Should().Be(DownloadRejectionReason.EditionNotMonitored);
        }

        [Test]
        public void should_accept_unmonitored_edition_in_user_search()
        {
            GivenSlot(tracked: true, monitored: false);

            Evaluate(userInvokedSearch: true).Accepted.Should().BeTrue();
        }
    }
}
