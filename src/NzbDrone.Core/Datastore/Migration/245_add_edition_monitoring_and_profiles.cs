using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(245)]
    public class add_edition_monitoring_and_profiles : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            // Lets a movie want only its extra editions (e.g. just the Extended cut).
            Alter.Table("Movies").AddColumn("StandardEditionMonitored").AsBoolean().NotNullable().WithDefaultValue(true);

            // 0 = use the movie's quality profile.
            Alter.Table("MovieEditions").AddColumn("QualityProfileId").AsInt32().NotNullable().WithDefaultValue(0);
        }
    }
}
