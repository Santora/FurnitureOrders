namespace FurnitureOrders.Shared.Grid;

/// <summary>
/// Second stage of the configuration pipeline: checks that a deserialized <see cref="GridConfig"/> is
/// internally consistent, without knowing which DTO the grid will show.
/// </summary>
/// <remarks>
/// <para>
/// <b>What it checks:</b> required values, ranges, uniqueness of columns, that every type name is known
/// to the <see cref="GridVocabulary"/>, that the combination of <c>dataType</c>, <c>displayType</c> and
/// <c>filterType</c> makes sense, and that the extra settings a type needs (<c>detailUrl</c>, <c>options</c>) are present.
/// </para>
/// <para>
/// <b>What it deliberately does not check:</b> whether a field exists on the DTO or whether the property's
/// CLR type matches. That needs the generic row type and is done later by <see cref="GridColumnBinder"/>.
/// The split keeps this class free of reflection and generics, so it can be tested with plain strings.
/// </para>
/// <para>
/// <b>Error reporting:</b> the validator never stops at the first problem. It collects everything so the
/// author sees the full list at once. Messages start with a path such as
/// <c>columns[3] (TotalPrice).displayType</c> that points to the exact place in the JSON.
/// </para>
/// </remarks>
public sealed class GridConfigValidator
{
    private static readonly string[] Numeric = ["Integer", "Decimal"];
    private static readonly string[] Dates = ["Date", "DateTime"];

