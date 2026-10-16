using System.ComponentModel.DataAnnotations;
using FurnitureOrders.Shared.Enums;
using FurnitureOrders.Shared.Validation;

namespace FurnitureOrders.Shared.Customers;

public record CustomerQuery(
    string? Search = null,
    bool? IsActive = null,
    string? SortBy = null,
    bool Desc = false);

public record CustomerListItemDto(
    int Id,
    string CustomerNumber,
    CustomerType Type,
    string Name,
    string? ContactPerson,
    string? Email,
    string? Phone,
    string? City,
    bool IsActive);

public record SetActiveRequest(bool IsActive);

public class CustomerEditDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Číslo zákazníka je povinné.")]
    [StringLength(30, ErrorMessage = "Maximálně {1} znaků.")]
    public string CustomerNumber { get; set; } = "";

    public CustomerType Type { get; set; } = CustomerType.Company;

    [Required(ErrorMessage = "Název je povinný.")]
    [StringLength(200, ErrorMessage = "Maximálně {1} znaků.")]
    public string Name { get; set; } = "";

    [StringLength(20, ErrorMessage = "Maximálně {1} znaků.")]
    public string? CompanyId { get; set; }

    [StringLength(20, ErrorMessage = "Maximálně {1} znaků.")]
    public string? VatId { get; set; }

    [StringLength(150, ErrorMessage = "Maximálně {1} znaků.")]
    public string? ContactPerson { get; set; }

    [OptionalEmail]
    [StringLength(200, ErrorMessage = "Maximálně {1} znaků.")]
    public string? Email { get; set; }

    [StringLength(50, ErrorMessage = "Maximálně {1} znaků.")]
    public string? Phone { get; set; }

    [StringLength(200, ErrorMessage = "Maximálně {1} znaků.")]
    public string? Street { get; set; }

    [StringLength(100, ErrorMessage = "Maximálně {1} znaků.")]
    public string? City { get; set; }

    [StringLength(20, ErrorMessage = "Maximálně {1} znaků.")]
    public string? PostalCode { get; set; }

    [StringLength(100, ErrorMessage = "Maximálně {1} znaků.")]
    public string? Country { get; set; }

    [StringLength(2000, ErrorMessage = "Maximálně {1} znaků.")]
    public string? Note { get; set; }

    public bool IsActive { get; set; } = true;
}