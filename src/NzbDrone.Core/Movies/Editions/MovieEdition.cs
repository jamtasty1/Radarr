using System;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Movies.Editions
{
    // An additional edition of a movie (Extended, Director's Cut, IMAX, ...) that is tracked
    // alongside the movie's standard edition. Each edition holds at most one movie file.
    public class MovieEdition : ModelBase
    {
        public int MovieId { get; set; }
        public string Name { get; set; }
        public bool Monitored { get; set; }
        public DateTime Added { get; set; }

        // 0 = use the movie's quality profile.
        public int QualityProfileId { get; set; }

        public string Key => EditionNormalizer.Normalize(Name);

        public override string ToString()
        {
            return string.Format("[{0}] {1} (Movie {2})", Id, Name, MovieId);
        }
    }
}
