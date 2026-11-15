using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(244)]
    public class add_movie_editions : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Create.TableForModel("MovieEditions")
                  .WithColumn("MovieId").AsInt32().NotNullable().Indexed()
                  .WithColumn("Name").AsString().NotNullable()
                  .WithColumn("Monitored").AsBoolean().NotNullable().WithDefaultValue(true)
                  .WithColumn("Added").AsDateTimeOffset().NotNullable();

            // 0 = the movie's standard edition, so every existing file keeps its current meaning.
            Alter.Table("MovieFiles").AddColumn("MovieEditionId").AsInt32().NotNullable().WithDefaultValue(0);
        }
    }
}
