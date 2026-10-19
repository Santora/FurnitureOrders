using FurnitureOrders.Api.Data;
using FurnitureOrders.Api.Domain;
using FurnitureOrders.Shared.Enums;
using FurnitureOrders.Shared.Orders;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FurnitureOrders.Api.Services;

public interface IOrderService
{
    Task<IReadOnlyList<OrderListItemDto>> ListAsync(OrderQuery query, CancellationToken ct);
    Task<OrderEditDto?> GetAsync(int id, CancellationToken ct);
    Task<OperationResult<OrderEditDto>> CreateAsync(OrderEditDto dto, CancellationToken ct);
    Task<OperationResult<OrderEditDto>> UpdateAsync(int id, OrderEditDto dto, CancellationToken ct);
    Task<OperationResult<OrderEditDto>> SetStatusAsync(int id, OrderStatus status, CancellationToken ct);
}

public class OrderService(AppDbContext db, TimeProvider clock) : IOrderService
{
    private const int MaxItems = 200;
    private const string DuplicateMessage = "Zakázka s tímto číslem již existuje.";

    public async Task<IReadOnlyList<OrderListItemDto>> ListAsync(OrderQuery q, CancellationToken ct)
    {
        var query = db.Orders.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim();
            query = query.Where(o => o.OrderNumber.Contains(s));
        }
        if (q.Status is { } status) query = query.Where(o => o.Status == status);
        if (q.CustomerId is { } customerId) query = query.Where(o => o.CustomerId == customerId);
        if (q.From is { } from) query = query.Where(o => o.CreatedDate >= from);
        if (q.To is { } to) query = query.Where(o => o.CreatedDate <= to);

        IOrderedQueryable<Order> ordered = (q.SortBy?.ToLowerInvariant(), q.Desc) switch
        {
            ("number", false) => query.OrderBy(o => o.OrderNumber),
            ("number", true) => query.OrderByDescending(o => o.OrderNumber),
            ("customer", false) => query.OrderBy(o => o.Customer!.Name),
            ("customer", true) => query.OrderByDescending(o => o.Customer!.Name),
            ("required", false) => query.OrderBy(o => o.RequiredDeliveryDate),
            ("required", true) => query.OrderByDescending(o => o.RequiredDeliveryDate),
            ("status", false) => query.OrderBy(o => o.Status),
            ("status", true) => query.OrderByDescending(o => o.Status),
            ("total", false) => query.OrderBy(o => o.TotalPrice),
            ("total", true) => query.OrderByDescending(o => o.TotalPrice),
            ("created", false) => query.OrderBy(o => o.CreatedDate).ThenBy(o => o.Id),
            _ => query.OrderByDescending(o => o.CreatedDate).ThenByDescending(o => o.Id)   // výchozí: nejnovější první
        };

