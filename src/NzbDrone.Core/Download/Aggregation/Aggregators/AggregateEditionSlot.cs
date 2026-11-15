using NzbDrone.Core.Movies.Editions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Download.Aggregation.Aggregators
{
    public class AggregateEditionSlot : IAggregateRemoteMovie
    {
        private readonly IMovieEditionService _movieEditionService;

        public AggregateEditionSlot(IMovieEditionService movieEditionService)
        {
            _movieEditionService = movieEditionService;
        }

        public RemoteMovie Aggregate(RemoteMovie remoteMovie)
        {
            if (remoteMovie.Movie != null)
            {
                remoteMovie.EditionSlot = _movieEditionService.Resolve(remoteMovie.Movie, remoteMovie.ParsedMovieInfo?.Edition);
            }

            return remoteMovie;
        }
    }
}
