using NLog;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    // Rejects releases for editions a movie doesn't track, and (outside user-invoked searches)
    // releases for unmonitored editions, including the standard edition when the movie only
    // wants its extra editions.
    public class EditionWantedSpecification : IDownloadDecisionEngineSpecification
    {
        private readonly Logger _logger;

        public EditionWantedSpecification(Logger logger)
        {
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        public DownloadSpecDecision IsSatisfiedBy(RemoteMovie subject, ReleaseDecisionInformation information)
        {
            var slot = subject.EditionSlot;

            if (slot == null)
            {
                return DownloadSpecDecision.Accept();
            }

            if (!slot.Tracked)
            {
                _logger.Debug("Edition '{0}' is not tracked for {1}. Rejecting", slot.Name, subject.Movie);
                return DownloadSpecDecision.Reject(DownloadRejectionReason.EditionNotTracked, "Edition '{0}' is not tracked for this movie", slot.Name);
            }

            // An unmonitored movie is MonitoredMovieSpecification's to reject.
            if (slot.Monitored || !subject.Movie.Monitored)
            {
                return DownloadSpecDecision.Accept();
            }

            if (information.SearchCriteria is { UserInvokedSearch: true })
            {
                _logger.Debug("Skipping edition monitored check during search");
                return DownloadSpecDecision.Accept();
            }

            _logger.Debug("Edition '{0}' of {1} is not monitored. Rejecting", slot.Name, subject.Movie);
            return DownloadSpecDecision.Reject(DownloadRejectionReason.EditionNotMonitored, "Edition '{0}' is not monitored", slot.Name);
        }
    }
}
