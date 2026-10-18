using FurnitureOrders.Api.Data;
using FurnitureOrders.Api.Domain;
using FurnitureOrders.Shared.Materials;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FurnitureOrders.Api.Services;

public interface IMaterialService
{
    Task<IReadOnlyList<MaterialListItemDto>> ListAsync(MaterialQuery query, CancellationToken ct);
    Task<MaterialEditDto?> GetAsync(int id, CancellationToken ct);
    Task<OperationResult<MaterialEditDto>> CreateAsync(MaterialEditDto dto, CancellationToken ct);
    Task<OperationResult<MaterialEditDto>> UpdateAsync(int id, MaterialEditDto dto, CancellationToken ct);
    Task<OperationResult<MaterialEditDto>> SetActiveAsync(int id, bool isActive, CancellationToken ct);
}

public class MaterialService(AppDbContext db) : IMaterialService
{
    private const string DuplicateMessage = "Materiál s tímto kódem již existuje.";

    public async Task<IReadOnlyList<MaterialListItemDto>> ListAsync(MaterialQuery q, CancellationToken ct)
    {
        var query = db.Materials.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim();
            query = query.Where(m => m.Code.Contains(s) || m.Name.Contains(s));
        }
        if (q.IsActive is { } active) query = query.Where(m => m.IsActive == active);
        if (q.Type is { } type) query = query.Where(m => m.Type == type);

        IOrderedQueryable<Material> ordered = (q.SortBy?.ToLowerInvariant(), q.Desc) switch
        {
            ("name", false) => query.OrderBy(m => m.Name),
            ("name", true) => query.OrderByDescending(m => m.Name),
            ("type", false) => query.OrderBy(m => m.Type),
            ("type", true) => query.OrderByDescending(m => m.Type),
            ("price", false) => query.OrderBy(m => m.PurchasePrice),
            ("price", true) => query.OrderByDescending(m => m.PurchasePrice),
            (_, true) => query.OrderByDescending(m => m.Code),
            _ => query.OrderBy(m => m.Code)
        };

        return await ordered
            .Select(m => new MaterialListItemDto(
                m.Id, m.Code, m.Name, m.Type, m.Unit, m.PurchasePrice, m.Supplier, m.IsActive))
            .ToListAsync(ct);
    }

    public async Task<MaterialEditDto?> GetAsync(int id, CancellationToken ct)
    {
        var m = await db.Materials.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return m is null ? null : ToDto(m);
    }

    public async Task<OperationResult<MaterialEditDto>> CreateAsync(MaterialEditDto dto, CancellationToken ct)
    {
        Normalize(dto);
        var errors = ModelValidator.Validate(dto);
        if (errors.Count > 0) return OperationResult<MaterialEditDto>.Invalid(errors);

        if (await db.Materials.AnyAsync(m => m.Code == dto.Code, ct)) return Duplicate();

        var entity = new Material();
        Apply(entity, dto);
        _ = db.Materials.Add(entity);
        return await SaveAsync(entity, ct);
    }

    public async Task<OperationResult<MaterialEditDto>> UpdateAsync(int id, MaterialEditDto dto, CancellationToken ct)
    {
        var entity = await db.Materials.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (entity is null) return OperationResult<MaterialEditDto>.NotFound();

        Normalize(dto);
        var errors = ModelValidator.Validate(dto);
        if (errors.Count > 0) return OperationResult<MaterialEditDto>.Invalid(errors);

        if (await db.Materials.AnyAsync(m => m.Code == dto.Code && m.Id != id, ct)) return Duplicate();

        Apply(entity, dto);
        return await SaveAsync(entity, ct);
    }

    public async Task<OperationResult<MaterialEditDto>> SetActiveAsync(int id, bool isActive, CancellationToken ct)
    {
        var entity = await db.Materials.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (entity is null) return OperationResult<MaterialEditDto>.NotFound();

        entity.IsActive = isActive;
        _ = await db.SaveChangesAsync(ct);
        return OperationResult<MaterialEditDto>.Ok(ToDto(entity));
    }

    private async Task<OperationResult<MaterialEditDto>> SaveAsync(Material entity, CancellationToken ct)
    {
        try
        {
            _ = await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return Duplicate();
        }
        return OperationResult<MaterialEditDto>.Ok(ToDto(entity));
    }

    private static OperationResult<MaterialEditDto> Duplicate()
    {
        return OperationResult<MaterialEditDto>.Invalid(new Dictionary<string, string[]>
        {
            [nameof(MaterialEditDto.Code)] = [DuplicateMessage]
        });
    }

    private static string? Clean(string? s)
    {
        return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    }

    private static void Normalize(MaterialEditDto d)
    {
        d.Code = d.Code?.Trim() ?? "";
        d.Name = d.Name?.Trim() ?? "";
        d.Unit = d.Unit?.Trim() ?? "";
        d.Description = Clean(d.Description);
        d.Supplier = Clean(d.Supplier);
        d.StockNumber = Clean(d.StockNumber);
        d.ColorOrDecor = Clean(d.ColorOrDecor);
        d.Note = Clean(d.Note);
    }

    private static void Apply(Material e, MaterialEditDto d)
    {
        e.Code = d.Code;
        e.Name = d.Name;
        e.Description = d.Description;
        e.Type = d.Type;
        e.Unit = d.Unit;
        e.PurchasePrice = d.PurchasePrice;
        e.Supplier = d.Supplier;
        e.StockNumber = d.StockNumber;
        e.MinOrderQuantity = d.MinOrderQuantity;
        e.ColorOrDecor = d.ColorOrDecor;
        e.Note = d.Note;
        e.IsActive = d.IsActive;
    }

    private static MaterialEditDto ToDto(Material e)
    {
        return new()
        {
            Id = e.Id,
            Code = e.Code,
            Name = e.Name,
            Description = e.Description,
            Type = e.Type,
            Unit = e.Unit,
            PurchasePrice = e.PurchasePrice,
            Supplier = e.Supplier,
            StockNumber = e.StockNumber,
            MinOrderQuantity = e.MinOrderQuantity,
            ColorOrDecor = e.ColorOrDecor,
            Note = e.Note,
            IsActive = e.IsActive
        };
    }
}