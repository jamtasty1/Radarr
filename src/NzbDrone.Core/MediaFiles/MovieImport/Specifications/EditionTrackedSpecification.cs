using NLog;
using NzbDrone.Core.Download;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.MovieImport.Specifications
{
    // Once a movie tracks editions, a file tagged with an edition the movie doesn't track is not
    // imported, so it can't take over the standard edition's place.
    public class EditionTrackedSpecification : IImportDecisionEngineSpecification
    {
        private readonly Logger _logger;

        public EditionTrackedSpecification(Logger logger)
        {
            _logger = logger;
        }

        public ImportSpecDecision IsSatisfiedBy(LocalMovie localMovie, DownloadClientItem downloadClientItem)
        {
            if (localMovie.EditionSlot == null || localMovie.EditionSlot.Tracked)
            {
                return ImportSpecDecision.Accept();
            }

            _logger.Debug("Edition '{0}' is not tracked for {1}, skipping {2}", localMovie.Edition, localMovie.Movie, localMovie.Path);

            return ImportSpecDecision.Reject(ImportRejectionReason.EditionNotTracked,
                "Edition '{0}' is not tracked for this movie. Add it as an edition to import this file.",
                localMovie.Edition);
        }
    }
}
