using FurnitureOrders.Api.Data;
using FurnitureOrders.Api.Domain;
using FurnitureOrders.Shared.Customers;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FurnitureOrders.Api.Services;

public interface ICustomerService
{
    Task<IReadOnlyList<CustomerListItemDto>> ListAsync(CustomerQuery query, CancellationToken ct);
    Task<CustomerEditDto?> GetAsync(int id, CancellationToken ct);
    Task<OperationResult<CustomerEditDto>> CreateAsync(CustomerEditDto dto, CancellationToken ct);
    Task<OperationResult<CustomerEditDto>> UpdateAsync(int id, CustomerEditDto dto, CancellationToken ct);
    Task<OperationResult<CustomerEditDto>> SetActiveAsync(int id, bool isActive, CancellationToken ct);
}

public class CustomerService(AppDbContext db) : ICustomerService
{
    private const string DuplicateMessage = "Zákazník s tímto číslem již existuje.";

    public async Task<IReadOnlyList<CustomerListItemDto>> ListAsync(CustomerQuery q, CancellationToken ct)
    {
        var query = db.Customers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim();
            query = query.Where(c => c.CustomerNumber.Contains(s) || c.Name.Contains(s));
        }

        if (q.IsActive is { } active)
            query = query.Where(c => c.IsActive == active);

        IOrderedQueryable<Customer> ordered = (q.SortBy?.ToLowerInvariant(), q.Desc) switch
        {
            ("name", false) => query.OrderBy(c => c.Name),
            ("name", true) => query.OrderByDescending(c => c.Name),
            ("city", false) => query.OrderBy(c => c.City),
            ("city", true) => query.OrderByDescending(c => c.City),
            ("email", false) => query.OrderBy(c => c.Email),
            ("email", true) => query.OrderByDescending(c => c.Email),
            (_, true) => query.OrderByDescending(c => c.CustomerNumber),
            _ => query.OrderBy(c => c.CustomerNumber)
        };

        return await ordered
            .Select(c => new CustomerListItemDto(
                c.Id, c.CustomerNumber, c.Type, c.Name, c.ContactPerson,
                c.Email, c.Phone, c.City, c.IsActive))
            .ToListAsync(ct);
    }

    public async Task<CustomerEditDto?> GetAsync(int id, CancellationToken ct)
    {
        var c = await db.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return c is null ? null : ToDto(c);
    }

    public async Task<OperationResult<CustomerEditDto>> CreateAsync(CustomerEditDto dto, CancellationToken ct)
    {
        Normalize(dto);
        var errors = ModelValidator.Validate(dto);
        if (errors.Count > 0)
            return OperationResult<CustomerEditDto>.Invalid(errors);

        if (await db.Customers.AnyAsync(c => c.CustomerNumber == dto.CustomerNumber, ct))
            return Duplicate();

        var entity = new Customer();
        Apply(entity, dto);
        _ = db.Customers.Add(entity);

        return await SaveAsync(entity, ct);
    }

    public async Task<OperationResult<CustomerEditDto>> UpdateAsync(int id, CustomerEditDto dto, CancellationToken ct)
    {
        var entity = await db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (entity is null)
            return OperationResult<CustomerEditDto>.NotFound();

        Normalize(dto);
        var errors = ModelValidator.Validate(dto);
        if (errors.Count > 0)
            return OperationResult<CustomerEditDto>.Invalid(errors);

        if (await db.Customers.AnyAsync(c => c.CustomerNumber == dto.CustomerNumber && c.Id != id, ct))
            return Duplicate();

        Apply(entity, dto);
        return await SaveAsync(entity, ct);
    }

    public async Task<OperationResult<CustomerEditDto>> SetActiveAsync(int id, bool isActive, CancellationToken ct)
    {
        var entity = await db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (entity is null)
            return OperationResult<CustomerEditDto>.NotFound();

        entity.IsActive = isActive;
        _ = await db.SaveChangesAsync(ct);
        return OperationResult<CustomerEditDto>.Ok(ToDto(entity));
    }

    private async Task<OperationResult<CustomerEditDto>> SaveAsync(Customer entity, CancellationToken ct)
    {
        try
        {
            _ = await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            // souběh: unikátní index zachytil duplicitu, kterou předchozí kontrola nestihla
            return Duplicate();
        }
        return OperationResult<CustomerEditDto>.Ok(ToDto(entity));
    }

    private static OperationResult<CustomerEditDto> Duplicate()
    {
        return OperationResult<CustomerEditDto>.Invalid(new Dictionary<string, string[]>
        {
            [nameof(CustomerEditDto.CustomerNumber)] = [DuplicateMessage]
        });
    }

    private static string? Clean(string? s)
    {
        return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    }

    private static void Normalize(CustomerEditDto d)
    {
        d.CustomerNumber = d.CustomerNumber?.Trim() ?? "";
        d.Name = d.Name?.Trim() ?? "";
        d.CompanyId = Clean(d.CompanyId);
        d.VatId = Clean(d.VatId);
        d.ContactPerson = Clean(d.ContactPerson);
        d.Email = Clean(d.Email);
        d.Phone = Clean(d.Phone);
        d.Street = Clean(d.Street);
        d.City = Clean(d.City);
        d.PostalCode = Clean(d.PostalCode);
        d.Country = Clean(d.Country);
        d.Note = Clean(d.Note);
    }

    private static void Apply(Customer e, CustomerEditDto d)
    {
        e.CustomerNumber = d.CustomerNumber;
        e.Type = d.Type;
        e.Name = d.Name;
        e.CompanyId = d.CompanyId;
        e.VatId = d.VatId;
        e.ContactPerson = d.ContactPerson;
        e.Email = d.Email;
        e.Phone = d.Phone;
        e.Street = d.Street;
        e.City = d.City;
        e.PostalCode = d.PostalCode;
        e.Country = d.Country;
        e.Note = d.Note;
        e.IsActive = d.IsActive;
    }

    private static CustomerEditDto ToDto(Customer e)
    {
        return new()
        {
            Id = e.Id,
            CustomerNumber = e.CustomerNumber,
            Type = e.Type,
            Name = e.Name,
            CompanyId = e.CompanyId,
            VatId = e.VatId,
            ContactPerson = e.ContactPerson,
            Email = e.Email,
            Phone = e.Phone,
            Street = e.Street,
            City = e.City,
            PostalCode = e.PostalCode,
            Country = e.Country,
            Note = e.Note,
            IsActive = e.IsActive
        };
    }
}