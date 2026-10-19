using FurnitureOrders.Shared.Orders;

namespace FurnitureOrders.Tests;

public class OrderPricingTests
{
    [Theory]
    [InlineData(3, 1000, 10, 2700)]
    [InlineData(2, 450.5, 0, 901)]
    [InlineData(1, 100, 100, 0)]
    [InlineData(1.5, 200, 50, 150)]
    [InlineData(3, 33.333, 0, 100)]        // 99.999 se zaokrouhlí na 100.00
    [InlineData(1, 0.125, 0, 0.13)]        // půlka se zaokrouhluje nahoru
    public void ItemTotal_AppliesFormulaAndRounding(double quantity, double unitPrice, double discount, double expected)
    {
        var result = OrderPricing.ItemTotal((decimal)quantity, (decimal)unitPrice, (decimal)discount);

        Assert.Equal((decimal)expected, result);
    }

    [Fact]
    public void OrderTotal_SumsItemTotals()
    {
        var total = OrderPricing.OrderTotal([2700m, 901m, 0.13m]);

        Assert.Equal(3601.13m, total);
    }

    [Fact]
    public void OrderTotal_OfNoItems_IsZero()
    {
        Assert.Equal(0m, OrderPricing.OrderTotal([]));
    }
}