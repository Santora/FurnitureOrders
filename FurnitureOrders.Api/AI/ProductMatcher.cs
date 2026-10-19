using FurnitureOrders.Shared.Orders;

namespace FurnitureOrders.Api.Ai;

public record MatchResult(DraftItemStatus Status, IReadOnlyList<CatalogEntry> Candidates);

public static class ProductMatcher
{
    private const int MaxCandidates = 5;

    /// Model jen navrhuje kódy kandidátů. Přijmeme je, jen když se jejich název (nebo kód)
    /// shoduje s rozpoznaným názvem ve všech slovech; jinak je potřeba potvrzení uživatele.
    public static MatchResult Match(RawItem item, IReadOnlyDictionary<string, CatalogEntry> byCode)
    {
        var mentionTokens = TextNormalizer.Tokens(
            string.IsNullOrWhiteSpace(item.NormalizedName) ? item.Mention : item.NormalizedName);

        if (mentionTokens.Count == 0)
            return new MatchResult(DraftItemStatus.Unknown, []);

        var kept = new List<(CatalogEntry Entry, bool Full)>();

        foreach (var code in item.CandidateCodes.Select(c => c.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Take(MaxCandidates))
        {
            if (!byCode.TryGetValue(code, out var entry)) continue;   // kód, který neexistuje nebo je neaktivní

            var tokens = TextNormalizer.Tokens($"{entry.Name} {entry.Code}").ToHashSet();
            var exact = mentionTokens.Count(tokens.Contains);

            if (exact > 0)   // kandidát bez jediného společného slova se zahodí (ochrana proti záměně)
                kept.Add((entry, exact == mentionTokens.Count));
        }

        if (kept.Count == 0)
            return new MatchResult(DraftItemStatus.Unknown, []);

        if (kept.Count == 1 && kept[0].Full)
            return new MatchResult(DraftItemStatus.Matched, [kept[0].Entry]);

        return new MatchResult(DraftItemStatus.NeedsConfirmation, kept.Select(k => k.Entry).ToList());
    }
}