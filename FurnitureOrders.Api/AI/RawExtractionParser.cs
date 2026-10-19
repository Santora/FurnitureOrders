using System.Text.Json;

namespace FurnitureOrders.Api.Ai;

public static class RawExtractionParser
{
    public static RawExtraction Parse(JsonElement input)
    {
        try
        {
            if (input.ValueKind != JsonValueKind.Object) throw Invalid("Výstup není objekt.");

            var items = new List<RawItem>();
            if (input.TryGetProperty("items", out var arr))
            {
                if (arr.ValueKind != JsonValueKind.Array) throw Invalid("Položky nejsou pole.");

                foreach (var el in arr.EnumerateArray())
                {
                    if (el.ValueKind != JsonValueKind.Object) throw Invalid("Položka není objekt.");

                    var mention = TextNormalizer.Sanitize(GetString(el, "mention"), 200)
                        ?? throw Invalid("Položka nemá popis výrobku.");

                    decimal? quantity = null;
                    if (el.TryGetProperty("quantity", out var q) && q.ValueKind != JsonValueKind.Null)
                    {
                        if (q.ValueKind != JsonValueKind.Number) throw Invalid("Množství není číslo.");
                        quantity = q.GetDecimal();
                    }

                    var codes = new List<string>();
                    if (el.TryGetProperty("candidate_codes", out var c) && c.ValueKind != JsonValueKind.Null)
                    {
                        if (c.ValueKind != JsonValueKind.Array) throw Invalid("Kandidáti nejsou pole.");
                        foreach (var code in c.EnumerateArray())
                        {
                            if (code.ValueKind != JsonValueKind.String) throw Invalid("Kód kandidáta není text.");
                            var s = TextNormalizer.Sanitize(code.GetString(), 50);
                            if (s is not null) codes.Add(s);
                        }
                    }

                    items.Add(new RawItem(mention, quantity, TextNormalizer.Sanitize(GetString(el, "normalized_name"), 200), codes));
                }
            }

            return new RawExtraction(
                items,
                TextNormalizer.Sanitize(GetString(input, "required_date"), 20),
                TextNormalizer.Sanitize(GetString(input, "delivery_address"), 500),
                TextNormalizer.Sanitize(GetString(input, "customer_notes"), 1000),
                TextNormalizer.Sanitize(GetString(input, "price_or_discount_requests"), 1000));
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or InvalidOperationException)
        {
            throw Invalid("Neplatná hodnota ve výstupu modelu.", ex);
        }
    }

    private static string? GetString(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p) || p.ValueKind == JsonValueKind.Null) return null;
        if (p.ValueKind != JsonValueKind.String) throw Invalid($"Pole {name} není text.");
        return p.GetString();
    }

    private static AiException Invalid(string message, Exception? inner = null)
    {
        return new(AiErrorKind.InvalidResponse, "AI služba vrátila neplatnou odpověď. " + message, inner);
    }
}