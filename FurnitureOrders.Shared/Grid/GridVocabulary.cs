namespace FurnitureOrders.Shared.Grid;

/// <summary>
/// The closed lists of type names a grid configuration may use. This is the single source of truth that the
/// validator consults, and it is the first place to look when asking "what can I put in the JSON?".
/// </summary>
/// <remarks>
/// <para>
/// The lists are instance members (not static constants) so a test or an extension can create a vocabulary
/// with extra names and pass it to <see cref="GridConfigParser.Parse"/>. All comparisons are case-insensitive.
/// </para>
/// <para>
/// A name here only means "the validator accepts it". Making it actually work needs the matching behaviour:
/// a registered renderer for display types (<see cref="GridCellRenderers"/>), a <c>case</c> in
/// <see cref="GridFiltering.Matches"/> and a control in the component for filter types, a source branch in
/// the component for option sources. The component reports an accepted-but-unimplemented display type as a
/// configuration error rather than silently showing plain text.
/// </para>
/// </remarks>
public sealed class GridVocabulary
{
    /// <summary>Semantic value types; verified against the CLR type of the bound property by <see cref="GridColumnBinder"/>.</summary>
    public HashSet<string> DataTypes { get; } = new(StringComparer.OrdinalIgnoreCase)
        { "String", "Integer", "Decimal", "Date", "DateTime", "Boolean", "Enum" };

    /// <summary>Ways to render a value; each needs a renderer in <see cref="GridCellRenderers"/>.</summary>
    public HashSet<string> DisplayTypes { get; } = new(StringComparer.OrdinalIgnoreCase)
        { "Text", "Number", "Decimal", "Date", "DateTime", "Checkbox", "Select", "DetailLink", "Currency" };

    /// <summary>Filter controls; each needs a branch in <see cref="GridFiltering.Matches"/> and in the component.</summary>
    public HashSet<string> FilterTypes { get; } = new(StringComparer.OrdinalIgnoreCase)
        { "TextBox", "NumberRange", "DateRange", "CheckBox", "Select" };

    /// <summary>Where a selection list gets its choices (see <see cref="GridOptionsConfig"/>).</summary>
    public HashSet<string> OptionSources { get; } = new(StringComparer.OrdinalIgnoreCase)
        { "Static", "Enum", "Distinct", "Lookup" };
}

/// <summary>Defaults applied when the configuration leaves something out.</summary>
public static class GridDefaults
{
    /// <summary>
    /// Filter type used when a filterable column does not specify one:
    /// numbers get a range, dates a date range, booleans a tri-state check box, enums a dropdown,
    /// and everything else (strings, unknown types) a text box.
    /// </summary>
    public static string FilterTypeFor(string? dataType)
    {
        return dataType?.ToLowerInvariant() switch
        {
            "integer" or "decimal" => "NumberRange",
            "date" or "datetime" => "DateRange",
            "boolean" => "CheckBox",
            "enum" => "Select",
            _ => "TextBox"
        };
    }
}
