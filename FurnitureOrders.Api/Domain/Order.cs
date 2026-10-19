using FurnitureOrders.Shared.Enums;

namespace FurnitureOrders.Api.Domain;

public class Order
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = "";
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public DateOnly CreatedDate { get; set; }
    public DateOnly RequiredDeliveryDate { get; set; }
    public DateOnly? ActualCompletionDate { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.New;
    public string? ContactPerson { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? ShippingMethod { get; set; }
    public decimal TotalPrice { get; set; }
    public string? Note { get; set; }
    public DateTime LastModifiedAt { get; set; }
    public List<OrderItem> Items { get; set; } = [];
}