using System.Collections.Generic;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Download.Clients;
using NzbDrone.Core.Languages;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Movies;
using NzbDrone.Core.Movies.Editions;
using NzbDrone.Core.Profiles.Qualities;

namespace NzbDrone.Core.Parser.Model
{
    public class RemoteMovie
    {
        public ReleaseInfo Release { get; set; }
        public ParsedMovieInfo ParsedMovieInfo { get; set; }
        public List<CustomFormat> CustomFormats { get; set; }
        public int CustomFormatScore { get; set; }
        public MovieMatchType MovieMatchType { get; set; }
        public Movie Movie { get; set; }
        public bool MovieRequested { get; set; }
        public bool DownloadAllowed { get; set; }
        public TorrentSeedConfiguration SeedConfiguration { get; set; }
        public List<Language> Languages { get; set; }
        public ReleaseSourceType ReleaseSource { get; set; }

        // Which edition of the movie this release is (standard or a tracked edition).
        public EditionSlot EditionSlot { get; set; }

        // The profile this release/file is judged by: its edition's own profile, else the movie's.
        public QualityProfile QualityProfile => EditionSlot?.QualityProfile ?? Movie?.QualityProfile;

        // The file this release would replace: the one in the same edition slot.
        public MovieFile ExistingMovieFile => EditionSlot != null
            ? EditionSlot.MovieFile
            : Movie?.MovieFileId > 0 ? Movie.MovieFile : null;

        // Standard edition keeps Movie.HasFile semantics (MovieFileId set, file object may not be loaded).
        public bool HasExistingFile => EditionSlot is { IsStandard: false }
            ? EditionSlot.MovieFile != null
            : Movie?.HasFile == true;

        public int EditionSlotId => EditionSlot?.MovieEditionId ?? EditionSlot.StandardEditionId;

        public RemoteMovie()
        {
            CustomFormats = new List<CustomFormat>();
            Languages = new List<Language>();
        }

        public override string ToString()
        {
            return Release.Title;
        }
    }

    public enum ReleaseSourceType
    {
        Unknown = 0,
        Rss = 1,
        Search = 2,
        UserInvokedSearch = 3,
        InteractiveSearch = 4,
        ReleasePush = 5
    }
}
