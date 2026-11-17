using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(246)]
    public class add_edition_tag_style : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            // 0 = Plex "{edition-...}" tags, the existing behaviour.
            Alter.Table("NamingConfig").AddColumn("EditionTagStyle").AsInt32().NotNullable().WithDefaultValue(0);
        }
    }
}
