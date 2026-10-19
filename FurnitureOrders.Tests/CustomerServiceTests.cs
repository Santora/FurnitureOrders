using FurnitureOrders.Api.Services;
using FurnitureOrders.Shared.Customers;

namespace FurnitureOrders.Tests;

public class CustomerServiceTests
{
    [Fact]
    public async Task DuplicateCustomerNumber_IsRejected()
    {
        await using var db = TestDb.Create();
        var svc = new CustomerService(db);

        var first = await svc.CreateAsync(new CustomerEditDto { CustomerNumber = "Z1", Name = "A" }, default);
        var second = await svc.CreateAsync(new CustomerEditDto { CustomerNumber = "Z1", Name = "B" }, default);

        Assert.Equal(OperationStatus.Ok, first.Status);
        Assert.Equal(OperationStatus.Invalid, second.Status);
        Assert.Contains(nameof(CustomerEditDto.CustomerNumber), second.Errors!.Keys);
    }

    [Fact]
    public async Task UpdateToNumberOfAnotherCustomer_IsRejected()
    {
        await using var db = TestDb.Create();
        var svc = new CustomerService(db);
        _ = await svc.CreateAsync(new CustomerEditDto { CustomerNumber = "Z1", Name = "A" }, default);
        var other = await svc.CreateAsync(new CustomerEditDto { CustomerNumber = "Z2", Name = "B" }, default);

        var dto = other.Value!;
        dto.CustomerNumber = "Z1";
        var result = await svc.UpdateAsync(dto.Id, dto, default);

        Assert.Equal(OperationStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task UpdateKeepingOwnNumber_IsAllowed()
    {
        await using var db = TestDb.Create();
        var svc = new CustomerService(db);
        var created = await svc.CreateAsync(new CustomerEditDto { CustomerNumber = "Z1", Name = "A" }, default);

        var dto = created.Value!;
        dto.Name = "A upraveno";
        var result = await svc.UpdateAsync(dto.Id, dto, default);

        Assert.Equal(OperationStatus.Ok, result.Status);
    }

    [Fact]
    public async Task MissingName_IsRejectedBeforeSave()
    {
        await using var db = TestDb.Create();
        var svc = new CustomerService(db);

        var result = await svc.CreateAsync(new CustomerEditDto { CustomerNumber = "Z1", Name = " " }, default);

        Assert.Equal(OperationStatus.Invalid, result.Status);
        Assert.Empty(db.Customers);
    }

    [Fact]
    public async Task Deactivation_KeepsTheRecord()
    {
        await using var db = TestDb.Create();
        var svc = new CustomerService(db);
        var created = await svc.CreateAsync(new CustomerEditDto { CustomerNumber = "Z1", Name = "A" }, default);

        var result = await svc.SetActiveAsync(created.Value!.Id, false, default);

        Assert.False(result.Value!.IsActive);
        _ = Assert.Single(db.Customers);
    }
}