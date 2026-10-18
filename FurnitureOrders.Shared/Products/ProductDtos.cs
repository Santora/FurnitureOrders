using System.ComponentModel.DataAnnotations;

namespace FurnitureOrders.Shared.Products;

public record ProductQuery(
    string? Search = null,
    bool? IsActive = null,
    string? Group = null,
    string? SortBy = null,
    bool Desc = false);

public record ProductListItemDto(
    int Id,
    string Code,
    string Name,
    string? ProductGroup,
    string? ProductType,
    string Unit,
    decimal SalesPrice,
    int? DeliveryDays,
    bool IsActive);

public class ProductEditDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Kód výrobku je povinný.")]
    [StringLength(30, ErrorMessage = "Maximálně {1} znaků.")]
    public string Code { get; set; } = "";

    [Required(ErrorMessage = "Název je povinný.")]
    [StringLength(200, ErrorMessage = "Maximálně {1} znaků.")]
    public string Name { get; set; } = "";

    [StringLength(1000, ErrorMessage = "Maximálně {1} znaků.")]
    public string? Description { get; set; }

    [StringLength(100, ErrorMessage = "Maximálně {1} znaků.")]
    public string? ProductGroup { get; set; }

    [StringLength(100, ErrorMessage = "Maximálně {1} znaků.")]
    public string? ProductType { get; set; }

    [Required(ErrorMessage = "Měrná jednotka je povinná.")]
    [StringLength(20, ErrorMessage = "Maximálně {1} znaků.")]
    public string Unit { get; set; } = "ks";

    [Required(ErrorMessage = "Prodejní cena je povinná.")]
    [Range(typeof(decimal), "0", "999999999", ErrorMessage = "Prodejní cena nesmí být záporná.")]
    public decimal? SalesPrice { get; set; }

    [Range(typeof(decimal), "0", "999999", ErrorMessage = "Šířka nesmí být záporná.")]
    public decimal? Width { get; set; }

    [Range(typeof(decimal), "0", "999999", ErrorMessage = "Výška nesmí být záporná.")]
    public decimal? Height { get; set; }

    [Range(typeof(decimal), "0", "999999", ErrorMessage = "Hloubka nesmí být záporná.")]
    public decimal? Depth { get; set; }

    [Range(typeof(decimal), "0", "999999", ErrorMessage = "Hmotnost nesmí být záporná.")]
    public decimal? Weight { get; set; }

    [Range(0, 10000, ErrorMessage = "Doba dodání nesmí být záporná.")]
    public int? DeliveryDays { get; set; }

    [StringLength(2000, ErrorMessage = "Maximálně {1} znaků.")]
    public string? Note { get; set; }

    public bool IsActive { get; set; } = true;
}