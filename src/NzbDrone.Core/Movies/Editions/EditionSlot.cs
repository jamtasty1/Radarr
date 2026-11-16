using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Profiles.Qualities;

namespace NzbDrone.Core.Movies.Editions
{
    // The place a release or file occupies within a movie: either the standard edition
    // (MovieEditionId 0) or one of the movie's tracked editions.
    public class EditionSlot
    {
        public const int StandardEditionId = 0;
        public const string StandardEditionName = "Standard";

        public int MovieEditionId { get; set; }
        public string Name { get; set; }

        // False when the release names an edition that this movie does not track.
        public bool Tracked { get; set; }
        public bool Monitored { get; set; }

        // The edition's own monitored setting, regardless of whether the movie is monitored.
        public bool EditionMonitored { get; set; }

        // The file currently filling this slot, if any.
        public MovieFile MovieFile { get; set; }

        // The profile releases for this slot are judged by: the edition's own, else the movie's.
        public QualityProfile QualityProfile { get; set; }

        public bool IsStandard => MovieEditionId == StandardEditionId;

        public static EditionSlot Standard(Movie movie, MovieFile movieFile)
        {
            return new EditionSlot
            {
                MovieEditionId = StandardEditionId,
                Name = StandardEditionName,
                Tracked = true,
                Monitored = movie.Monitored && movie.StandardEditionMonitored,
                EditionMonitored = movie.StandardEditionMonitored,
                MovieFile = movieFile,
                QualityProfile = movie.QualityProfile
            };
        }

        public static EditionSlot Untracked(string edition)
        {
            return new EditionSlot
            {
                MovieEditionId = -1,
                Name = edition,
                Tracked = false,
                Monitored = false
            };
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
