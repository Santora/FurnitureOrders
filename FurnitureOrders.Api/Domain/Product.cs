namespace FurnitureOrders.Api.Domain;

public class Product
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? ProductGroup { get; set; }
    public string? ProductType { get; set; }
    public string Unit { get; set; } = "ks";
    public decimal SalesPrice { get; set; }
    public decimal? Width { get; set; }
    public decimal? Height { get; set; }
    public decimal? Depth { get; set; }
    public decimal? Weight { get; set; }
    public int? DeliveryDays { get; set; }
    public string? Note { get; set; }
    public bool IsActive { get; set; } = true;
}