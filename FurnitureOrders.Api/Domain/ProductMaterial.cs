namespace FurnitureOrders.Api.Domain;

public class ProductMaterial
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public int MaterialId { get; set; }
    public Material? Material { get; set; }
    public int Sequence { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "";
    public decimal? WastePercent { get; set; }
    public string? Note { get; set; }
}