    // Compatibility table: display type -> data types it can render.
    // A display type that is not listed here (Text, Select, DetailLink) accepts any data type:
    // Text and DetailLink fall back to ToString(), Select looks the value up in its option labels.
    private static readonly Dictionary<string, string[]> DisplayRules = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Number"] = Numeric,
        ["Decimal"] = Numeric,
        ["Currency"] = Numeric,
        ["Date"] = Dates,
        ["DateTime"] = Dates,
        ["Checkbox"] = ["Boolean"]
    };

    // Compatibility table: filter type -> data types it can filter.
    // TextBox and Select are not listed because they work on the text key of any value.
    private static readonly Dictionary<string, string[]> FilterRules = new(StringComparer.OrdinalIgnoreCase)
    {
        ["NumberRange"] = Numeric,
        ["DateRange"] = Dates,
        ["CheckBox"] = ["Boolean"]
    };

    private readonly GridVocabulary _vocabulary;

    /// <param name="vocabulary">Allowed type names; <c>null</c> means the built-in defaults.</param>
    public GridConfigValidator(GridVocabulary? vocabulary = null)
    {
        _vocabulary = vocabulary ?? new GridVocabulary();
    }

    /// <summary>Validates the whole configuration and returns all problems found (empty list = valid).</summary>
    public IReadOnlyList<string> Validate(GridConfig config)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(config.Entity)) errors.Add("entity: je povinné.");
        if (string.IsNullOrWhiteSpace(config.Title)) errors.Add("title: je povinné.");
        if (config.PageSize is < 5 or > 200) errors.Add("pageSize: musí být v rozsahu 5 až 200.");

        // Without columns there is nothing more to validate, and the loop below would be pointless.
        if (config.Columns is null || config.Columns.Count == 0)
        {
            errors.Add("columns: musí obsahovat alespoň jeden sloupec.");
            return errors;
        }

        // Field names are compared case-insensitively because the binder resolves properties that way too;
        // "Status" and "status" would otherwise be two columns bound to the same property.
        var fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < config.Columns.Count; i++)
            ValidateColumn(config, config.Columns[i], i, fields, errors);

        // The default sort is checked after the columns because it refers to them.
        if (config.DefaultSort is { } sort)
        {
            var target = config.Columns.FirstOrDefault(c =>
                string.Equals(c.Field, sort.Field, StringComparison.OrdinalIgnoreCase));

            if (target is null) errors.Add($"defaultSort.field: sloupec '{sort.Field}' neexistuje.");
            else if (!target.Sortable) errors.Add($"defaultSort.field: sloupec '{sort.Field}' není řaditelný.");
        }

        return errors;
    }

    private void ValidateColumn(GridConfig config, GridColumnConfig col, int index, HashSet<string> fields, List<string> errors)
    {
        // Error prefix: the index always, the field name when there is one, so the message can be found in the file.
        var p = string.IsNullOrWhiteSpace(col.Field) ? $"columns[{index}]" : $"columns[{index}] ({col.Field})";

        if (string.IsNullOrWhiteSpace(col.Field)) errors.Add($"{p}.field: je povinné.");
        else if (!fields.Add(col.Field)) errors.Add($"{p}.field: sloupec je v konfiguraci vícekrát.");

        if (string.IsNullOrWhiteSpace(col.Label)) errors.Add($"{p}.label: je povinné.");

        // An unknown dataType is reported once here; the "dataTypeKnown" flag then stops the compatibility
        // checks below from piling secondary errors on top of the same root cause.
        var dataType = col.DataType ?? "";
        var dataTypeKnown = _vocabulary.DataTypes.Contains(dataType);
        if (!dataTypeKnown)
            errors.Add($"{p}.dataType: neznámá hodnota '{col.DataType}'. Podporováno: {string.Join(", ", _vocabulary.DataTypes)}.");

        var display = col.DisplayType ?? "";
        if (!_vocabulary.DisplayTypes.Contains(display))
            errors.Add($"{p}.displayType: neznámá hodnota '{col.DisplayType}'. Podporováno: {string.Join(", ", _vocabulary.DisplayTypes)}.");
        else
        {
            // Does this way of displaying fit the data type? (e.g. Currency on a String is rejected)
            if (dataTypeKnown && DisplayRules.TryGetValue(display, out var allowed)
                && !allowed.Contains(dataType, StringComparer.OrdinalIgnoreCase))
                errors.Add($"{p}.displayType: '{display}' nelze použít pro dataType '{dataType}'.");

            // A link needs a target. The check only looks for a placeholder; that the placeholder names a
            // real property is verified on the client, where the row type is known.
            if (display.Equals("DetailLink", StringComparison.OrdinalIgnoreCase) && !(config.DetailUrl?.Contains('{') ?? false))
                errors.Add($"{p}.displayType: DetailLink vyžaduje 'detailUrl' se zástupným výrazem, např. \"orders/{{Id}}\".");

            // Select translates stored values into labels, so it needs a source of those labels.
            if (display.Equals("Select", StringComparison.OrdinalIgnoreCase) && col.Options is null)
                errors.Add($"{p}.displayType: Select vyžaduje 'options'.");
        }

        // Filter and option settings are only checked where they take effect: a column that is not filterable
        // may carry a leftover filterType without causing an error, but options are always checked if present.
        if (col.Filterable) ValidateFilter(col, dataType, dataTypeKnown, p, errors);
        if (col.Options is not null) ValidateOptions(col.Options, p, errors);
    }

    private void ValidateFilter(GridColumnConfig col, string dataType, bool dataTypeKnown, string p, List<string> errors)
    {
        // The effective filter is the configured one or the default for the data type. It must be derived
        // the same way here as in GridColumnBinder, otherwise the validator would approve something the
        // binder then renders differently.
        var filterType = string.IsNullOrWhiteSpace(col.FilterType) ? GridDefaults.FilterTypeFor(dataType) : col.FilterType;

        if (!_vocabulary.FilterTypes.Contains(filterType))
        {
            errors.Add($"{p}.filterType: neznámá hodnota '{filterType}'. Podporováno: {string.Join(", ", _vocabulary.FilterTypes)}.");
            return;
        }

        if (dataTypeKnown && FilterRules.TryGetValue(filterType, out var allowed)
            && !allowed.Contains(dataType, StringComparer.OrdinalIgnoreCase))
            errors.Add($"{p}.filterType: '{filterType}' nelze použít pro dataType '{dataType}'.");

        if (filterType.Equals("Select", StringComparison.OrdinalIgnoreCase) && col.Options is null)
            errors.Add($"{p}.filterType: Select vyžaduje 'options' (zdroj hodnot).");
    }

    private void ValidateOptions(GridOptionsConfig options, string p, List<string> errors)
    {
        var source = options.Source ?? "";
        if (!_vocabulary.OptionSources.Contains(source))
        {
            errors.Add($"{p}.options.source: neznámá hodnota '{options.Source}'. Podporováno: {string.Join(", ", _vocabulary.OptionSources)}.");
            return;
        }

        // Each source has its own mandatory setting. "Distinct" needs none (it is computed from the data).
        // Whether a named enum or lookup is actually registered is a client concern, checked when options are resolved.
        switch (source.ToLowerInvariant())
        {
            case "static":
                if (options.Items is null || options.Items.Count == 0)
                    errors.Add($"{p}.options.items: Static vyžaduje alespoň jednu položku.");
                else if (options.Items.Select(i => i.Value).Distinct().Count() != options.Items.Count)
                    errors.Add($"{p}.options.items: hodnoty musí být unikátní.");
                break;
            case "enum":
                if (string.IsNullOrWhiteSpace(options.EnumType))
                    errors.Add($"{p}.options.enumType: Enum vyžaduje název výčtu.");
                break;
            case "lookup":
                if (string.IsNullOrWhiteSpace(options.Lookup))
                    errors.Add($"{p}.options.lookup: Lookup vyžaduje název poskytovatele.");
                break;
        }
    }
}
