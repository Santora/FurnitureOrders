using FurnitureOrders.Shared.Enums;

namespace FurnitureOrders.Api.Domain;

public class Material
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public MaterialType Type { get; set; } = MaterialType.Sheet;
    public string Unit { get; set; } = "";
    public decimal? PurchasePrice { get; set; }
    public string? Supplier { get; set; }
    public string? StockNumber { get; set; }
    public decimal? MinOrderQuantity { get; set; }
    public string? ColorOrDecor { get; set; }
    public string? Note { get; set; }
    public bool IsActive { get; set; } = true;
}