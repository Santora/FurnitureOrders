namespace FurnitureOrders.Api.Domain;

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order? Order { get; set; }
    public int Sequence { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public string? Description { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal TotalPrice { get; set; }
    public DateOnly? RequiredDeliveryDate { get; set; }
    public string? Note { get; set; }
}