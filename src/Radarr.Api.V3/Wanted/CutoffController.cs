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
    [V3ApiController("wanted/cutoff")]
    public class CutoffController : MovieControllerWithSignalR
    {
        private readonly IMovieCutoffService _movieCutoffService;
        private readonly IMovieEditionService _movieEditionService;
        private readonly IUpgradableSpecification _cutoffSpecification;

        public CutoffController(IMovieCutoffService movieCutoffService,
                            IMovieEditionService movieEditionService,
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
            _movieCutoffService = movieCutoffService;
            _movieEditionService = movieEditionService;
            _cutoffSpecification = upgradableSpecification;
        }

        [NonAction]
        public override Results<Ok<MovieResource>, NotFound> GetResourceByIdWithErrorHandler(int id)
        {
            throw new NotImplementedException();
        }

        [HttpGet]
        [Produces("application/json")]
        public Ok<PagingResource<MovieResource>> GetCutoffUnmetMovies([FromQuery] PagingRequestResource paging, bool monitored = true)
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

            var resource = pagingSpec.ApplyToPage(_movieCutoffService.MoviesWhereCutoffUnmet, v => AddCutoffUnmetEditions(MapToResource(v), v));

            return TypedResults.Ok(resource);
        }

        // Matches the cutoff query, which compares quality only. Each entry names the edition and
        // its file's quality, since the row's own quality column shows the standard file.
        private MovieResource AddCutoffUnmetEditions(MovieResource resource, Movie movie)
        {
            if (_movieEditionService.HasEditions(movie.Id))
            {
                resource.WantedEditions = _movieEditionService.GetSlots(movie)
                    .Where(s => s.EditionMonitored && s.MovieFile != null && s.QualityProfile != null)
                    .Where(s => _cutoffSpecification.QualityCutoffNotMet(s.QualityProfile, s.MovieFile.Quality))
                    .Select(s => $"{s.Name} ({s.MovieFile.Quality.Quality.Name})")
                    .ToList();
            }

            return resource;
        }
    }
}
