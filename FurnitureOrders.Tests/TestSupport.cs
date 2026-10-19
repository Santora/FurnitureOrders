using FurnitureOrders.Api.Ai;
using FurnitureOrders.Api.Data;
using FurnitureOrders.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace FurnitureOrders.Tests;

public static class TestDb
{
    public static AppDbContext Create()
    {
        return new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
    }

    /// aktivní zákazník a výrobky pojmenované jako v zadání (Alfa, Beta, dvě police)
    public static async Task<int> SeedCatalogAsync(AppDbContext db)
    {
        var customer = new Customer { CustomerNumber = "T-ZAK", Name = "Testovací zákazník", IsActive = true };
        _ = db.Customers.Add(customer);

        db.Products.AddRange(
            Product("VYR-001", "Psací stůl Alfa", 4990m),
            Product("VYR-002", "Skříňka Beta", 2490m),
            Product("VYR-004", "Police Delta", 1290m),
            Product("VYR-005", "Police Epsilon", 1590m));

        _ = await db.SaveChangesAsync();
        return customer.Id;
    }

    public static Product Product(string code, string name, decimal price, bool active = true)
    {
        return new() { Code = code, Name = name, Unit = "ks", SalesPrice = price, IsActive = active };
    }
}

public sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow()
    {
        return now;
    }

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}

public sealed class StubExtractor(Func<RawExtraction> produce) : IOrderTextExtractor
{
    public string Provider => "Stub";
    public bool IsConfigured => true;

    public Task<RawExtraction> ExtractAsync(
        string text, DateOnly today, IReadOnlyList<CatalogEntry> catalog, CancellationToken ct)
    {
        return Task.FromResult(produce());
    }
}
