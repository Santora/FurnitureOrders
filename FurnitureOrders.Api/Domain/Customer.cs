using FurnitureOrders.Shared.Enums;

namespace FurnitureOrders.Api.Domain;

public class Customer
{
    public int Id { get; set; }
    public string CustomerNumber { get; set; } = "";
    public CustomerType Type { get; set; } = CustomerType.Company;
    public string Name { get; set; } = "";
    public string? CompanyId { get; set; }      // IČO
    public string? VatId { get; set; }          // DIČ
    public string? ContactPerson { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? Note { get; set; }
    public bool IsActive { get; set; } = true;
}
