namespace FurnitureOrders.Api.Domain;

public class Operation
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? Workplace { get; set; }
    public string? OperationType { get; set; }
    public int? DefaultSetupMinutes { get; set; }
    public int? DefaultRunMinutes { get; set; }
    public decimal? HourlyRate { get; set; }
    public string? RequiredQualification { get; set; }
    public string? Note { get; set; }
    public bool IsActive { get; set; } = true;
}