using System.Text.RegularExpressions;
using FurnitureOrders.Shared.Grid;

namespace FurnitureOrders.Client.Grid;

public sealed partial class GridConfigLoader(HttpClient http)
{
    public async Task<GridConfigParseResult> LoadAsync(string name, CancellationToken ct = default)
    {
        if (!NamePattern().IsMatch(name))
            return new(null, ["Neplatný název konfigurace gridu."]);

        try
        {
            var json = await http.GetStringAsync($"grids/{name}.json", ct);
            return GridConfigParser.Parse(json);
        }
        catch (HttpRequestException ex)
        {
            return new(null, [$"Konfiguraci 'grids/{name}.json' se nepodařilo načíst: {ex.Message}"]);
        }
    }

    [GeneratedRegex("^[a-z0-9_-]+$", RegexOptions.IgnoreCase)]
    private static partial Regex NamePattern();
}