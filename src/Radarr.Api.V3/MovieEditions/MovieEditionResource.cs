using System;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Movies.Editions;
using Radarr.Http.REST;

namespace Radarr.Api.V3.MovieEditions
{
    public class MovieEditionResource : RestResource
    {
        public int MovieId { get; set; }
        public string Name { get; set; }
        public bool Monitored { get; set; }
        public DateTime Added { get; set; }

        // 0 = use the movie's quality profile.
        public int QualityProfileId { get; set; }

        // Read-only: the file currently filling this edition.
        public int? MovieFileId { get; set; }
        public bool HasFile { get; set; }
    }

    public class StandardEditionResource
    {
        public int MovieId { get; set; }

        // Blank for the plain standard edition.
        public string Name { get; set; }
    }

    public static class MovieEditionResourceMapper
    {
        public static MovieEditionResource ToResource(this MovieEdition model, MovieFile movieFile)
        {
            if (model == null)
            {
                return null;
            }

            return new MovieEditionResource
            {
                Id = model.Id,
                MovieId = model.MovieId,
                Name = model.Name,
                Monitored = model.Monitored,
                Added = model.Added,
                QualityProfileId = model.QualityProfileId,
                MovieFileId = movieFile?.Id,
                HasFile = movieFile != null
            };
        }

        public static MovieEdition ToModel(this MovieEditionResource resource)
        {
            if (resource == null)
            {
                return null;
            }

            return new MovieEdition
            {
                Id = resource.Id,
                MovieId = resource.MovieId,
                Name = resource.Name,
                Monitored = resource.Monitored,
                QualityProfileId = resource.QualityProfileId
            };
        }
    }
}
