using System.ComponentModel.DataAnnotations;

namespace FurnitureOrders.Shared.Products;

public record ProductMaterialDto(
    int Id, int ProductId, int Sequence,
    int MaterialId, string MaterialCode, string MaterialName,
    decimal Quantity, string Unit, decimal? WastePercent, string? Note);

public class ProductMaterialEditDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Materiál je povinný.")]
    public int? MaterialId { get; set; }

    [Range(1, 10000, ErrorMessage = "Pořadí musí být v rozsahu 1 až 10000.")]
    public int? Sequence { get; set; }

    [Required(ErrorMessage = "Množství je povinné.")]
    [Range(typeof(decimal), "0.001", "999999999",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true,
        ErrorMessage = "Množství musí být větší než nula.")]
    public decimal? Quantity { get; set; }

    [StringLength(20, ErrorMessage = "Maximálně {1} znaků.")]
    public string? Unit { get; set; }

    [Range(typeof(decimal), "0", "100",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true,
        ErrorMessage = "Procento odpadu musí být v rozsahu 0 až 100.")]
    public decimal? WastePercent { get; set; }

    [StringLength(1000, ErrorMessage = "Maximálně {1} znaků.")]
    public string? Note { get; set; }
}

public record ProductOperationDto(
    int Id, int ProductId, int Sequence,
    int OperationId, string OperationCode, string OperationName,
    string? Workplace, int? SetupMinutes, decimal? RunMinutesPerUnit,
    string? WorkInstructions, string? Note);

public class ProductOperationEditDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Operace je povinná.")]
    public int? OperationId { get; set; }

    [Range(1, 10000, ErrorMessage = "Pořadí musí být v rozsahu 1 až 10000.")]
    public int? Sequence { get; set; }

    [StringLength(100, ErrorMessage = "Maximálně {1} znaků.")]
    public string? Workplace { get; set; }

    [Range(0, 100000, ErrorMessage = "Doba přípravy nesmí být záporná.")]
    public int? SetupMinutes { get; set; }

    [Range(typeof(decimal), "0", "100000",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true,
        ErrorMessage = "Doba výroby jednoho kusu nesmí být záporná.")]
    public decimal? RunMinutesPerUnit { get; set; }

    [StringLength(4000, ErrorMessage = "Maximálně {1} znaků.")]
    public string? WorkInstructions { get; set; }

    [StringLength(1000, ErrorMessage = "Maximálně {1} znaků.")]
    public string? Note { get; set; }
}