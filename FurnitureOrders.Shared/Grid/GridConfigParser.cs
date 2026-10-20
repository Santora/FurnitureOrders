using System.Text.Json;
using System.Text.Json.Serialization;

namespace FurnitureOrders.Shared.Grid;

public sealed record GridConfigParseResult(GridConfig? Config, IReadOnlyList<string> Errors)
{
    public bool IsValid => Config is not null && Errors.Count == 0;
}

public static class GridConfigParser
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// Nikdy nevyhazuje výjimku kvůli obsahu konfigurace: chyby vrací jako seznam textů.
    public static GridConfigParseResult Parse(string json, GridVocabulary? vocabulary = null)
    {
        GridConfig? config;
        try
        {
            config = JsonSerializer.Deserialize<GridConfig>(json, Options);
        }
        catch (JsonException ex)
        {
            return new(null, [$"Neplatný JSON v konfiguraci gridu (řádek {(ex.LineNumber ?? 0) + 1}): {ex.Message}"]);
        }

        if (config is null)
            return new(null, ["Konfigurace gridu je prázdná."]);

        var errors = new GridConfigValidator(vocabulary).Validate(config);
        return new(errors.Count == 0 ? config : null, errors);
    }
}