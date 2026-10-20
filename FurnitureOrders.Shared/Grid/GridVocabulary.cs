namespace FurnitureOrders.Shared.Grid;

/// Seznam podporovaných typů. Nový typ sloupce se přidá sem (a registrací rendereru na klientovi),
/// bez zásahu do validátoru nebo komponenty.
public sealed class GridVocabulary
{
    public HashSet<string> DataTypes { get; } = new(StringComparer.OrdinalIgnoreCase)
        { "String", "Integer", "Decimal", "Date", "DateTime", "Boolean", "Enum" };

    public HashSet<string> DisplayTypes { get; } = new(StringComparer.OrdinalIgnoreCase)
        { "Text", "Number", "Decimal", "Date", "DateTime", "Checkbox", "Select", "DetailLink", "Currency" };

    public HashSet<string> FilterTypes { get; } = new(StringComparer.OrdinalIgnoreCase)
        { "TextBox", "NumberRange", "DateRange", "CheckBox", "Select" };

    public HashSet<string> OptionSources { get; } = new(StringComparer.OrdinalIgnoreCase)
        { "Static", "Enum", "Distinct", "Lookup" };
}

public static class GridDefaults
{
    /// výchozí filtr podle datového typu, když konfigurace žádný neurčí
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