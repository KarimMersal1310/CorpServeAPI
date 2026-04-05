using CorpServe.Presistence.Data.DbContext;
using CorpServe.Domain.Contracts;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CorpServe.Web.Extensions
{
    public static class WebApplicationRegistration
    {
        public async static Task<WebApplication> MigrateDatabaseAsync(this WebApplication app)
        {
            try
            {
                await using var scope = app.Services.CreateAsyncScope();
                var dbContextService = scope.ServiceProvider.GetRequiredService<CorpServeDbContext>();
                var pendingMigrations = await dbContextService.Database.GetPendingMigrationsAsync();
                if (pendingMigrations.Any())
                    await dbContextService.Database.MigrateAsync();
            }
            catch (SqlException ex)
            {
                app.Logger.LogError(ex, "Database connection failed while checking/applying migrations.");
            }
            catch (Exception ex)
            {
                app.Logger.LogError(ex, "Unexpected error while checking/applying migrations.");
            }

            return app;
        }

        public async static Task<WebApplication> SeedIdentityDatabaseAsync(this WebApplication app)
        {
            await using var scope = app.Services.CreateAsyncScope();
            var dataInitializerService = scope.ServiceProvider.GetRequiredKeyedService<IDataInitializer>("Identity");
            await dataInitializerService.InitializeAsync();
            return app;
        }

        public async static Task<WebApplication> SeedDatabaseAsync(this WebApplication app)
        {
            await using var scope = app.Services.CreateAsyncScope();
            var dataInitializerService = scope.ServiceProvider.GetRequiredKeyedService<IDataInitializer>("Default");
            await dataInitializerService.InitializeAsync();
            return app;
        }
    }
}

