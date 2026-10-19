using System.Globalization;

namespace FurnitureOrders.Client.Services;

public static class QueryString
{
    public static string Build(params (string Key, object? Value)[] parameters)
    {
        var parts = parameters
            .Where(p => p.Value is not null && p.Value is not "")
            .Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(Format(p.Value!))}")
            .ToList();

        return parts.Count == 0 ? "" : "?" + string.Join("&", parts);
    }

    private static string Format(object v)
    {
        return v switch
        {
            bool b => b ? "true" : "false",
            string s => s.Trim(),
            DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            DateTime dt => dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => v.ToString() ?? ""
        };
    }
}