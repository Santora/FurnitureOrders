using System.ComponentModel.DataAnnotations;
using FurnitureOrders.Shared.Enums;
using FurnitureOrders.Shared.Validation;

namespace FurnitureOrders.Shared.Orders;

public record OrderQuery(
    string? Search = null,
    OrderStatus? Status = null,
    int? CustomerId = null,
    DateOnly? From = null,
    DateOnly? To = null,
    string? SortBy = null,
    bool Desc = false);

public record OrderListItemDto(
    int Id,
    string OrderNumber,
    int CustomerId,
    string CustomerName,
    DateOnly CreatedDate,
    DateOnly RequiredDeliveryDate,
    OrderStatus Status,
    decimal TotalPrice);

public record SetOrderStatusRequest(OrderStatus Status);

public class OrderItemEditDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Výrobek je povinný.")]
    public int? ProductId { get; set; }

    [StringLength(500, ErrorMessage = "Maximálně {1} znaků.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Množství je povinné.")]
    [Range(typeof(decimal), "0.001", "999999999",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true,
        ErrorMessage = "Množství musí být větší než nula.")]
    public decimal? Quantity { get; set; }

    [StringLength(20, ErrorMessage = "Maximálně {1} znaků.")]
    public string? Unit { get; set; }

    [Required(ErrorMessage = "Jednotková cena je povinná.")]
    [Range(typeof(decimal), "0", "999999999",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true,
        ErrorMessage = "Jednotková cena nesmí být záporná.")]
    public decimal? UnitPrice { get; set; }

    [Range(typeof(decimal), "0", "100",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true,
        ErrorMessage = "Sleva musí být v rozsahu 0 až 100 %.")]
    public decimal? DiscountPercent { get; set; }

    public DateOnly? RequiredDeliveryDate { get; set; }

    [StringLength(1000, ErrorMessage = "Maximálně {1} znaků.")]
    public string? Note { get; set; }

    // pouze pro čtení: plní server
    public int Sequence { get; set; }
    public decimal TotalPrice { get; set; }
    public string? ProductCode { get; set; }
    public string? ProductName { get; set; }
}

public class OrderEditDto
{
    public int Id { get; set; }

    [StringLength(30, ErrorMessage = "Maximálně {1} znaků.")]
    public string OrderNumber { get; set; } = "";

    [Required(ErrorMessage = "Zákazník je povinný.")]
    public int? CustomerId { get; set; }

    [Required(ErrorMessage = "Datum vytvoření je povinné.")]
    public DateOnly? CreatedDate { get; set; }

    [Required(ErrorMessage = "Požadovaný termín dodání je povinný.")]
    public DateOnly? RequiredDeliveryDate { get; set; }

    public DateOnly? ActualCompletionDate { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.New;

    [StringLength(150, ErrorMessage = "Maximálně {1} znaků.")]
    public string? ContactPerson { get; set; }

    [OptionalEmail]
    [StringLength(200, ErrorMessage = "Maximálně {1} znaků.")]
    public string? Email { get; set; }

    [StringLength(50, ErrorMessage = "Maximálně {1} znaků.")]
    public string? Phone { get; set; }

    [StringLength(500, ErrorMessage = "Maximálně {1} znaků.")]
    public string? DeliveryAddress { get; set; }

    [StringLength(100, ErrorMessage = "Maximálně {1} znaků.")]
    public string? ShippingMethod { get; set; }

    [StringLength(2000, ErrorMessage = "Maximálně {1} znaků.")]
    public string? Note { get; set; }

    public List<OrderItemEditDto> Items { get; set; } = [];

    // pouze pro čtení: plní server
    public decimal TotalPrice { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    public string? CustomerName { get; set; }
}