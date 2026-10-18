using FurnitureOrders.Api.Data;
using FurnitureOrders.Api.Domain;
using FurnitureOrders.Shared.Products;
using Microsoft.EntityFrameworkCore;

namespace FurnitureOrders.Api.Services;

public interface IProductRoutingService
{
    Task<OperationResult<IReadOnlyList<ProductMaterialDto>>> ListMaterialsAsync(int productId, CancellationToken ct);
    Task<OperationResult<ProductMaterialDto>> CreateMaterialAsync(int productId, ProductMaterialEditDto dto, CancellationToken ct);
    Task<OperationResult<ProductMaterialDto>> UpdateMaterialAsync(int productId, int id, ProductMaterialEditDto dto, CancellationToken ct);
    Task<OperationResult<bool>> DeleteMaterialAsync(int productId, int id, CancellationToken ct);

    Task<OperationResult<IReadOnlyList<ProductOperationDto>>> ListOperationsAsync(int productId, CancellationToken ct);
    Task<OperationResult<ProductOperationDto>> CreateOperationAsync(int productId, ProductOperationEditDto dto, CancellationToken ct);
    Task<OperationResult<ProductOperationDto>> UpdateOperationAsync(int productId, int id, ProductOperationEditDto dto, CancellationToken ct);
    Task<OperationResult<bool>> DeleteOperationAsync(int productId, int id, CancellationToken ct);
    Task<OperationResult<bool>> MoveOperationAsync(int productId, int id, int direction, CancellationToken ct);
}

public class ProductRoutingService(AppDbContext db) : IProductRoutingService
{
    private static string? Clean(string? s)
    {
        return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    }

    private Task<bool> ProductExistsAsync(int productId, CancellationToken ct)
    {
        return db.Products.AnyAsync(p => p.Id == productId, ct);
    }

    // ---------------------------------------------------------------- materiály

    public async Task<OperationResult<IReadOnlyList<ProductMaterialDto>>> ListMaterialsAsync(int productId, CancellationToken ct)
    {
        if (!await ProductExistsAsync(productId, ct))
            return OperationResult<IReadOnlyList<ProductMaterialDto>>.NotFound();

        var items = await db.ProductMaterials.AsNoTracking()
            .Where(x => x.ProductId == productId)
            .OrderBy(x => x.Sequence).ThenBy(x => x.Id)
            .Select(x => new ProductMaterialDto(
                x.Id, x.ProductId, x.Sequence,
                x.MaterialId, x.Material!.Code, x.Material.Name,
                x.Quantity, x.Unit, x.WastePercent, x.Note))
            .ToListAsync(ct);

        return OperationResult<IReadOnlyList<ProductMaterialDto>>.Ok(items);
    }

    public async Task<OperationResult<ProductMaterialDto>> CreateMaterialAsync(int productId, ProductMaterialEditDto dto, CancellationToken ct)
    {
        if (!await ProductExistsAsync(productId, ct))
            return OperationResult<ProductMaterialDto>.NotFound();

        var (material, errors) = await ValidateMaterialAsync(dto, currentMaterialId: null, ct);
        if (errors.Count > 0) return OperationResult<ProductMaterialDto>.Invalid(errors);

        var maxSeq = await db.ProductMaterials
            .Where(x => x.ProductId == productId)
            .MaxAsync(x => (int?)x.Sequence, ct) ?? 0;

        var entity = new ProductMaterial { ProductId = productId, Sequence = dto.Sequence ?? maxSeq + 1 };
        ApplyMaterial(entity, dto, material!);

        _ = db.ProductMaterials.Add(entity);
        _ = await db.SaveChangesAsync(ct);
        return OperationResult<ProductMaterialDto>.Ok(ToDto(entity, material!));
    }

