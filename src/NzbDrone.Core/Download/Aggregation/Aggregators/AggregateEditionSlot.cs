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
                var parsedEdition = remoteMovie.ParsedMovieInfo?.Edition;

                remoteMovie.EditionSlot = _movieEditionService.Resolve(remoteMovie.Movie,
                    parsedEdition,
                    remoteMovie.Release?.Title,
                    remoteMovie.ParsedMovieInfo?.ReleaseTitle);

                // A tracked edition the parser doesn't know (e.g. "Redux") was found in the title:
                // record it so custom formats, the UI and naming see the edition too.
                if (remoteMovie.ParsedMovieInfo != null && remoteMovie.EditionSlot is { Tracked: true, IsNamedCut: true } && EditionNormalizer.IsStandard(parsedEdition))
                {
                    remoteMovie.ParsedMovieInfo.Edition = remoteMovie.EditionSlot.Name;
                }
            }

            return remoteMovie;
        }
    }
}
