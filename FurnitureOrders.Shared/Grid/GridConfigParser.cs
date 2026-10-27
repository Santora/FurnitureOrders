using System.Text.Json;
using System.Text.Json.Serialization;

namespace FurnitureOrders.Shared.Grid;

/// <summary>
/// Outcome of parsing a grid configuration: either a usable <see cref="Config"/> or a list of
/// human-readable <see cref="Errors"/>, never both.
/// </summary>
/// <param name="Config">The validated configuration, or <c>null</c> when anything was wrong.</param>
/// <param name="Errors">All problems found (empty on success). Messages are shown to the user as they are.</param>
public sealed record GridConfigParseResult(GridConfig? Config, IReadOnlyList<string> Errors)
{
    /// <summary>True only when a configuration exists and no errors were reported.</summary>
    public bool IsValid => Config is not null && Errors.Count == 0;
}

/// <summary>
/// First stage of the configuration pipeline: turns JSON text into a validated <see cref="GridConfig"/>.
/// </summary>
/// <remarks>
/// The parser is intentionally a pure function of its input (no I/O, no Blazor), which is why the loading of
/// the file itself lives in the client's <c>GridConfigLoader</c> and not here. That keeps it trivially unit testable.
/// </remarks>
public static class GridConfigParser
{
    // Web defaults = camelCase property names, case-insensitive matching, numbers allowed as strings.
    // Comments and trailing commas are tolerated because the files are edited by hand and it is
    // friendlier to allow "// why this column is hidden" than to fail on it.
    // The string enum converter lets the JSON say "Descending" instead of a magic number.
    // Note: unmapped (misspelled) properties are ignored by System.Text.Json by default; see the
    // limitations in the overview at the top of GridConfig.cs.
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Parses and validates a configuration. Never throws because of the content of <paramref name="json"/>:
    /// every problem is returned as text in <see cref="GridConfigParseResult.Errors"/>.
    /// </summary>
    /// <param name="json">The text of the configuration file.</param>
    /// <param name="vocabulary">
    /// The set of allowed type names. Normally <c>null</c> (the built-in <see cref="GridVocabulary"/> is used);
    /// a custom instance lets tests or an extension allow additional type names.
    /// </param>
    /// <remarks>
    /// Two kinds of failure are distinguished. Syntax and shape errors (invalid JSON, a number where text
    /// is expected, an unknown enum word) are reported with the line number from the serializer. Semantic
    /// errors (missing label, incompatible types, ...) come from <see cref="GridConfigValidator"/> and are
    /// all collected in one pass, so the author can fix the whole file at once instead of one error per reload.
    /// </remarks>
    public static GridConfigParseResult Parse(string json, GridVocabulary? vocabulary = null)
    {
        GridConfig? config;
        try
        {
            config = JsonSerializer.Deserialize<GridConfig>(json, Options);
        }
        catch (JsonException ex)
        {
            // LineNumber is zero-based, editors show lines from one.
            return new(null, [$"Neplatný JSON v konfiguraci gridu (řádek {(ex.LineNumber ?? 0) + 1}): {ex.Message}"]);
        }

        // "null" is valid JSON and deserializes to null; treat it as an empty file.
        if (config is null)
            return new(null, ["Konfigurace gridu je prázdná."]);

        var errors = new GridConfigValidator(vocabulary).Validate(config);
        return new(errors.Count == 0 ? config : null, errors);
    }
}