    public async Task<OperationResult<ProductMaterialDto>> UpdateMaterialAsync(int productId, int id, ProductMaterialEditDto dto, CancellationToken ct)
    {
        var entity = await db.ProductMaterials.FirstOrDefaultAsync(x => x.Id == id && x.ProductId == productId, ct);
        if (entity is null) return OperationResult<ProductMaterialDto>.NotFound();

        var (material, errors) = await ValidateMaterialAsync(dto, entity.MaterialId, ct);
        if (errors.Count > 0) return OperationResult<ProductMaterialDto>.Invalid(errors);

        if (dto.Sequence is { } seq) entity.Sequence = seq;
        ApplyMaterial(entity, dto, material!);

        _ = await db.SaveChangesAsync(ct);
        return OperationResult<ProductMaterialDto>.Ok(ToDto(entity, material!));
    }

    public async Task<OperationResult<bool>> DeleteMaterialAsync(int productId, int id, CancellationToken ct)
    {
        var entity = await db.ProductMaterials.FirstOrDefaultAsync(x => x.Id == id && x.ProductId == productId, ct);
        if (entity is null) return OperationResult<bool>.NotFound();

        _ = db.ProductMaterials.Remove(entity);
        _ = await db.SaveChangesAsync(ct);
        return OperationResult<bool>.Ok(true);
    }

    private async Task<(Material? Material, Dictionary<string, string[]> Errors)> ValidateMaterialAsync(
        ProductMaterialEditDto dto, int? currentMaterialId, CancellationToken ct)
    {
        dto.Unit = Clean(dto.Unit);
        dto.Note = Clean(dto.Note);

        var errors = ModelValidator.Validate(dto);
        Material? material = null;

        if (dto.MaterialId is { } id)
        {
            material = await db.Materials.FirstOrDefaultAsync(m => m.Id == id, ct);
            if (material is null)
                errors[nameof(ProductMaterialEditDto.MaterialId)] = ["Vybraný materiál neexistuje."];
            else if (!material.IsActive && id != currentMaterialId)
                errors[nameof(ProductMaterialEditDto.MaterialId)] = ["Neaktivní materiál nelze k výrobku přiřadit."];
        }

        return (material, errors);
    }

    private static void ApplyMaterial(ProductMaterial e, ProductMaterialEditDto d, Material material)
    {
        e.MaterialId = material.Id;
        e.Quantity = d.Quantity!.Value;
        e.Unit = d.Unit ?? material.Unit;
        e.WastePercent = d.WastePercent;
        e.Note = d.Note;
    }

    private static ProductMaterialDto ToDto(ProductMaterial e, Material m)
    {
        return new(e.Id, e.ProductId, e.Sequence, m.Id, m.Code, m.Name, e.Quantity, e.Unit, e.WastePercent, e.Note);
    }

    // ----------------------------------------------------------------- operace

    public async Task<OperationResult<IReadOnlyList<ProductOperationDto>>> ListOperationsAsync(int productId, CancellationToken ct)
    {
        if (!await ProductExistsAsync(productId, ct))
            return OperationResult<IReadOnlyList<ProductOperationDto>>.NotFound();

        var items = await db.ProductOperations.AsNoTracking()
            .Where(x => x.ProductId == productId)
            .OrderBy(x => x.Sequence).ThenBy(x => x.Id)
            .Select(x => new ProductOperationDto(
                x.Id, x.ProductId, x.Sequence,
                x.OperationId, x.Operation!.Code, x.Operation.Name,
                x.Workplace, x.SetupMinutes, x.RunMinutesPerUnit, x.WorkInstructions, x.Note))
            .ToListAsync(ct);

        return OperationResult<IReadOnlyList<ProductOperationDto>>.Ok(items);
    }

    public async Task<OperationResult<ProductOperationDto>> CreateOperationAsync(int productId, ProductOperationEditDto dto, CancellationToken ct)
    {
        if (!await ProductExistsAsync(productId, ct))
            return OperationResult<ProductOperationDto>.NotFound();

        var (operation, errors) = await ValidateOperationAsync(dto, currentOperationId: null, ct);
        if (errors.Count > 0) return OperationResult<ProductOperationDto>.Invalid(errors);

        var maxSeq = await db.ProductOperations
            .Where(x => x.ProductId == productId)
            .MaxAsync(x => (int?)x.Sequence, ct) ?? 0;

        var entity = new ProductOperation { ProductId = productId, Sequence = dto.Sequence ?? maxSeq + 1 };
        ApplyOperation(entity, dto, operation!);

        _ = db.ProductOperations.Add(entity);
        _ = await db.SaveChangesAsync(ct);
        return OperationResult<ProductOperationDto>.Ok(ToDto(entity, operation!));
    }

