using Dapper;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    public class CleanupOrphanedMovieFiles : IHousekeepingTask
    {
        private readonly IMainDatabase _database;

        public CleanupOrphanedMovieFiles(IMainDatabase database)
        {
            _database = database;
        }

        public void Clean()
        {
            using var mapper = _database.OpenConnection();

            // Standard edition: a file is only kept while its movie points at it (Movies.MovieFileId).
            mapper.Execute(@"DELETE FROM ""MovieFiles""
                             WHERE ""MovieEditionId"" = 0 AND ""Id"" IN (
                             SELECT ""MovieFiles"".""Id"" FROM ""MovieFiles""
                             LEFT OUTER JOIN ""Movies""
                             ON ""MovieFiles"".""Id"" = ""Movies"".""MovieFileId""
                             WHERE ""Movies"".""Id"" IS NULL)");

            // Other editions: drop files whose movie or edition no longer exists.
            mapper.Execute(@"DELETE FROM ""MovieFiles""
                             WHERE ""MovieEditionId"" <> 0 AND ""Id"" IN (
                             SELECT ""MovieFiles"".""Id"" FROM ""MovieFiles""
                             LEFT OUTER JOIN ""Movies""
                             ON ""MovieFiles"".""MovieId"" = ""Movies"".""Id""
                             LEFT OUTER JOIN ""MovieEditions""
                             ON ""MovieFiles"".""MovieEditionId"" = ""MovieEditions"".""Id""
                             AND ""MovieFiles"".""MovieId"" = ""MovieEditions"".""MovieId""
                             WHERE ""Movies"".""Id"" IS NULL OR ""MovieEditions"".""Id"" IS NULL)");

            // Other editions: one file per edition, the newest wins (mirrors MovieFileId moving to the newest standard file).
            mapper.Execute(@"DELETE FROM ""MovieFiles""
                             WHERE ""MovieEditionId"" <> 0 AND ""Id"" IN (
                             SELECT ""Older"".""Id"" FROM ""MovieFiles"" AS ""Older""
                             INNER JOIN ""MovieFiles"" AS ""Newer""
                             ON ""Newer"".""MovieId"" = ""Older"".""MovieId""
                             AND ""Newer"".""MovieEditionId"" = ""Older"".""MovieEditionId""
                             AND ""Newer"".""Id"" > ""Older"".""Id"")");
        }
    }
}
