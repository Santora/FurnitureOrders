using FurnitureOrders.Api.Ai;
using FurnitureOrders.Api.Data;
using FurnitureOrders.Shared.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FurnitureOrders.Api.Services;

public interface IOrderDraftService
{
    AiStatusDto Status();
    Task<OperationResult<OrderDraftDto>> CreateDraftAsync(OrderDraftRequest request, CancellationToken ct);
}

public class OrderDraftService(
    AppDbContext db, IOrderTextExtractor extractor, IOptions<AiOptions> options, TimeProvider clock) : IOrderDraftService
{
    private const decimal MaxQuantity = 100_000m;

    public AiStatusDto Status()
    {
        return new(extractor.IsConfigured, extractor.Provider, options.Value.MaxInputLength);
    }

    public async Task<OperationResult<OrderDraftDto>> CreateDraftAsync(OrderDraftRequest request, CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();
        var text = request.Text?.Trim();
        var max = options.Value.MaxInputLength;

        if (string.IsNullOrEmpty(text)) errors["Text"] = ["Text poptávky je povinný."];
        else if (text.Length > max) errors["Text"] = [$"Text poptávky může mít nejvýše {max} znaků."];

        if (request.CustomerId is not { } customerId)
            errors["CustomerId"] = ["Zákazník je povinný."];
        else
        {
            var customer = await db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == customerId, ct);
            if (customer is null) errors["CustomerId"] = ["Vybraný zákazník neexistuje."];
            else if (!customer.IsActive) errors["CustomerId"] = ["Neaktivního zákazníka nelze použít."];
        }

        if (errors.Count > 0) return OperationResult<OrderDraftDto>.Invalid(errors);

        // pouze aktivní výrobky; cena a jednotka pocházejí vždy z databáze
        var catalog = await db.Products.AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Code)
            .Take(500)
            .Select(p => new CatalogEntry(p.Id, p.Code, p.Name, p.Unit, p.SalesPrice))
            .ToListAsync(ct);

        var today = DateOnly.FromDateTime(clock.GetLocalNow().DateTime);

        // AiException se propaguje do endpointu, kde se mapuje na srozumitelnou chybu
        var raw = await extractor.ExtractAsync(text!, today, catalog, ct);

        var byCode = catalog.ToDictionary(c => c.Code, StringComparer.OrdinalIgnoreCase);
        var warnings = new List<string>();
        var items = new List<OrderDraftItemDto>();

        foreach (var item in raw.Items.Take(options.Value.MaxItems))
            items.Add(BuildItem(item, byCode));

        if (raw.Items.Count > options.Value.MaxItems)
            warnings.Add($"Poptávka obsahuje více než {options.Value.MaxItems} položek, převzato bylo prvních {options.Value.MaxItems}.");
        if (items.Count == 0)
            warnings.Add("V poptávce nebyl rozpoznán žádný výrobek. Položky zadejte ručně.");

        // termín
        DateOnly? required = null;
        if (raw.RequiredDate is null)
        {
            warnings.Add("Termín dodání v poptávce chybí. Doplňte ho před uložením.");
        }
        else if (!DateOnly.TryParseExact(raw.RequiredDate, "yyyy-MM-dd", out var parsed))
        {
            warnings.Add("Termín dodání se nepodařilo rozpoznat. Doplňte ho před uložením.");
        }
        else if (parsed < today)
        {
            warnings.Add($"Termín dodání z poptávky ({parsed:dd.MM.yyyy}) je v minulosti. Doplňte správný termín.");
        }
        else required = parsed;

        // poznámka: požadavky zákazníka a cenové požadavky (ty se nikdy neuplatní, jen se zachovají k ručnímu posouzení)
        var noteParts = new List<string>();
        if (raw.CustomerNotes is not null) noteParts.Add(raw.CustomerNotes);
        if (raw.PriceRequests is not null)
        {
            noteParts.Add($"Požadavek zákazníka na cenu nebo slevu (k ručnímu posouzení, nebylo uplatněno): {raw.PriceRequests}");
            warnings.Add("Poptávka obsahuje požadavek na cenu nebo slevu. Ceny zůstaly podle evidence výrobků, požadavek je v poznámce.");
        }

        return OperationResult<OrderDraftDto>.Ok(new OrderDraftDto(
            request.CustomerId!.Value,
            items,
            required,
            raw.DeliveryAddress,
            noteParts.Count == 0 ? null : string.Join("\n", noteParts),
            warnings,
            extractor.Provider));
    }

    private static OrderDraftItemDto BuildItem(RawItem item, IReadOnlyDictionary<string, CatalogEntry> byCode)
    {
        var warnings = new List<string>();

        decimal? quantity = item.Quantity;
        if (quantity is null)
            warnings.Add("Chybí množství. Doplňte ho před uložením.");
        else if (quantity <= 0 || quantity > MaxQuantity)
        {
            quantity = null;
            warnings.Add("Množství v poptávce není platné. Doplňte ho před uložením.");
        }

        var match = ProductMatcher.Match(item, byCode);
        DraftProductDto? product = null;
        var candidates = match.Candidates.Select(ToDto).ToList();

        switch (match.Status)
        {
            case DraftItemStatus.Matched:
                product = candidates[0];
                candidates = [];
                break;
            case DraftItemStatus.NeedsConfirmation:
                warnings.Add(candidates.Count > 1
                    ? $"Zmínce „{item.Mention}“ odpovídá více výrobků. Vyberte správný."
                    : $"Přiřazení zmínky „{item.Mention}“ není jisté. Potvrďte výrobek.");
                break;
            default:
                warnings.Add($"Výrobek „{item.Mention}“ nebyl v evidenci nalezen. Přiřaďte ho ručně, nebo položku odeberte.");
                break;
        }

        return new OrderDraftItemDto(item.Mention, quantity, match.Status, product, candidates, warnings);
    }

    private static DraftProductDto ToDto(CatalogEntry e)
    {
        return new(e.Id, e.Code, e.Name, e.Unit, e.Price);
    }
}