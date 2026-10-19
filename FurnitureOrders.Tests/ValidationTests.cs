using FurnitureOrders.Api.Services;
using FurnitureOrders.Shared.Customers;
using FurnitureOrders.Shared.Materials;
using FurnitureOrders.Shared.Orders;
using FurnitureOrders.Shared.Products;

namespace FurnitureOrders.Tests;

public class ValidationTests
{
    [Fact]
    public void Customer_RequiresName()
    {
        var errors = ModelValidator.Validate(new CustomerEditDto { CustomerNumber = "Z1", Name = "" });

        Assert.Contains(nameof(CustomerEditDto.Name), errors.Keys);
    }

    [Theory]
    [InlineData("neplatny", true)]
    [InlineData("a@b", false)]
    [InlineData("jana@example.com", false)]
    [InlineData("", false)]     // prázdný e-mail je v pořádku, kontroluje se jen vyplněný
    [InlineData(null, false)]
    public void Customer_EmailIsCheckedOnlyWhenFilled(string? email, bool expectError)
    {
        var errors = ModelValidator.Validate(new CustomerEditDto { Name = "Test", Email = email });

        Assert.Equal(expectError, errors.ContainsKey(nameof(CustomerEditDto.Email)));
    }

    [Fact]
    public void Material_RequiresCodeNameAndUnit_AndRejectsNegativePrice()
    {
        var errors = ModelValidator.Validate(new MaterialEditDto { Code = "", Name = "", Unit = "", PurchasePrice = -1 });

        Assert.Contains(nameof(MaterialEditDto.Code), errors.Keys);
        Assert.Contains(nameof(MaterialEditDto.Name), errors.Keys);
        Assert.Contains(nameof(MaterialEditDto.Unit), errors.Keys);
        Assert.Contains(nameof(MaterialEditDto.PurchasePrice), errors.Keys);
    }

    [Fact]
    public void Product_RequiresPrice_AndRejectsNegativeDimensions()
    {
        var errors = ModelValidator.Validate(new ProductEditDto { Code = "P", Name = "P", Unit = "ks", Width = -1 });

        Assert.Contains(nameof(ProductEditDto.SalesPrice), errors.Keys);
        Assert.Contains(nameof(ProductEditDto.Width), errors.Keys);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void ProductMaterial_QuantityMustBeGreaterThanZero(int quantity)
    {
        var errors = ModelValidator.Validate(new ProductMaterialEditDto { MaterialId = 1, Quantity = quantity });

        Assert.Contains(nameof(ProductMaterialEditDto.Quantity), errors.Keys);
    }

    [Fact]
    public void OrderItem_ValidatesQuantityPriceAndDiscount()
    {
        var errors = ModelValidator.Validate(new OrderItemEditDto
        {
            ProductId = 1,
            Quantity = 0,
            UnitPrice = -5,
            DiscountPercent = 150
        });

        Assert.Contains(nameof(OrderItemEditDto.Quantity), errors.Keys);
        Assert.Contains(nameof(OrderItemEditDto.UnitPrice), errors.Keys);
        Assert.Contains(nameof(OrderItemEditDto.DiscountPercent), errors.Keys);
    }

    [Fact]
    public void Order_RequiresCustomerAndDates()
    {
        var errors = ModelValidator.Validate(new OrderEditDto());

        Assert.Contains(nameof(OrderEditDto.CustomerId), errors.Keys);
        Assert.Contains(nameof(OrderEditDto.CreatedDate), errors.Keys);
        Assert.Contains(nameof(OrderEditDto.RequiredDeliveryDate), errors.Keys);
    }
}