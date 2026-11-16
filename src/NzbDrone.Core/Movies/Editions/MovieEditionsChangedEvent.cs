using NzbDrone.Common.Messaging;

namespace NzbDrone.Core.Movies.Editions
{
    // A movie's editions were added, changed or removed, so releases already matched to
    // one of its editions may now belong to a different one.
    public class MovieEditionsChangedEvent : IEvent
    {
        public int MovieId { get; private set; }

        public MovieEditionsChangedEvent(int movieId)
        {
            MovieId = movieId;
        }
    }
}
