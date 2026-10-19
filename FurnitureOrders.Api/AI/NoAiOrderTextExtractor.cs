namespace FurnitureOrders.Api.Ai;

public sealed class NoAiOrderTextExtractor : IOrderTextExtractor
{
    public string Provider => "None";
    public bool IsConfigured => false;

    public Task<RawExtraction> ExtractAsync(string text, DateOnly today, IReadOnlyList<CatalogEntry> catalog, CancellationToken ct)
    {
        throw new AiException(AiErrorKind.NotConfigured, "AI služba není nakonfigurována.");
    }
}