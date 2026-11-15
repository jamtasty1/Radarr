using Dapper;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    public class CleanupOrphanedMovieEditions : IHousekeepingTask
    {
        private readonly IMainDatabase _database;

        public CleanupOrphanedMovieEditions(IMainDatabase database)
        {
            _database = database;
        }

        public void Clean()
        {
            using var mapper = _database.OpenConnection();

            mapper.Execute(@"DELETE FROM ""MovieEditions""
                             WHERE ""Id"" IN (
                             SELECT ""MovieEditions"".""Id"" FROM ""MovieEditions""
                             LEFT OUTER JOIN ""Movies""
                             ON ""MovieEditions"".""MovieId"" = ""Movies"".""Id""
                             WHERE ""Movies"".""Id"" IS NULL)");
        }
    }
}
