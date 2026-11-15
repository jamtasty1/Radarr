using System.Collections.Generic;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Movies.Editions
{
    public interface IMovieEditionRepository : IBasicRepository<MovieEdition>
    {
        List<MovieEdition> GetByMovieId(int movieId);
        void DeleteForMovies(List<int> movieIds);
    }

    public class MovieEditionRepository : BasicRepository<MovieEdition>, IMovieEditionRepository
    {
        public MovieEditionRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public List<MovieEdition> GetByMovieId(int movieId)
        {
            return Query(e => e.MovieId == movieId);
        }

        public void DeleteForMovies(List<int> movieIds)
        {
            Delete(e => movieIds.Contains(e.MovieId));
        }
    }
}
