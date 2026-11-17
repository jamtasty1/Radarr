using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(247)]
    public class add_standard_edition_name : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            // Null = the plain standard edition; otherwise the cut the movie's main copy is (e.g. "Extended").
            Alter.Table("Movies").AddColumn("StandardEditionName").AsString().Nullable();
        }
    }
}
