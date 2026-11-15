using NzbDrone.Core.Download;
using NzbDrone.Core.Movies.Editions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.MovieImport.Aggregation.Aggregators
{
    // Runs after AggregateEdition so the file's edition text is known.
    public class AggregateMovieEditionSlot : IAggregateLocalMovie
    {
        private readonly IMovieEditionService _movieEditionService;

        public AggregateMovieEditionSlot(IMovieEditionService movieEditionService)
        {
            _movieEditionService = movieEditionService;
        }

        public int Order => 10;

        public LocalMovie Aggregate(LocalMovie localMovie, DownloadClientItem downloadClientItem)
        {
            if (localMovie.Movie != null)
            {
                localMovie.EditionSlot = _movieEditionService.Resolve(localMovie.Movie, localMovie.Edition);
            }

            return localMovie;
        }
    }
}
