using System.Text.RegularExpressions;

namespace FurnitureOrders.Api.Ai;

public sealed partial class FakeOrderTextExtractor : IOrderTextExtractor
{
    public string Provider => "Fake";
    public bool IsConfigured => true;

    private static readonly Dictionary<string, decimal> NumberWords = new()
    {
        ["jedna"] = 1,
        ["jeden"] = 1,
        ["jednu"] = 1,
        ["dva"] = 2,
        ["dve"] = 2,
        ["tri"] = 3,
        ["ctyri"] = 4,
        ["pet"] = 5,
        ["sest"] = 6,
        ["sedm"] = 7,
        ["osm"] = 8,
        ["devet"] = 9,
        ["deset"] = 10
    };

    public Task<RawExtraction> ExtractAsync(string text, DateOnly today, IReadOnlyList<CatalogEntry> catalog, CancellationToken ct)
    {
        // simulace chyb
        if (text.Contains("FAKE_UNAVAILABLE")) throw new AiException(AiErrorKind.Unavailable, "AI služba je nedostupná (simulace).");
        if (text.Contains("FAKE_TIMEOUT")) throw new AiException(AiErrorKind.Timeout, "AI služba neodpověděla včas (simulace).");
        if (text.Contains("FAKE_INVALID")) throw new AiException(AiErrorKind.InvalidResponse, "AI služba vrátila neplatnou odpověď (simulace).");

        var original = WordRegex().Matches(text).Select(m => m.Value).ToList();
        var normalized = original.Select(TextNormalizer.Normalize).ToList();
        var items = new List<RawItem>();
        var consumed = new HashSet<int>();

        // výrobky podle posledního slova názvu (Alfa, Beta, Delta ...)
        foreach (var product in catalog)
        {
            var key = TextNormalizer.Tokens(product.Name).LastOrDefault();
            var index = key is null ? -1 : normalized.IndexOf(key);
            if (index < 0) continue;

            items.Add(BuildItem(original, normalized, index, product.Name, [product.Code], consumed));
        }

        // obecné označení "police": odpovídá více výrobkům
        var policeIndex = normalized.IndexOf("police");
        if (policeIndex >= 0 && !items.Any(i => TextNormalizer.Tokens(i.Mention).Contains("police")))
        {
            var codes = catalog.Where(p => TextNormalizer.Tokens(p.Name).Contains("police")).Select(p => p.Code).ToList();
            items.Add(BuildItem(original, normalized, policeIndex, "police", codes, consumed));
        }

        // výrobek, který v evidenci není
        var zetaIndex = normalized.IndexOf("zeta");
        if (zetaIndex >= 0)
            items.Add(BuildItem(original, normalized, zetaIndex, "skříň Zeta", [], consumed));

        var date = DateRegex().Match(text) is { Success: true } m
            ? $"{m.Groups[3].Value}-{int.Parse(m.Groups[2].Value):D2}-{int.Parse(m.Groups[1].Value):D2}"
            : null;

        var address = AddressRegex().Match(text) is { Success: true } a ? a.Groups[1].Value.Trim() : null;

        var norm = TextNormalizer.Normalize(text);
        var notes = norm.Contains("zavolej") || norm.Contains("zavolat") ? "Před doručením zavolat." : null;

        var price = norm.Contains("cenu") || norm.Contains("slev") || norm.Contains("ignoruj")
            ? TextNormalizer.Sanitize(text, 300)
            : null;

        return Task.FromResult(new RawExtraction(items, date, address, notes, price));
    }

    private static RawItem BuildItem(IReadOnlyList<string> original, IReadOnlyList<string> normalized,
        int index, string normalizedName, IReadOnlyList<string> codes, HashSet<int> consumed)
    {
        decimal? quantity = null;
        var from = Math.Max(0, index - 3);

        for (var i = index - 1; i >= from; i--)
        {
            if (consumed.Contains(i)) break;
            if (decimal.TryParse(normalized[i], out var n)) { quantity = n; _ = consumed.Add(i); break; }
            if (NumberWords.TryGetValue(normalized[i], out var w)) { quantity = w; _ = consumed.Add(i); break; }
        }

        var mentionWords = new List<string>();
        for (var i = from; i <= index; i++)
        {
            if (decimal.TryParse(normalized[i], out _) || NumberWords.ContainsKey(normalized[i])) continue;
            mentionWords.Add(original[i]);
        }

        return new RawItem(string.Join(' ', mentionWords), quantity, normalizedName, codes);
    }

    [GeneratedRegex(@"\p{L}+|\d+")]
    private static partial Regex WordRegex();

    [GeneratedRegex(@"\b(\d{1,2})\s*\.\s*(\d{1,2})\s*\.\s*(\d{4})\b")]
    private static partial Regex DateRegex();

    [GeneratedRegex(@"na adresu\s+([^.\n]+)", RegexOptions.IgnoreCase)]
    private static partial Regex AddressRegex();
}