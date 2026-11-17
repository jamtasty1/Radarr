using System.IO;
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
                // Grabbed release first, then download, folder and file names, matching AggregateEdition's order.
                localMovie.EditionSlot = _movieEditionService.Resolve(localMovie.Movie,
                    localMovie.Edition,
                    localMovie.Release?.Title,
                    localMovie.DownloadClientMovieInfo?.ReleaseTitle,
                    downloadClientItem?.Title,
                    localMovie.FolderMovieInfo?.ReleaseTitle,
                    localMovie.FileMovieInfo?.ReleaseTitle,
                    Path.GetFileNameWithoutExtension(localMovie.Path ?? string.Empty));

                // A tracked edition the parser doesn't know was found in a name: keep it as the file's
                // edition so renaming writes it and the file stays in that edition.
                if (localMovie.EditionSlot is { Tracked: true } slot && (slot.IsNamedCut || !slot.IsStandard) && EditionNormalizer.IsStandard(localMovie.Edition))
                {
                    localMovie.Edition = localMovie.EditionSlot.Name;
                }
            }

            return localMovie;
        }
    }
}
