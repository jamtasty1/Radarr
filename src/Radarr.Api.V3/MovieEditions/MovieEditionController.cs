using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Movies.Editions;
using NzbDrone.SignalR;
using Radarr.Http;
using Radarr.Http.REST;
using Radarr.Http.REST.Attributes;

namespace Radarr.Api.V3.MovieEditions
{
    // Editions a movie tracks in addition to its standard edition (which is the movie's own movieFile).
    [V3ApiController("movieedition")]
    public class MovieEditionController : RestControllerWithSignalR<MovieEditionResource, MovieEdition>
    {
        private readonly IMovieEditionService _movieEditionService;
        private readonly IMediaFileService _mediaFileService;

        public MovieEditionController(IBroadcastSignalRMessage signalRBroadcaster,
                                      IMovieEditionService movieEditionService,
                                      IMediaFileService mediaFileService)
            : base(signalRBroadcaster)
        {
            _movieEditionService = movieEditionService;
            _mediaFileService = mediaFileService;

            SharedValidator.RuleFor(e => e.Name).NotEmpty();
            PostValidator.RuleFor(e => e.MovieId).GreaterThan(0);
        }

        protected override MovieEditionResource GetResourceById(int id)
        {
            var edition = _movieEditionService.Get(id);

            return ToResources(edition.MovieId, new[] { edition }).Single();
        }

        [HttpGet]
        [Produces("application/json")]
        public Results<Ok<List<MovieEditionResource>>, BadRequest> GetEditions([FromQuery] int movieId)
        {
            if (movieId <= 0)
            {
                throw new BadRequestException("movieId must be provided");
            }

            return TypedResults.Ok(ToResources(movieId, _movieEditionService.GetByMovieId(movieId)));
        }

        [RestPostById]
        [Consumes("application/json")]
        public Results<Created<MovieEditionResource>, NotFound> Create([FromBody] MovieEditionResource resource)
        {
            return Created(_movieEditionService.Add(resource.ToModel()).Id);
        }

        [RestPutById]
        [Consumes("application/json")]
        public Results<Accepted<MovieEditionResource>, NotFound> Update([FromBody] MovieEditionResource resource)
        {
            _movieEditionService.Update(resource.ToModel());

            return Accepted(resource.Id);
        }

        [RestDeleteById]
        public Ok Delete(int id)
        {
            _movieEditionService.Delete(id);

            return TypedResults.Ok();
        }

        private List<MovieEditionResource> ToResources(int movieId, IEnumerable<MovieEdition> editions)
        {
            var files = _mediaFileService.GetFilesByMovie(movieId);

            return editions
                .Select(e => e.ToResource(files.Where(f => f.MovieEditionId == e.Id).MaxBy(f => f.Id)))
                .ToList();
        }
    }
}
