using System.Text.RegularExpressions;
using FurnitureOrders.Shared.Grid;

namespace FurnitureOrders.Client.Grid;

/// <summary>
/// Fetches a grid configuration file from the client's static assets and passes it through
/// <see cref="GridConfigParser"/>. It is the only part of the pipeline that performs I/O.
/// </summary>
/// <remarks>
/// <para>
/// The files are served as static content from <c>wwwroot/grids/{name}.json</c>, so a change to a grid is a
/// file edit with no recompilation of any C# (during development a browser refresh is enough). The trade-off is that
/// the configuration is public and shipped with the client, which is acceptable because it contains no secrets:
/// it only names DTO properties that the user can already see.
/// </para>
/// <para>
/// The HTTP client is created in <c>Program.cs</c> with the app's own base address, i.e. it is a separate
/// instance from the one that talks to the API, so API headers or base URLs can never leak into this request.
/// </para>
/// </remarks>
public sealed partial class GridConfigLoader(HttpClient http)
{
    /// <summary>
    /// Loads and validates the configuration called <paramref name="name"/>.
    /// Always returns a result; failures (invalid name, file not found, network error, bad content) are
    /// reported through <see cref="GridConfigParseResult.Errors"/>, never as an exception.
    /// </summary>
    /// <param name="name">File name without extension, for example <c>"orders"</c>.</param>
    public async Task<GridConfigParseResult> LoadAsync(string name, CancellationToken ct = default)
    {
        // The name ends up in a URL, so it is restricted to letters, digits, underscore and hyphen.
        // This rules out path segments ("../"), query strings and absolute URLs.
        if (!NamePattern().IsMatch(name))
            return new(null, ["Neplatný název konfigurace gridu."]);

        try
        {
            var json = await http.GetStringAsync($"grids/{name}.json", ct);
            return GridConfigParser.Parse(json);
        }
        catch (HttpRequestException ex)
        {
            // Includes 404 for a missing file, so a typo in ConfigName produces a readable message.
            return new(null, [$"Konfiguraci 'grids/{name}.json' se nepodařilo načíst: {ex.Message}"]);
        }
    }

    // Source-generated regex (no runtime construction cost, trimming friendly).
    // The ^...$ anchors matter: without them any string merely containing a valid character would match.
    [GeneratedRegex("^[a-z0-9_-]+$", RegexOptions.IgnoreCase)]
    private static partial Regex NamePattern();
}
