using System.Linq.Expressions;
using System.Reflection;

namespace FurnitureOrders.Shared.Grid;

public sealed class GridColumn<TItem>
{
    public GridColumn(GridColumnConfig config, Func<TItem, object?> get, string filterType)
    {
        Config = config;
        Get = get;
        FilterType = filterType;
    }

    public GridColumnConfig Config { get; }
    public string Field => Config.Field;
    public Func<TItem, object?> Get { get; }
    public string FilterType { get; }                                    // prázdný text = sloupec nelze filtrovat
    public IReadOnlyList<GridOption> Options { get; set; } = [];
    public IReadOnlyDictionary<string, string> OptionLabels { get; set; } = new Dictionary<string, string>();
}

public static class GridColumnBinder
{
    private const BindingFlags Flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;

    public static Func<TItem, object?>? TryGetGetter<TItem>(string property)
    {
        PropertyInfo? prop;
        try
        {
            prop = typeof(TItem).GetProperty(property, Flags);
        }
        catch (AmbiguousMatchException)
        {
            return null;
        }

        if (prop?.GetMethod is null || prop.GetIndexParameters().Length > 0) return null;

        var param = Expression.Parameter(typeof(TItem), "item");
        var body = Expression.Convert(Expression.Property(param, prop), typeof(object));
        return Expression.Lambda<Func<TItem, object?>>(body, param).Compile();
    }

    /// Ověří, že každý sloupec odpovídá vlastnosti typu TItem a že datový typ sedí; vrací sloupce seřazené podle "order".
    public static (List<GridColumn<TItem>> Columns, List<string> Errors) Bind<TItem>(GridConfig config)
    {
        var columns = new List<GridColumn<TItem>>();
        var errors = new List<string>();

        foreach (var col in config.Columns.OrderBy(c => c.Order))
        {
            PropertyInfo? prop;
            try
            {
                prop = typeof(TItem).GetProperty(col.Field, Flags);
            }
            catch (AmbiguousMatchException)
            {
                errors.Add($"columns ({col.Field}): název vlastnosti je nejednoznačný.");
                continue;
            }

            if (prop is null)
            {
                errors.Add($"columns ({col.Field}): typ {typeof(TItem).Name} nemá vlastnost '{col.Field}'.");
                continue;
            }

            var mismatch = CheckType(col.DataType, prop.PropertyType);
            if (mismatch is not null)
            {
                errors.Add($"columns ({col.Field}): {mismatch}");
                continue;
            }

            var getter = TryGetGetter<TItem>(prop.Name);
            if (getter is null)
            {
                errors.Add($"columns ({col.Field}): vlastnost nelze číst.");
                continue;
            }

            var filterType = !col.Filterable ? ""
                : string.IsNullOrWhiteSpace(col.FilterType) ? GridDefaults.FilterTypeFor(col.DataType)
                : col.FilterType!;

            columns.Add(new GridColumn<TItem>(col, getter, filterType));
        }

        return (columns, errors);
    }

    private static string? CheckType(string dataType, Type type)
    {
        var t = Nullable.GetUnderlyingType(type) ?? type;

        var (ok, expected) = dataType.ToLowerInvariant() switch
        {
            "string" => (t == typeof(string), "string"),
            "integer" => (t == typeof(int) || t == typeof(long) || t == typeof(short) || t == typeof(byte), "int nebo long"),
            "decimal" => (t == typeof(decimal) || t == typeof(double) || t == typeof(float), "decimal nebo double"),
            "date" => (t == typeof(DateOnly) || t == typeof(DateTime), "DateOnly nebo DateTime"),
            "datetime" => (t == typeof(DateTime) || t == typeof(DateTimeOffset), "DateTime nebo DateTimeOffset"),
            "boolean" => (t == typeof(bool), "bool"),
            "enum" => (t.IsEnum, "výčtový typ"),
            _ => (false, "podporovaný typ")
        };

        return ok ? null : $"vlastnost je typu {t.Name}, dataType '{dataType}' očekává {expected}.";
    }
}