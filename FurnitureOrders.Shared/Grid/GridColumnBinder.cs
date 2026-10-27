using System.Linq.Expressions;
using System.Reflection;

namespace FurnitureOrders.Shared.Grid;

/// <summary>
/// A column that has been successfully tied to a property of the row type: the configuration plus
/// everything the component needs at run time (a compiled getter, the effective filter, resolved options).
/// </summary>
/// <typeparam name="TItem">The row type (a DTO).</typeparam>
public sealed class GridColumn<TItem>
{
    /// <param name="config">The column definition from the configuration file.</param>
    /// <param name="get">Compiled accessor that reads the bound property from a row.</param>
    /// <param name="filterType">Effective filter type, or an empty string when the column is not filterable.</param>
    public GridColumn(GridColumnConfig config, Func<TItem, object?> get, string filterType)
    {
        Config = config;
        Get = get;
        FilterType = filterType;
    }

    /// <summary>The original column definition.</summary>
    public GridColumnConfig Config { get; }

    /// <summary>Column key (the configured field name); used for sorting, filter state and visibility.</summary>
    public string Field => Config.Field;

    /// <summary>
    /// Reads this column's value from a row. Built once from an expression tree, so no reflection lookup
    /// happens per cell (note that under WebAssembly, which has no JIT compiler, the runtime may interpret
    /// the compiled expression; it is still a plain delegate call from the caller's point of view).
    /// The value is boxed to <c>object</c> so that columns of different types can share one list.
    /// </summary>
    public Func<TItem, object?> Get { get; }

    /// <summary>
    /// Effective filter type: the configured one, else the default for the data type
    /// (<see cref="GridDefaults.FilterTypeFor"/>), else an empty string meaning "this column cannot be filtered".
    /// Using an empty string rather than <c>null</c> lets callers test <c>FilterType.Length &gt; 0</c> without null checks.
    /// </summary>
    public string FilterType { get; }                                    // empty string = column is not filterable

    /// <summary>
    /// Choices for a Select filter and Select renderer. Filled by the component after binding, because
    /// resolving them may need an HTTP call (Lookup) or the loaded data (Distinct).
    /// </summary>
    public IReadOnlyList<GridOption> Options { get; set; } = [];

    /// <summary>The same choices as a value-to-label dictionary, for fast lookups when rendering cells.</summary>
    public IReadOnlyDictionary<string, string> OptionLabels { get; set; } = new Dictionary<string, string>();
}

/// <summary>
/// Third stage of the configuration pipeline: connects the column definitions to the real properties of the
/// row type and produces fast accessors. This is where "the configuration is valid" becomes
/// "the configuration is valid <i>for this DTO</i>".
/// </summary>
/// <remarks>
/// <para>
/// <b>Why reflection plus compiled expressions.</b> The property name arrives as a string from JSON, so some
/// reflection is unavoidable. It is used once per column (to find the property and check its type) and then
/// replaced by a delegate built from an expression tree. Filtering, sorting and rendering touch every cell
/// of many rows, so they call that delegate instead of <see cref="PropertyInfo.GetValue(object?)"/> each time.
/// </para>
/// <para>
/// <b>Trimming.</b> The client is published as WebAssembly with the IL trimmer, which removes members that
/// are not referenced statically. Properties that are only reached by name from JSON would disappear. The
/// client project therefore lists <c>FurnitureOrders.Shared</c> as a <c>TrimmerRootAssembly</c> so the
/// DTO properties survive. A grid over a DTO defined in another assembly would need the same treatment.
/// </para>
/// <para>
/// <b>Security.</b> Only public instance properties with a getter can be reached, so a configuration file
/// cannot call methods or touch private state. A typo in the file produces an error, not an exception.
/// </para>
/// </remarks>
public static class GridColumnBinder
{
    // IgnoreCase: "orderNumber" in JSON finds OrderNumber. Public + Instance: nothing private or static is reachable.
    private const BindingFlags Flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;

    /// <summary>
    /// Builds a compiled getter for a single property, or returns <c>null</c> when the property does not exist,
    /// has no public getter, is an indexer, or its name is ambiguous. Also used by the component to resolve
    /// the <c>{Placeholders}</c> of <see cref="GridConfig.DetailUrl"/>.
    /// </summary>
    /// <param name="property">Property name, case-insensitive.</param>
    public static Func<TItem, object?>? TryGetGetter<TItem>(string property)
    {
        PropertyInfo? prop;
        try
        {
            prop = typeof(TItem).GetProperty(property, Flags);
        }
        catch (AmbiguousMatchException)
        {
            // Possible with IgnoreCase when a type has two properties whose names differ only by case.
            return null;
        }

        if (prop?.GetMethod is null || prop.GetIndexParameters().Length > 0) return null;

        // Equivalent of: (TItem item) => (object?)item.Property
        // Expression.Convert is what boxes value types (int, decimal, DateOnly, enums) into object.
        var param = Expression.Parameter(typeof(TItem), "item");
        var body = Expression.Convert(Expression.Property(param, prop), typeof(object));
        return Expression.Lambda<Func<TItem, object?>>(body, param).Compile();
    }

    /// <summary>
    /// Binds every configured column to a property of <typeparamref name="TItem"/>, verifying that the declared
    /// <c>dataType</c> fits the property's CLR type. Returns the columns ordered by <c>order</c>, plus all
    /// errors found. The caller must treat a non-empty error list as "do not render the grid".
    /// </summary>
    /// <remarks>
    /// A column with a problem is skipped and reported, but processing continues so that one run lists every
    /// broken column. The ordering is stable (LINQ <c>OrderBy</c>), so columns with equal <c>order</c> keep
    /// the sequence from the file.
    /// </remarks>
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

            // prop.Name (the real, correctly cased name) is passed on rather than col.Field.
            var getter = TryGetGetter<TItem>(prop.Name);
            if (getter is null)
            {
                errors.Add($"columns ({col.Field}): vlastnost nelze číst.");
                continue;
            }

            // Effective filter type, derived exactly as in GridConfigValidator.ValidateFilter.
            // Not filterable -> empty string, which the component treats as "no filter control".
            var filterType = !col.Filterable ? ""
                : string.IsNullOrWhiteSpace(col.FilterType) ? GridDefaults.FilterTypeFor(col.DataType)
                : col.FilterType!;

            columns.Add(new GridColumn<TItem>(col, getter, filterType));
        }

        return (columns, errors);
    }

    /// <summary>
    /// Compares the declared data type with the property's CLR type. Returns <c>null</c> when they fit,
    /// otherwise a message saying what was found and what was expected.
    /// </summary>
    /// <remarks>
    /// <c>Nullable&lt;T&gt;</c> is unwrapped first, so <c>int?</c> is accepted as <c>Integer</c>; a null value
    /// is later handled by filters (no match for a range) and renderers (empty text).
    /// The accepted sets are intentionally generous within a category (any integral type for <c>Integer</c>)
    /// but never cross categories (a string is never accepted as a number).
    /// </remarks>
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
