using FurnitureOrders.Api.Data;
using FurnitureOrders.Api.Domain;
using FurnitureOrders.Shared.Products;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FurnitureOrders.Api.Services;

public interface IProductService
{
    Task<IReadOnlyList<ProductListItemDto>> ListAsync(ProductQuery query, CancellationToken ct);
    Task<ProductEditDto?> GetAsync(int id, CancellationToken ct);
    Task<OperationResult<ProductEditDto>> CreateAsync(ProductEditDto dto, CancellationToken ct);
    Task<OperationResult<ProductEditDto>> UpdateAsync(int id, ProductEditDto dto, CancellationToken ct);
    Task<OperationResult<ProductEditDto>> SetActiveAsync(int id, bool isActive, CancellationToken ct);
    Task<IReadOnlyList<string>> GroupsAsync(CancellationToken ct);
}

public class ProductService(AppDbContext db) : IProductService
{
    private const string DuplicateMessage = "Výrobek s tímto kódem již existuje.";

    public async Task<IReadOnlyList<ProductListItemDto>> ListAsync(ProductQuery q, CancellationToken ct)
    {
        var query = db.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim();
            query = query.Where(p => p.Code.Contains(s) || p.Name.Contains(s));
        }
        if (q.IsActive is { } active) query = query.Where(p => p.IsActive == active);
        if (!string.IsNullOrWhiteSpace(q.Group))
        {
            var g = q.Group.Trim();
            query = query.Where(p => p.ProductGroup == g);
        }

        IOrderedQueryable<Product> ordered = (q.SortBy?.ToLowerInvariant(), q.Desc) switch
        {
            ("name", false) => query.OrderBy(p => p.Name),
            ("name", true) => query.OrderByDescending(p => p.Name),
            ("group", false) => query.OrderBy(p => p.ProductGroup),
            ("group", true) => query.OrderByDescending(p => p.ProductGroup),
            ("price", false) => query.OrderBy(p => p.SalesPrice),
            ("price", true) => query.OrderByDescending(p => p.SalesPrice),
            (_, true) => query.OrderByDescending(p => p.Code),
            _ => query.OrderBy(p => p.Code)
        };

        return await ordered
            .Select(p => new ProductListItemDto(
                p.Id, p.Code, p.Name, p.ProductGroup, p.ProductType,
                p.Unit, p.SalesPrice, p.DeliveryDays, p.IsActive))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<string>> GroupsAsync(CancellationToken ct)
    {
        return await db.Products.AsNoTracking()
            .Where(p => p.ProductGroup != null && p.ProductGroup != "")
            .Select(p => p.ProductGroup!)
            .Distinct()
            .OrderBy(g => g)
            .ToListAsync(ct);
    }

    public async Task<ProductEditDto?> GetAsync(int id, CancellationToken ct)
    {
        var p = await db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return p is null ? null : ToDto(p);
    }

    public async Task<OperationResult<ProductEditDto>> CreateAsync(ProductEditDto dto, CancellationToken ct)
    {
        Normalize(dto);
        var errors = ModelValidator.Validate(dto);
        if (errors.Count > 0) return OperationResult<ProductEditDto>.Invalid(errors);

        if (await db.Products.AnyAsync(p => p.Code == dto.Code, ct)) return Duplicate();

        var entity = new Product();
        Apply(entity, dto);
        _ = db.Products.Add(entity);
        return await SaveAsync(entity, ct);
    }

    public async Task<OperationResult<ProductEditDto>> UpdateAsync(int id, ProductEditDto dto, CancellationToken ct)
    {
        var entity = await db.Products.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (entity is null) return OperationResult<ProductEditDto>.NotFound();

        Normalize(dto);
        var errors = ModelValidator.Validate(dto);
        if (errors.Count > 0) return OperationResult<ProductEditDto>.Invalid(errors);

        if (await db.Products.AnyAsync(p => p.Code == dto.Code && p.Id != id, ct)) return Duplicate();

        Apply(entity, dto);
        return await SaveAsync(entity, ct);
    }

    public async Task<OperationResult<ProductEditDto>> SetActiveAsync(int id, bool isActive, CancellationToken ct)
    {
        var entity = await db.Products.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (entity is null) return OperationResult<ProductEditDto>.NotFound();

        entity.IsActive = isActive;
        _ = await db.SaveChangesAsync(ct);
        return OperationResult<ProductEditDto>.Ok(ToDto(entity));
    }

    private async Task<OperationResult<ProductEditDto>> SaveAsync(Product entity, CancellationToken ct)
    {
        try
        {
            _ = await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return Duplicate();
        }
        return OperationResult<ProductEditDto>.Ok(ToDto(entity));
    }

    private static OperationResult<ProductEditDto> Duplicate()
    {
        return OperationResult<ProductEditDto>.Invalid(new Dictionary<string, string[]>
        {
            [nameof(ProductEditDto.Code)] = [DuplicateMessage]
        });
    }

    private static string? Clean(string? s)
    {
        return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    }

    private static void Normalize(ProductEditDto d)
    {
        d.Code = d.Code?.Trim() ?? "";
        d.Name = d.Name?.Trim() ?? "";
        d.Unit = d.Unit?.Trim() ?? "";
        d.Description = Clean(d.Description);
        d.ProductGroup = Clean(d.ProductGroup);
        d.ProductType = Clean(d.ProductType);
        d.Note = Clean(d.Note);
    }

    private static void Apply(Product e, ProductEditDto d)
    {
        e.Code = d.Code;
        e.Name = d.Name;
        e.Description = d.Description;
        e.ProductGroup = d.ProductGroup;
        e.ProductType = d.ProductType;
        e.Unit = d.Unit;
        e.SalesPrice = d.SalesPrice!.Value;   // validace zaručuje, že hodnota je vyplněná
        e.Width = d.Width;
        e.Height = d.Height;
        e.Depth = d.Depth;
        e.Weight = d.Weight;
        e.DeliveryDays = d.DeliveryDays;
        e.Note = d.Note;
        e.IsActive = d.IsActive;
    }

    private static ProductEditDto ToDto(Product e)
    {
        return new()
        {
            Id = e.Id,
            Code = e.Code,
            Name = e.Name,
            Description = e.Description,
            ProductGroup = e.ProductGroup,
            ProductType = e.ProductType,
            Unit = e.Unit,
            SalesPrice = e.SalesPrice,
            Width = e.Width,
            Height = e.Height,
            Depth = e.Depth,
            Weight = e.Weight,
            DeliveryDays = e.DeliveryDays,
            Note = e.Note,
            IsActive = e.IsActive
        };
    }
}