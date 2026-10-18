using System.ComponentModel.DataAnnotations;

namespace FurnitureOrders.Shared.Operations;

public record OperationQuery(
    string? Search = null,
    bool? IsActive = null,
    string? Workplace = null,
    string? SortBy = null,
    bool Desc = false);

public record OperationListItemDto(
    int Id,
    string Code,
    string Name,
    string? Workplace,
    string? OperationType,
    int? DefaultSetupMinutes,
    int? DefaultRunMinutes,
    decimal? HourlyRate,
    bool IsActive);

public class OperationEditDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Kód operace je povinný.")]
    [StringLength(30, ErrorMessage = "Maximálně {1} znaků.")]
    public string Code { get; set; } = "";

    [Required(ErrorMessage = "Název je povinný.")]
    [StringLength(200, ErrorMessage = "Maximálně {1} znaků.")]
    public string Name { get; set; } = "";

    [StringLength(1000, ErrorMessage = "Maximálně {1} znaků.")]
    public string? Description { get; set; }

    [StringLength(100, ErrorMessage = "Maximálně {1} znaků.")]
    public string? Workplace { get; set; }

    [StringLength(100, ErrorMessage = "Maximálně {1} znaků.")]
    public string? OperationType { get; set; }

    [Range(0, 100000, ErrorMessage = "Doba přípravy nesmí být záporná.")]
    public int? DefaultSetupMinutes { get; set; }

    [Range(0, 100000, ErrorMessage = "Doba výroby nesmí být záporná.")]
    public int? DefaultRunMinutes { get; set; }

    [Range(typeof(decimal), "0", "999999999", ErrorMessage = "Hodinová sazba nesmí být záporná.")]
    public decimal? HourlyRate { get; set; }

    [StringLength(100, ErrorMessage = "Maximálně {1} znaků.")]
    public string? RequiredQualification { get; set; }

    [StringLength(2000, ErrorMessage = "Maximálně {1} znaků.")]
    public string? Note { get; set; }

    public bool IsActive { get; set; } = true;
}