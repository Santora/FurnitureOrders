using FurnitureOrders.Api.Data;
using FurnitureOrders.Api.Domain;
using FurnitureOrders.Api.Services;
using FurnitureOrders.Shared.Enums;
using FurnitureOrders.Shared.Orders;
using Microsoft.EntityFrameworkCore;

namespace FurnitureOrders.Tests;

public class OrderServiceTests
{
    private static readonly FixedTimeProvider Clock = new(new DateTimeOffset(2026, 10, 19, 8, 0, 0, TimeSpan.Zero));

    private static int _counter;

    private static OrderEditDto NewOrder(int customerId, params OrderItemEditDto[] items)
    {
        return new()
        {
            OrderNumber = $"T-{Interlocked.Increment(ref _counter)}",
            CustomerId = customerId,
            CreatedDate = new DateOnly(2026, 10, 19),
            RequiredDeliveryDate = new DateOnly(2026, 11, 30),
            Status = OrderStatus.New,
            Items = items.ToList()
        };
    }

    private static OrderItemEditDto Item(int productId, decimal qty, decimal price, decimal? discount = null)
    {
        return new() { ProductId = productId, Quantity = qty, UnitPrice = price, DiscountPercent = discount };
    }

    private static async Task<(AppDbContext Db, OrderService Svc, int CustomerId, int ProductId)> SetupAsync()
    {
        var db = TestDb.Create();
        var customerId = await TestDb.SeedCatalogAsync(db);
        var productId = (await db.Products.FirstAsync(p => p.Code == "VYR-001")).Id;
        return (db, new OrderService(db, Clock), customerId, productId);
    }

    [Fact]
    public async Task Prices_AreComputedByServer_AndClientValuesAreIgnored()
    {
        var (db, svc, customerId, productId) = await SetupAsync();
        await using var dbScope = db;

        var dto = NewOrder(customerId, Item(productId, 3, 1000, 10), Item(productId, 2, 450.50m));
        dto.TotalPrice = 1;
        dto.Items[0].TotalPrice = 1;

        var result = await svc.CreateAsync(dto, default);

        Assert.Equal(OperationStatus.Ok, result.Status);
        Assert.Equal(2700m, result.Value!.Items[0].TotalPrice);
        Assert.Equal(901m, result.Value.Items[1].TotalPrice);
        Assert.Equal(3601m, result.Value.TotalPrice);
    }

    [Fact]
    public async Task OrderWithoutItems_IsRejected()
    {
        var (db, svc, customerId, _) = await SetupAsync();
        await using var dbScope = db;

        var result = await svc.CreateAsync(NewOrder(customerId), default);

        Assert.Equal(OperationStatus.Invalid, result.Status);
        Assert.Contains("Items", result.Errors!.Keys);
        Assert.Empty(db.Orders);
    }

    [Fact]
    public async Task ItemErrors_AreReportedPerRow()
    {
        var (db, svc, customerId, productId) = await SetupAsync();
        await using var dbScope = db;

        var result = await svc.CreateAsync(
            NewOrder(customerId, Item(productId, 1, 100), Item(productId, 0, 100, 150)), default);

        Assert.Equal(OperationStatus.Invalid, result.Status);
        Assert.Contains("Items[1].Quantity", result.Errors!.Keys);
        Assert.Contains("Items[1].DiscountPercent", result.Errors.Keys);
        Assert.DoesNotContain("Items[0].Quantity", result.Errors.Keys);
    }

    [Fact]
    public async Task InactiveCustomer_IsRejected()
    {
        var (db, svc, _, productId) = await SetupAsync();
        await using var dbScope = db;
        var inactive = new Customer { CustomerNumber = "T-NEAKT", Name = "Neaktivní", IsActive = false };
        _ = db.Customers.Add(inactive);
        _ = await db.SaveChangesAsync();

        var result = await svc.CreateAsync(NewOrder(inactive.Id, Item(productId, 1, 100)), default);

        Assert.Contains(nameof(OrderEditDto.CustomerId), result.Errors!.Keys);
    }

    [Fact]
    public async Task InactiveProduct_IsRejected()
    {
        var (db, svc, customerId, _) = await SetupAsync();
        await using var dbScope = db;
        var inactive = TestDb.Product("VYR-NEAKT", "Neaktivní", 10m, active: false);
        _ = db.Products.Add(inactive);
        _ = await db.SaveChangesAsync();

        var result = await svc.CreateAsync(NewOrder(customerId, Item(inactive.Id, 1, 10)), default);

        Assert.Contains("Items[0].ProductId", result.Errors!.Keys);
    }

    [Fact]
    public async Task RequiredDateBeforeCreatedDate_IsRejected()
    {
        var (db, svc, customerId, productId) = await SetupAsync();
        await using var dbScope = db;
        var dto = NewOrder(customerId, Item(productId, 1, 100));
        dto.RequiredDeliveryDate = new DateOnly(2026, 10, 1);

        var result = await svc.CreateAsync(dto, default);

        Assert.Contains(nameof(OrderEditDto.RequiredDeliveryDate), result.Errors!.Keys);
    }

    [Fact]
    public async Task DuplicateOrderNumber_IsRejected()
    {
        var (db, svc, customerId, productId) = await SetupAsync();
        await using var dbScope = db;
        var first = NewOrder(customerId, Item(productId, 1, 100));
        _ = await svc.CreateAsync(first, default);

        var second = NewOrder(customerId, Item(productId, 1, 100));
        second.OrderNumber = first.OrderNumber;
        var result = await svc.CreateAsync(second, default);

        Assert.Contains(nameof(OrderEditDto.OrderNumber), result.Errors!.Keys);
    }

    [Fact]
    public async Task Update_RemovesMissingItems_AndRecalculates()
    {
        var (db, svc, customerId, productId) = await SetupAsync();
        await using var dbScope = db;
        var created = (await svc.CreateAsync(
            NewOrder(customerId, Item(productId, 3, 1000, 10), Item(productId, 2, 450.50m)), default)).Value!;

        var dto = created;
        dto.Items.RemoveAt(1);
        dto.Items[0].Quantity = 4;
        dto.Items[0].DiscountPercent = 0;
        var result = await svc.UpdateAsync(dto.Id, dto, default);

        Assert.Equal(OperationStatus.Ok, result.Status);
        Assert.Equal(4000m, result.Value!.TotalPrice);
        Assert.Equal(1, await db.OrderItems.CountAsync(i => i.OrderId == dto.Id));
    }

    [Fact]
    public async Task Update_WithForeignItemId_IsRejected()
    {
        var (db, svc, customerId, productId) = await SetupAsync();
        await using var dbScope = db;
        var created = (await svc.CreateAsync(NewOrder(customerId, Item(productId, 1, 100)), default)).Value!;

        created.Items[0].Id = 999_999;
        var result = await svc.UpdateAsync(created.Id, created, default);

        Assert.Contains("Items[0].Id", result.Errors!.Keys);
    }

    [Fact]
    public async Task InvalidStatus_IsRejected()
    {
        var (db, svc, customerId, productId) = await SetupAsync();
        await using var dbScope = db;
        var created = (await svc.CreateAsync(NewOrder(customerId, Item(productId, 1, 100)), default)).Value!;

        var result = await svc.SetStatusAsync(created.Id, (OrderStatus)99, default);

        Assert.Equal(OperationStatus.Invalid, result.Status);
    }
}