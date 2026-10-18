namespace FurnitureOrders.Api.Domain;

public class ProductOperation
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public int OperationId { get; set; }
    public Operation? Operation { get; set; }
    public int Sequence { get; set; }
    public string? Workplace { get; set; }
    public int? SetupMinutes { get; set; }
    public decimal? RunMinutesPerUnit { get; set; }
    public string? WorkInstructions { get; set; }
    public string? Note { get; set; }
}