        return await ordered
            .Select(o => new OrderListItemDto(
                o.Id, o.OrderNumber, o.CustomerId, o.Customer!.Name,
                o.CreatedDate, o.RequiredDeliveryDate, o.Status, o.TotalPrice))
            .ToListAsync(ct);
    }

    public async Task<OrderEditDto?> GetAsync(int id, CancellationToken ct)
    {
        var o = await db.Orders.AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        return o is null ? null : ToDto(o);
    }

    public Task<OperationResult<OrderEditDto>> CreateAsync(OrderEditDto dto, CancellationToken ct)
    {
        return SaveAsync(null, dto, ct);
    }

    public async Task<OperationResult<OrderEditDto>> UpdateAsync(int id, OrderEditDto dto, CancellationToken ct)
    {
        var existing = await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id, ct);
        if (existing is null) return OperationResult<OrderEditDto>.NotFound();
        return await SaveAsync(existing, dto, ct);
    }

    public async Task<OperationResult<OrderEditDto>> SetStatusAsync(int id, OrderStatus status, CancellationToken ct)
    {
        var entity = await db.Orders.FirstOrDefaultAsync(o => o.Id == id, ct);
        if (entity is null) return OperationResult<OrderEditDto>.NotFound();

        if (!Enum.IsDefined(status))
            return OperationResult<OrderEditDto>.Invalid(new Dictionary<string, string[]>
            {
                [nameof(OrderEditDto.Status)] = ["Neplatný stav zakázky."]
            });

        entity.Status = status;
        entity.LastModifiedAt = clock.GetUtcNow().UtcDateTime;
        _ = await db.SaveChangesAsync(ct);

        return OperationResult<OrderEditDto>.Ok((await GetAsync(id, ct))!);
    }

    private async Task<OperationResult<OrderEditDto>> SaveAsync(Order? existing, OrderEditDto dto, CancellationToken ct)
    {
        Normalize(dto);
        var errors = ModelValidator.Validate(dto);

        // zákazník
        if (dto.CustomerId is { } customerId)
        {
            var customer = await db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == customerId, ct);
            if (customer is null)
                errors[nameof(OrderEditDto.CustomerId)] = ["Vybraný zákazník neexistuje."];
            else if (!customer.IsActive && customerId != existing?.CustomerId)
                errors[nameof(OrderEditDto.CustomerId)] = ["Neaktivního zákazníka nelze k zakázce přiřadit."];
        }

        // stav a data
        if (!Enum.IsDefined(dto.Status))
            errors[nameof(OrderEditDto.Status)] = ["Neplatný stav zakázky."];

        if (dto.CreatedDate is { } created)
        {
            if (dto.RequiredDeliveryDate is { } required && required < created)
                errors[nameof(OrderEditDto.RequiredDeliveryDate)] = ["Požadovaný termín dodání nesmí být před datem vytvoření."];
            if (dto.ActualCompletionDate is { } actual && actual < created)
                errors[nameof(OrderEditDto.ActualCompletionDate)] = ["Skutečný termín dokončení nesmí být před datem vytvoření."];
        }

        if (existing is not null && string.IsNullOrEmpty(dto.OrderNumber))
            errors[nameof(OrderEditDto.OrderNumber)] = ["Číslo zakázky je povinné."];

        // položky
        if (dto.Items.Count == 0)
            errors["Items"] = ["Zakázka musí obsahovat alespoň jednu položku."];
        else if (dto.Items.Count > MaxItems)
            errors["Items"] = [$"Zakázka může mít nejvýše {MaxItems} položek."];

        var existingItems = existing?.Items.ToDictionary(i => i.Id) ?? [];
        var productIds = dto.Items.Where(i => i.ProductId.HasValue).Select(i => i.ProductId!.Value).Distinct().ToList();
        var products = await db.Products.AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        var seenIds = new HashSet<int>();
        for (var i = 0; i < dto.Items.Count; i++)
        {
            var item = dto.Items[i];
            NormalizeItem(item);

            foreach (var (key, messages) in ModelValidator.Validate(item))
                errors[$"Items[{i}].{key}"] = messages;

            if (item.ProductId is { } productId)
            {
                if (!products.TryGetValue(productId, out var product))
                {
                    errors[$"Items[{i}].ProductId"] = ["Vybraný výrobek neexistuje."];
                }
                else if (!product.IsActive)
                {
                    var unchanged = item.Id != 0
                        && existingItems.TryGetValue(item.Id, out var old)
                        && old.ProductId == productId;
                    if (!unchanged)
                        errors[$"Items[{i}].ProductId"] = ["Neaktivní výrobek nelze do zakázky přidat."];
                }
            }

            if (item.Id != 0 && (!existingItems.ContainsKey(item.Id) || !seenIds.Add(item.Id)))
                errors[$"Items[{i}].Id"] = ["Položka nepatří k této zakázce."];
        }

        if (errors.Count > 0)
            return OperationResult<OrderEditDto>.Invalid(errors);

        // číslo zakázky
        if (existing is null && string.IsNullOrEmpty(dto.OrderNumber))
            dto.OrderNumber = await NextNumberAsync(ct);

        var existingId = existing?.Id ?? 0;
        if (await db.Orders.AnyAsync(o => o.OrderNumber == dto.OrderNumber && o.Id != existingId, ct))
            return Duplicate();

        // zápis
        var entity = existing ?? new Order();
        entity.OrderNumber = dto.OrderNumber;
        entity.CustomerId = dto.CustomerId!.Value;
        entity.CreatedDate = dto.CreatedDate!.Value;
        entity.RequiredDeliveryDate = dto.RequiredDeliveryDate!.Value;
        entity.ActualCompletionDate = dto.ActualCompletionDate;
        entity.Status = dto.Status;
        entity.ContactPerson = dto.ContactPerson;
        entity.Email = dto.Email;
        entity.Phone = dto.Phone;
        entity.DeliveryAddress = dto.DeliveryAddress;
        entity.ShippingMethod = dto.ShippingMethod;
        entity.Note = dto.Note;

        var keep = new HashSet<int>();
        var sequence = 1;
        foreach (var item in dto.Items)
        {
            OrderItem row;
            if (item.Id != 0)
            {
                row = existingItems[item.Id];
                _ = keep.Add(item.Id);
            }
            else
            {
                row = new OrderItem();
                entity.Items.Add(row);
            }

            var product = products[item.ProductId!.Value];
            row.Sequence = sequence++;
            row.ProductId = product.Id;
            row.Description = item.Description;
            row.Quantity = item.Quantity!.Value;
            row.Unit = item.Unit ?? product.Unit;
            row.UnitPrice = item.UnitPrice!.Value;
            row.DiscountPercent = item.DiscountPercent ?? 0m;
            row.RequiredDeliveryDate = item.RequiredDeliveryDate;
            row.Note = item.Note;
            row.TotalPrice = OrderPricing.ItemTotal(row.Quantity, row.UnitPrice, row.DiscountPercent);
        }

        foreach (var removed in existingItems.Values.Where(i => !keep.Contains(i.Id)).ToList())
        {
            _ = entity.Items.Remove(removed);
            _ = db.OrderItems.Remove(removed);
        }

        entity.TotalPrice = OrderPricing.OrderTotal(entity.Items.Select(i => i.TotalPrice));
        entity.LastModifiedAt = clock.GetUtcNow().UtcDateTime;

        if (existing is null) _ = db.Orders.Add(entity);

        try
        {
            _ = await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return Duplicate();
        }

        return OperationResult<OrderEditDto>.Ok((await GetAsync(entity.Id, ct))!);
    }

    private async Task<string> NextNumberAsync(CancellationToken ct)
    {
        while (true)
        {
            var n = await NumberSequence.NextAsync(db, "OrderNumberSeq", ct);
            var number = $"ZAK-{n:D5}";

            // přeskočí číslo, které už někdo zadal ručně
            if (!await db.Orders.AnyAsync(o => o.OrderNumber == number, ct))
                return number;
        }
    }

    private static OperationResult<OrderEditDto> Duplicate()
    {
        return OperationResult<OrderEditDto>.Invalid(new Dictionary<string, string[]>
        {
            [nameof(OrderEditDto.OrderNumber)] = [DuplicateMessage]
        });
    }

    private static string? Clean(string? s)
    {
        return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    }

    private static void Normalize(OrderEditDto d)
    {
        d.OrderNumber = d.OrderNumber?.Trim() ?? "";
        d.ContactPerson = Clean(d.ContactPerson);
        d.Email = Clean(d.Email);
        d.Phone = Clean(d.Phone);
        d.DeliveryAddress = Clean(d.DeliveryAddress);
        d.ShippingMethod = Clean(d.ShippingMethod);
        d.Note = Clean(d.Note);
        d.Items ??= [];
    }

    private static void NormalizeItem(OrderItemEditDto d)
    {
        d.Description = Clean(d.Description);
        d.Unit = Clean(d.Unit);
        d.Note = Clean(d.Note);
    }

    private static OrderEditDto ToDto(Order o)
    {
        return new()
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            CustomerId = o.CustomerId,
            CustomerName = o.Customer?.Name,
            CreatedDate = o.CreatedDate,
            RequiredDeliveryDate = o.RequiredDeliveryDate,
            ActualCompletionDate = o.ActualCompletionDate,
            Status = o.Status,
            ContactPerson = o.ContactPerson,
            Email = o.Email,
            Phone = o.Phone,
            DeliveryAddress = o.DeliveryAddress,
            ShippingMethod = o.ShippingMethod,
            Note = o.Note,
            TotalPrice = o.TotalPrice,
            LastModifiedAt = o.LastModifiedAt,
            Items = o.Items
            .OrderBy(i => i.Sequence).ThenBy(i => i.Id)
            .Select(i => new OrderItemEditDto
            {
                Id = i.Id,
                Sequence = i.Sequence,
                ProductId = i.ProductId,
                ProductCode = i.Product?.Code,
                ProductName = i.Product?.Name,
                Description = i.Description,
                Quantity = i.Quantity,
                Unit = i.Unit,
                UnitPrice = i.UnitPrice,
                DiscountPercent = i.DiscountPercent,
                RequiredDeliveryDate = i.RequiredDeliveryDate,
                Note = i.Note,
                TotalPrice = i.TotalPrice
            })
            .ToList()
        };
    }
}