    public async Task<OperationResult<ProductOperationDto>> UpdateOperationAsync(int productId, int id, ProductOperationEditDto dto, CancellationToken ct)
    {
        var entity = await db.ProductOperations.FirstOrDefaultAsync(x => x.Id == id && x.ProductId == productId, ct);
        if (entity is null) return OperationResult<ProductOperationDto>.NotFound();

        var (operation, errors) = await ValidateOperationAsync(dto, entity.OperationId, ct);
        if (errors.Count > 0) return OperationResult<ProductOperationDto>.Invalid(errors);

        if (dto.Sequence is { } seq) entity.Sequence = seq;
        ApplyOperation(entity, dto, operation!);

        _ = await db.SaveChangesAsync(ct);
        return OperationResult<ProductOperationDto>.Ok(ToDto(entity, operation!));
    }

    public async Task<OperationResult<bool>> DeleteOperationAsync(int productId, int id, CancellationToken ct)
    {
        var entity = await db.ProductOperations.FirstOrDefaultAsync(x => x.Id == id && x.ProductId == productId, ct);
        if (entity is null) return OperationResult<bool>.NotFound();

        _ = db.ProductOperations.Remove(entity);
        _ = await db.SaveChangesAsync(ct);
        return OperationResult<bool>.Ok(true);
    }

    public async Task<OperationResult<bool>> MoveOperationAsync(int productId, int id, int direction, CancellationToken ct)
    {
        if (direction is not (-1 or 1))
            return OperationResult<bool>.Invalid(new Dictionary<string, string[]>
            {
                ["Direction"] = ["Směr musí být -1 (nahoru) nebo 1 (dolů)."]
            });

        var items = await db.ProductOperations
            .Where(x => x.ProductId == productId)
            .OrderBy(x => x.Sequence).ThenBy(x => x.Id)
            .ToListAsync(ct);

        var index = items.FindIndex(x => x.Id == id);
        if (index < 0) return OperationResult<bool>.NotFound();

        var target = index + direction;
        if (target >= 0 && target < items.Count)
            (items[index], items[target]) = (items[target], items[index]);

        for (var i = 0; i < items.Count; i++)
            items[i].Sequence = i + 1;

        _ = await db.SaveChangesAsync(ct);
        return OperationResult<bool>.Ok(true);
    }

    private async Task<(Operation? Operation, Dictionary<string, string[]> Errors)> ValidateOperationAsync(
        ProductOperationEditDto dto, int? currentOperationId, CancellationToken ct)
    {
        dto.Workplace = Clean(dto.Workplace);
        dto.WorkInstructions = Clean(dto.WorkInstructions);
        dto.Note = Clean(dto.Note);

        var errors = ModelValidator.Validate(dto);
        Operation? operation = null;

        if (dto.OperationId is { } id)
        {
            operation = await db.Operations.FirstOrDefaultAsync(o => o.Id == id, ct);
            if (operation is null)
                errors[nameof(ProductOperationEditDto.OperationId)] = ["Vybraná operace neexistuje."];
            else if (!operation.IsActive && id != currentOperationId)
                errors[nameof(ProductOperationEditDto.OperationId)] = ["Neaktivní operaci nelze k výrobku přiřadit."];
        }

        return (operation, errors);
    }

    private static void ApplyOperation(ProductOperation e, ProductOperationEditDto d, Operation operation)
    {
        e.OperationId = operation.Id;
        e.Workplace = d.Workplace;
        e.SetupMinutes = d.SetupMinutes;
        e.RunMinutesPerUnit = d.RunMinutesPerUnit;
        e.WorkInstructions = d.WorkInstructions;
        e.Note = d.Note;
    }

    private static ProductOperationDto ToDto(ProductOperation e, Operation o)
    {
        return new(e.Id, e.ProductId, e.Sequence, o.Id, o.Code, o.Name,
            e.Workplace, e.SetupMinutes, e.RunMinutesPerUnit, e.WorkInstructions, e.Note);
    }
}