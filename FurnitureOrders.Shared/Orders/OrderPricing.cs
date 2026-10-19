namespace FurnitureOrders.Shared.Orders;

public static class OrderPricing
{
    /// množství × jednotková cena × (1 − sleva / 100), zaokrouhleno na 2 desetinná místa
    public static decimal ItemTotal(decimal quantity, decimal unitPrice, decimal discountPercent)
    {
        return Math.Round(quantity * unitPrice * (1 - discountPercent / 100m), 2, MidpointRounding.AwayFromZero);
    }

    /// součet již zaokrouhlených cen položek
    public static decimal OrderTotal(IEnumerable<decimal> itemTotals)
    {
        return itemTotals.Sum();
    }
}