namespace FurnitureOrders.Api.Ai;

public record CatalogEntry(int Id, string Code, string Name, string Unit, decimal Price);

public record RawItem(string Mention, decimal? Quantity, string? NormalizedName, IReadOnlyList<string> CandidateCodes);

public record RawExtraction(
    IReadOnlyList<RawItem> Items,
    string? RequiredDate,        // ISO yyyy-MM-dd, jen pokud je v textu jednoznačné
    string? DeliveryAddress,
    string? CustomerNotes,
    string? PriceRequests);

public interface IOrderTextExtractor
{
    string Provider { get; }
    bool IsConfigured { get; }
    Task<RawExtraction> ExtractAsync(string text, DateOnly today, IReadOnlyList<CatalogEntry> catalog, CancellationToken ct);
}