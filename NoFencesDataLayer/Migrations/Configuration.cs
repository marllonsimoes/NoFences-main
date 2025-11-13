namespace NoFencesDataLayer.Migrations
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<NoFencesService.Repository.LocalDBContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
            AutomaticMigrationDataLossAllowed = true;
            // SQLite provider is configured via App.config - no need to manually set SQL generator
            // SetSqlGenerator is handled automatically by System.Data.SQLite.EF6 provider
        }

        protected override void Seed(NoFencesService.Repository.LocalDBContext context)
        {
            //  This method will be called after migrating to the latest version.

            //  You can use the DbSet<T>.AddOrUpdate() helper extension method
            //  to avoid creating duplicate seed data.
        }
    }
}
