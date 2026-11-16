using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.Movies;
using NzbDrone.Core.Movies.Editions;
using NzbDrone.Core.Movies.Translations;
using NzbDrone.Core.MovieStats;
using NzbDrone.SignalR;
using Radarr.Api.V3.Movies;
using Radarr.Http;
using Radarr.Http.Extensions;

namespace Radarr.Api.V3.Wanted
{
    [V3ApiController("wanted/missing")]
    public class MissingController : MovieControllerWithSignalR
    {
        private readonly IMovieEditionService _movieEditionService;

        public MissingController(IMovieEditionService movieEditionService,
                            IMovieService movieService,
                            IMovieTranslationService movieTranslationService,
                            IMovieStatisticsService movieStatisticsService,
                            IUpgradableSpecification upgradableSpecification,
                            ICustomFormatCalculationService formatCalculator,
                            IConfigService configService,
                            IMapCoversToLocal coverMapper,
                            IBroadcastSignalRMessage signalRBroadcaster)
            : base(movieService, movieTranslationService, movieStatisticsService, upgradableSpecification, formatCalculator, configService, coverMapper, signalRBroadcaster)
        {
            _movieEditionService = movieEditionService;
        }

        [NonAction]
        public override Results<Ok<MovieResource>, NotFound> GetResourceByIdWithErrorHandler(int id)
        {
            throw new NotImplementedException();
        }

        [HttpGet]
        [Produces("application/json")]
        public Ok<PagingResource<MovieResource>> GetMissingMovies([FromQuery] PagingRequestResource paging, bool monitored = true)
        {
            var pagingResource = new PagingResource<MovieResource>(paging);
            var pagingSpec = pagingResource.MapToPagingSpec<MovieResource, Movie>(
                new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "movieMetadata.digitalRelease",
                    "movieMetadata.inCinemas",
                    "movieMetadata.physicalRelease",
                    "movieMetadata.sortTitle",
                    "movieMetadata.year",
                    "movies.lastSearchTime"
                },
                "movieMetadata.sortTitle",
                SortDirection.Ascending);

            pagingSpec.FilterExpressions.Add(v => v.Monitored == monitored);

            var resource = pagingSpec.ApplyToPage(_movieService.MoviesWithoutFiles, v => AddMissingEditions(MapToResource(v), v));

            return TypedResults.Ok(resource);
        }

        private MovieResource AddMissingEditions(MovieResource resource, Movie movie)
        {
            if (_movieEditionService.HasEditions(movie.Id))
            {
                resource.WantedEditions = _movieEditionService.GetSlots(movie)
                    .Where(s => s.EditionMonitored && s.MovieFile == null)
                    .Select(s => s.Name)
                    .ToList();
            }

            return resource;
        }
    }
}
