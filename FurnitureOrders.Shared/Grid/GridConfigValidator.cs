namespace FurnitureOrders.Shared.Grid;

public sealed class GridConfigValidator
{
    private static readonly string[] Numeric = ["Integer", "Decimal"];
    private static readonly string[] Dates = ["Date", "DateTime"];

    private static readonly Dictionary<string, string[]> DisplayRules = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Number"] = Numeric,
        ["Decimal"] = Numeric,
        ["Currency"] = Numeric,
        ["Date"] = Dates,
        ["DateTime"] = Dates,
        ["Checkbox"] = ["Boolean"]
    };

    private static readonly Dictionary<string, string[]> FilterRules = new(StringComparer.OrdinalIgnoreCase)
    {
        ["NumberRange"] = Numeric,
        ["DateRange"] = Dates,
        ["CheckBox"] = ["Boolean"]
    };

    private readonly GridVocabulary _vocabulary;

    public GridConfigValidator(GridVocabulary? vocabulary = null)
    {
        _vocabulary = vocabulary ?? new GridVocabulary();
    }

    public IReadOnlyList<string> Validate(GridConfig config)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(config.Entity)) errors.Add("entity: je povinné.");
        if (string.IsNullOrWhiteSpace(config.Title)) errors.Add("title: je povinné.");
        if (config.PageSize is < 5 or > 200) errors.Add("pageSize: musí být v rozsahu 5 až 200.");

        if (config.Columns is null || config.Columns.Count == 0)
        {
            errors.Add("columns: musí obsahovat alespoň jeden sloupec.");
            return errors;
        }

        var fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < config.Columns.Count; i++)
            ValidateColumn(config, config.Columns[i], i, fields, errors);

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
        var p = string.IsNullOrWhiteSpace(col.Field) ? $"columns[{index}]" : $"columns[{index}] ({col.Field})";

        if (string.IsNullOrWhiteSpace(col.Field)) errors.Add($"{p}.field: je povinné.");
        else if (!fields.Add(col.Field)) errors.Add($"{p}.field: sloupec je v konfiguraci vícekrát.");

        if (string.IsNullOrWhiteSpace(col.Label)) errors.Add($"{p}.label: je povinné.");

        var dataType = col.DataType ?? "";
        var dataTypeKnown = _vocabulary.DataTypes.Contains(dataType);
        if (!dataTypeKnown)
            errors.Add($"{p}.dataType: neznámá hodnota '{col.DataType}'. Podporováno: {string.Join(", ", _vocabulary.DataTypes)}.");

        var display = col.DisplayType ?? "";
        if (!_vocabulary.DisplayTypes.Contains(display))
            errors.Add($"{p}.displayType: neznámá hodnota '{col.DisplayType}'. Podporováno: {string.Join(", ", _vocabulary.DisplayTypes)}.");
        else
        {
            if (dataTypeKnown && DisplayRules.TryGetValue(display, out var allowed)
                && !allowed.Contains(dataType, StringComparer.OrdinalIgnoreCase))
                errors.Add($"{p}.displayType: '{display}' nelze použít pro dataType '{dataType}'.");

            if (display.Equals("DetailLink", StringComparison.OrdinalIgnoreCase) && !(config.DetailUrl?.Contains('{') ?? false))
                errors.Add($"{p}.displayType: DetailLink vyžaduje 'detailUrl' se zástupným výrazem, např. \"orders/{{Id}}\".");

            if (display.Equals("Select", StringComparison.OrdinalIgnoreCase) && col.Options is null)
                errors.Add($"{p}.displayType: Select vyžaduje 'options'.");
        }

        if (col.Filterable) ValidateFilter(col, dataType, dataTypeKnown, p, errors);
        if (col.Options is not null) ValidateOptions(col.Options, p, errors);
    }

    private void ValidateFilter(GridColumnConfig col, string dataType, bool dataTypeKnown, string p, List<string> errors)
    {
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