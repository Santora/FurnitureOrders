using Microsoft.EntityFrameworkCore;

namespace FurnitureOrders.Api.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        var section = app.Configuration.GetSection("Database");
        var migrate = section.GetValue("MigrateOnStartup", true);
        var seed = section.GetValue("SeedDemoData", true);
        if (!migrate && !seed) return;

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitializer");

        if (migrate)
        {
            logger.LogInformation("Applying database migrations.");
            await db.Database.MigrateAsync();
        }

        if (seed)
            await DemoDataSeeder.SeedAsync(db, logger);
    }
}