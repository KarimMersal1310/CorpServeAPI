using CorpServe.Presistence.Data.DbContext;
using EventHub.Domain.Contracts;
using Microsoft.EntityFrameworkCore;

namespace EventHubWeb.Extensions
{
    public static class WebApplicationRegistration
    {
        public async static Task<WebApplication> MigrateDatabaseAsync(this WebApplication app)
        {
            await using var scope = app.Services.CreateAsyncScope();
            var DbContextService = scope.ServiceProvider.GetRequiredService<CorpServeDbContext>();
            var pendingMigrations = await DbContextService.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
                await DbContextService.Database.MigrateAsync();
            return app;
        }

        public async static Task<WebApplication> SeedIdentityDatabaseAsync(this WebApplication app)
        {
            await using var scope = app.Services.CreateAsyncScope();
            var DataInitializerService = scope.ServiceProvider.GetRequiredKeyedService<IDataInitializer>("Identity");
            await DataInitializerService.InitializeAsync();
            return app;
        }
        public async static Task<WebApplication> SeedDatabaseAsync(this WebApplication app)
        {
            await using var scope = app.Services.CreateAsyncScope();
            var DataInitializerService = scope.ServiceProvider.GetRequiredKeyedService<IDataInitializer>("Default");
            await DataInitializerService.InitializeAsync();
            return app;
        }
    }
}
