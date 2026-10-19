using FurnitureOrders.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace FurnitureOrders.Tests;

public class DemoDataSeederTests
{
    private static async Task<int[]> CountsAsync(AppDbContext db)
    {
        return [
        await db.Customers.CountAsync(), await db.Materials.CountAsync(), await db.Operations.CountAsync(),
        await db.Products.CountAsync(), await db.ProductMaterials.CountAsync(), await db.ProductOperations.CountAsync(),
        await db.Orders.CountAsync(), await db.OrderItems.CountAsync()
    ];
    }

    [Fact]
    public async Task Seed_CreatesRecommendedAmountOfData()
    {
        await using var db = TestDb.Create();

        await DemoDataSeeder.SeedAsync(db, NullLogger.Instance);

        Assert.Equal(10, await db.Customers.CountAsync());
        Assert.Equal(15, await db.Materials.CountAsync());
        Assert.Equal(10, await db.Operations.CountAsync());
        Assert.Equal(10, await db.Products.CountAsync());
        Assert.Equal(5, await db.ProductMaterials.Select(x => x.ProductId).Distinct().CountAsync());
        Assert.Equal(5, await db.ProductOperations.Select(x => x.ProductId).Distinct().CountAsync());
        Assert.Equal(10, await db.Orders.CountAsync());
        Assert.Equal(24, await db.OrderItems.CountAsync());
    }

    [Fact]
    public async Task Seed_IsIdempotent()
    {
        await using var db = TestDb.Create();
        await DemoDataSeeder.SeedAsync(db, NullLogger.Instance);
        var before = await CountsAsync(db);

        await DemoDataSeeder.SeedAsync(db, NullLogger.Instance);

        Assert.Equal(before, await CountsAsync(db));
    }

    [Fact]
    public async Task Seed_DoesNotTouchExistingRecords()
    {
        await using var db = TestDb.Create();
        _ = db.Products.Add(TestDb.Product("VYR-001", "Upravený název", 1m));
        _ = await db.SaveChangesAsync();

        await DemoDataSeeder.SeedAsync(db, NullLogger.Instance);

        var product = await db.Products.SingleAsync(p => p.Code == "VYR-001");
        Assert.Equal("Upravený název", product.Name);
        Assert.Equal(10, await db.Products.CountAsync());
    }

    [Fact]
    public async Task SeededOrderTotals_MatchTheirItems()
    {
        await using var db = TestDb.Create();
        await DemoDataSeeder.SeedAsync(db, NullLogger.Instance);

        var orders = await db.Orders.Include(o => o.Items).ToListAsync();

        Assert.All(orders, o => Assert.Equal(o.Items.Sum(i => i.TotalPrice), o.TotalPrice));
        // 3 × 4 990 + 2 × 2 490
        Assert.Equal(19_950m, orders.Single(o => o.OrderNumber == "ZAK-90001").TotalPrice);
    }

    [Fact]
    public async Task SeededCatalog_ContainsProductsForAiScenarios()
    {
        await using var db = TestDb.Create();
        await DemoDataSeeder.SeedAsync(db, NullLogger.Instance);

        var names = await db.Products.Select(p => p.Name).ToListAsync();

        Assert.Contains("Psací stůl Alfa", names);
        Assert.Contains("Skříňka Beta", names);
        Assert.Equal(2, names.Count(n => n.StartsWith("Police")));
        Assert.DoesNotContain(names, n => n.Contains("Zeta"));
    }
}