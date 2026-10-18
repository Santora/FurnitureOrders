using FurnitureOrders.Api.Data;
using FurnitureOrders.Api.Domain;
using FurnitureOrders.Shared.Operations;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FurnitureOrders.Api.Services;

public interface IOperationService
{
    Task<IReadOnlyList<OperationListItemDto>> ListAsync(OperationQuery query, CancellationToken ct);
    Task<OperationEditDto?> GetAsync(int id, CancellationToken ct);
    Task<OperationResult<OperationEditDto>> CreateAsync(OperationEditDto dto, CancellationToken ct);
    Task<OperationResult<OperationEditDto>> UpdateAsync(int id, OperationEditDto dto, CancellationToken ct);
    Task<OperationResult<OperationEditDto>> SetActiveAsync(int id, bool isActive, CancellationToken ct);
    Task<IReadOnlyList<string>> WorkplacesAsync(CancellationToken ct);
}

public class OperationService(AppDbContext db) : IOperationService
{
    private const string DuplicateMessage = "Operace s tímto kódem již existuje.";

    public async Task<IReadOnlyList<OperationListItemDto>> ListAsync(OperationQuery q, CancellationToken ct)
    {
        var query = db.Operations.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim();
            query = query.Where(o => o.Code.Contains(s) || o.Name.Contains(s));
        }
        if (q.IsActive is { } active) query = query.Where(o => o.IsActive == active);
        if (!string.IsNullOrWhiteSpace(q.Workplace))
        {
            var w = q.Workplace.Trim();
            query = query.Where(o => o.Workplace == w);
        }

        IOrderedQueryable<Operation> ordered = (q.SortBy?.ToLowerInvariant(), q.Desc) switch
        {
            ("name", false) => query.OrderBy(o => o.Name),
            ("name", true) => query.OrderByDescending(o => o.Name),
            ("workplace", false) => query.OrderBy(o => o.Workplace),
            ("workplace", true) => query.OrderByDescending(o => o.Workplace),
            ("rate", false) => query.OrderBy(o => o.HourlyRate),
            ("rate", true) => query.OrderByDescending(o => o.HourlyRate),
            (_, true) => query.OrderByDescending(o => o.Code),
            _ => query.OrderBy(o => o.Code)
        };

        return await ordered
            .Select(o => new OperationListItemDto(
                o.Id, o.Code, o.Name, o.Workplace, o.OperationType,
                o.DefaultSetupMinutes, o.DefaultRunMinutes, o.HourlyRate, o.IsActive))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<string>> WorkplacesAsync(CancellationToken ct)
    {
        return await db.Operations.AsNoTracking()
            .Where(o => o.Workplace != null && o.Workplace != "")
            .Select(o => o.Workplace!)
            .Distinct()
            .OrderBy(w => w)
            .ToListAsync(ct);
    }

    public async Task<OperationEditDto?> GetAsync(int id, CancellationToken ct)
    {
        var o = await db.Operations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return o is null ? null : ToDto(o);
    }

    public async Task<OperationResult<OperationEditDto>> CreateAsync(OperationEditDto dto, CancellationToken ct)
    {
        Normalize(dto);
        var errors = ModelValidator.Validate(dto);
        if (errors.Count > 0) return OperationResult<OperationEditDto>.Invalid(errors);

        if (await db.Operations.AnyAsync(o => o.Code == dto.Code, ct)) return Duplicate();

        var entity = new Operation();
        Apply(entity, dto);
        _ = db.Operations.Add(entity);
        return await SaveAsync(entity, ct);
    }

    public async Task<OperationResult<OperationEditDto>> UpdateAsync(int id, OperationEditDto dto, CancellationToken ct)
    {
        var entity = await db.Operations.FirstOrDefaultAsync(o => o.Id == id, ct);
        if (entity is null) return OperationResult<OperationEditDto>.NotFound();

        Normalize(dto);
        var errors = ModelValidator.Validate(dto);
        if (errors.Count > 0) return OperationResult<OperationEditDto>.Invalid(errors);

        if (await db.Operations.AnyAsync(o => o.Code == dto.Code && o.Id != id, ct)) return Duplicate();

        Apply(entity, dto);
        return await SaveAsync(entity, ct);
    }

    public async Task<OperationResult<OperationEditDto>> SetActiveAsync(int id, bool isActive, CancellationToken ct)
    {
        var entity = await db.Operations.FirstOrDefaultAsync(o => o.Id == id, ct);
        if (entity is null) return OperationResult<OperationEditDto>.NotFound();

        entity.IsActive = isActive;
        _ = await db.SaveChangesAsync(ct);
        return OperationResult<OperationEditDto>.Ok(ToDto(entity));
    }

    private async Task<OperationResult<OperationEditDto>> SaveAsync(Operation entity, CancellationToken ct)
    {
        try
        {
            _ = await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return Duplicate();
        }
        return OperationResult<OperationEditDto>.Ok(ToDto(entity));
    }

    private static OperationResult<OperationEditDto> Duplicate()
    {
        return OperationResult<OperationEditDto>.Invalid(new Dictionary<string, string[]>
        {
            [nameof(OperationEditDto.Code)] = [DuplicateMessage]
        });
    }

    private static string? Clean(string? s)
    {
        return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    }

    private static void Normalize(OperationEditDto d)
    {
        d.Code = d.Code?.Trim() ?? "";
        d.Name = d.Name?.Trim() ?? "";
        d.Description = Clean(d.Description);
        d.Workplace = Clean(d.Workplace);
        d.OperationType = Clean(d.OperationType);
        d.RequiredQualification = Clean(d.RequiredQualification);
        d.Note = Clean(d.Note);
    }

    private static void Apply(Operation e, OperationEditDto d)
    {
        e.Code = d.Code;
        e.Name = d.Name;
        e.Description = d.Description;
        e.Workplace = d.Workplace;
        e.OperationType = d.OperationType;
        e.DefaultSetupMinutes = d.DefaultSetupMinutes;
        e.DefaultRunMinutes = d.DefaultRunMinutes;
        e.HourlyRate = d.HourlyRate;
        e.RequiredQualification = d.RequiredQualification;
        e.Note = d.Note;
        e.IsActive = d.IsActive;
    }

    private static OperationEditDto ToDto(Operation e)
    {
        return new()
        {
            Id = e.Id,
            Code = e.Code,
            Name = e.Name,
            Description = e.Description,
            Workplace = e.Workplace,
            OperationType = e.OperationType,
            DefaultSetupMinutes = e.DefaultSetupMinutes,
            DefaultRunMinutes = e.DefaultRunMinutes,
            HourlyRate = e.HourlyRate,
            RequiredQualification = e.RequiredQualification,
            Note = e.Note,
            IsActive = e.IsActive
        };
    }
}