using System.ComponentModel.DataAnnotations;
using FurnitureOrders.Shared.Enums;

namespace FurnitureOrders.Shared.Materials;

public record MaterialQuery(
    string? Search = null,
    bool? IsActive = null,
    MaterialType? Type = null,
    string? SortBy = null,
    bool Desc = false);

public record MaterialListItemDto(
    int Id,
    string Code,
    string Name,
    MaterialType Type,
    string Unit,
    decimal? PurchasePrice,
    string? Supplier,
    bool IsActive);

public class MaterialEditDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Kód materiálu je povinný.")]
    [StringLength(30, ErrorMessage = "Maximálně {1} znaků.")]
    public string Code { get; set; } = "";

    [Required(ErrorMessage = "Název je povinný.")]
    [StringLength(200, ErrorMessage = "Maximálně {1} znaků.")]
    public string Name { get; set; } = "";

    [StringLength(1000, ErrorMessage = "Maximálně {1} znaků.")]
    public string? Description { get; set; }

    public MaterialType Type { get; set; } = MaterialType.Sheet;

    [Required(ErrorMessage = "Měrná jednotka je povinná.")]
    [StringLength(20, ErrorMessage = "Maximálně {1} znaků.")]
    public string Unit { get; set; } = "";

    [Range(typeof(decimal), "0", "999999999", ErrorMessage = "Nákupní cena nesmí být záporná.")]
    public decimal? PurchasePrice { get; set; }

    [StringLength(200, ErrorMessage = "Maximálně {1} znaků.")]
    public string? Supplier { get; set; }

    [StringLength(50, ErrorMessage = "Maximálně {1} znaků.")]
    public string? StockNumber { get; set; }

    [Range(typeof(decimal), "0", "999999999", ErrorMessage = "Minimální množství nesmí být záporné.")]
    public decimal? MinOrderQuantity { get; set; }

    [StringLength(100, ErrorMessage = "Maximálně {1} znaků.")]
    public string? ColorOrDecor { get; set; }

    [StringLength(2000, ErrorMessage = "Maximálně {1} znaků.")]
    public string? Note { get; set; }

    public bool IsActive { get; set; } = true;
}