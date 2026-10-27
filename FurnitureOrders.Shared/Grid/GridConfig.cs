namespace FurnitureOrders.Shared.Grid;

// =====================================================================================================
//  DYNAMIC GRID - ARCHITECTURE OVERVIEW
// =====================================================================================================
//
//  What it is
//  ----------
//  A reusable overview grid whose columns, filters, sorting and formatting are described by a JSON file
//  (FurnitureOrders.Client/wwwroot/grids/{name}.json) instead of being hard-coded in a Razor page.
//  Adding a new overview of an existing DTO means adding a JSON file and a three-line page; the grid
//  component itself is not touched.
//
//  Data flow (left to right)
//  -------------------------
//
//    wwwroot/grids/orders.json
//        |  GridConfigLoader (client)          HTTP GET, name whitelisted by a regex
//        v
//    GridConfigParser                          JSON text  -> GridConfig (this file), never throws on bad content
//        |
//        v
//    GridConfigValidator                       STATIC checks: required values, known type names, compatible
//        |                                     combinations (e.g. Currency needs a numeric dataType)
//        v
//    GridColumnBinder.Bind<TItem>              REFLECTION checks against the real DTO: does the property exist
//        |                                     and does its CLR type fit the declared dataType? Builds compiled
//        |                                     getters (Func<TItem, object?>) so rows are read without reflection
//        v
//    DynamicGrid<TItem> (client component)     resolves option lists, loads rows through the LoadItems delegate,
//                                              then filters -> sorts -> pages entirely in memory
//        |
//        +-- GridFiltering                     "does this value pass this filter?" and the sort comparer
//        +-- GridCellRenderers                 "how is this value shown as text?" (one renderer per displayType)
//        +-- GridValue                         culture-independent conversions shared by the two above
//
//  Where the code lives and why
//  ----------------------------
//  Everything that does not depend on Blazor (models, parser, validator, binder, filtering, formatting)
//  is in FurnitureOrders.Shared/Grid so it can be unit tested without a browser or a component host.
//  Only the pieces that need the Blazor runtime or the app's services (HttpClient, API clients, MudBlazor)
//  are in FurnitureOrders.Client: the component, the config loader, the enum registry and the lookup registry.
//
//  Three layers of protection against a broken configuration
//  ---------------------------------------------------------
//  1. Parser    - malformed JSON becomes a readable error (with a line number), not an exception.
//  2. Validator - the file is internally consistent (no DTO needed, so it is cheap and unit-testable).
//  3. Binder    - the file matches the DTO it is used with (needs TItem, so it can only run on the client).
//  All errors from all layers are collected and shown to the user as one list in a MudAlert instead of
//  crashing the page. The component never renders a half-valid grid.
//
//  Extension points (nothing else needs to change)
//  -----------------------------------------------
//  New display type  : GridCellRenderers.Register(...) + add the name to GridVocabulary.DisplayTypes.
//  New enum for lists: GridEnums.Register(...)                  (client)
//  New lookup list   : add a provider to GridLookups            (client)
//  Custom cell       : DynamicGrid.CellTemplates parameter, keyed by field name.
//  New filter type   : NOT config-only - needs GridFiltering.Matches, GridDefaults and the filter block in
//                      DynamicGrid.razor (listed as a known limitation in the README).
//
//  Known limitations (deliberate scope decisions for the test assignment)
//  ----------------------------------------------------------------------
//  * Filtering, sorting and paging run in memory over the complete list returned by LoadItems. That is fine
//    for hundreds or low thousands of rows; for more, the next step is a server-side query built from a
//    whitelist of the configured fields.
//  * The JSON is deserialized leniently: an unknown property name (for example a typo such as "sortabel")
//    is ignored rather than reported, and the column then silently keeps its default value.
//  * Column order and visibility are fixed by the file plus per-session user toggles; they are not persisted.
// =====================================================================================================

/// <summary>Direction of a sort. Serialized as a string in JSON (<c>"Ascending"</c> / <c>"Descending"</c>).</summary>
public enum GridSortDirection { Ascending, Descending }

/// <summary>
/// Root of one grid definition, i.e. the in-memory form of a single <c>wwwroot/grids/{name}.json</c> file.
/// </summary>
/// <remarks>
/// <para>
/// This is a plain data holder (a DTO for the configuration file). It contains no behaviour and no
/// validation: <see cref="GridConfigParser"/> creates it and <see cref="GridConfigValidator"/> checks it.
/// Property names in JSON are camelCase and matched case-insensitively.
/// </para>
/// <para>Minimal example:</para>
/// <code language="json">
/// {
///   "entity": "Order",
///   "title": "Orders",
///   "pageSize": 10,
///   "detailUrl": "orders/{Id}",
///   "defaultSort": { "field": "CreatedDate", "direction": "Descending" },
///   "columns": [
///     { "field": "OrderNumber", "label": "Number", "dataType": "String",
///       "displayType": "DetailLink", "filterable": true, "sortable": true, "order": 1 }
///   ]
/// }
/// </code>
/// </remarks>
public class GridConfig
{
    /// <summary>
    /// Logical name of the data set (for example <c>"Order"</c>). Required. Used for the CSV export file
    /// name (<c>order-20261027-0930.csv</c>); it is not used to look up any type, the row type is given to
    /// the component through its <c>TItem</c> type parameter.
    /// </summary>
    public string Entity { get; set; } = "";

    /// <summary>Heading shown above the grid. Required.</summary>
    public string Title { get; set; } = "";

    /// <summary>
    /// Rows per page. Validated to the range 5 to 200 so a typo cannot produce an unusable grid
    /// (one giant page, or a pager with thousands of pages).
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Optional URL template for columns with <c>displayType = "DetailLink"</c>, for example
    /// <c>"orders/{Id}"</c>. Each <c>{Name}</c> placeholder is replaced by the value of the row property
    /// with that name (matched case-insensitively) and URL-escaped. The placeholder does not have to be a
    /// configured column, any public property of the row type can be used. A DetailLink column requires
    /// this value to contain at least one placeholder (checked by the validator).
    /// </summary>
    public string? DetailUrl { get; set; }              // e.g. "orders/{Id}"

    /// <summary>
    /// Optional initial sort. Must reference an existing column that has <c>sortable = true</c>.
    /// When omitted, rows are shown in the order the data source returned them.
    /// </summary>
    public GridSortConfig? DefaultSort { get; set; }

    /// <summary>
    /// Column definitions. At least one is required. The order in this list is irrelevant for display:
    /// columns are arranged by <see cref="GridColumnConfig.Order"/> (see <see cref="GridColumnBinder.Bind{TItem}"/>).
    /// </summary>
    public List<GridColumnConfig> Columns { get; set; } = [];
}

/// <summary>Initial sort of the grid (see <see cref="GridConfig.DefaultSort"/>).</summary>
public class GridSortConfig
{
    /// <summary>Name of the column (its <see cref="GridColumnConfig.Field"/>), matched case-insensitively.</summary>
    public string Field { get; set; } = "";

    /// <summary>Sort direction, ascending by default.</summary>
    public GridSortDirection Direction { get; set; } = GridSortDirection.Ascending;
}

/// <summary>
/// Definition of one column: which property it shows, how the value is interpreted (<see cref="DataType"/>),
/// how it is rendered (<see cref="DisplayType"/>), and whether it can be sorted or filtered.
/// </summary>
/// <remarks>
/// <b>DataType vs. DisplayType.</b> These two are deliberately separate. <see cref="DataType"/> says what
/// the value <i>is</i> and is verified against the real CLR type of the DTO property (so the binder can
/// reject a configuration that calls a string an integer). <see cref="DisplayType"/> says how the value
/// <i>looks</i>. The same data type can be displayed in several ways (a <c>Decimal</c> can be shown as a plain
/// number or as <c>Currency</c>), and the validator keeps only meaningful pairs
/// (see <see cref="GridConfigValidator"/>).
/// </remarks>
public class GridColumnConfig
{
    /// <summary>
    /// Name of the public property on the row type (<c>TItem</c>) that supplies the value. Required and
    /// unique within the grid. Matched case-insensitively, so <c>"orderNumber"</c> works for
    /// <c>OrderNumber</c>. Also serves as the stable key for sorting, filters and column toggling.
    /// </summary>
    public string Field { get; set; } = "";

    /// <summary>Column header and filter caption shown to the user. Required.</summary>
    public string Label { get; set; } = "";

    /// <summary>
    /// Semantic type of the value: <c>String</c>, <c>Integer</c>, <c>Decimal</c>, <c>Date</c>,
    /// <c>DateTime</c>, <c>Boolean</c> or <c>Enum</c> (the allowed set lives in <see cref="GridVocabulary"/>).
    /// It decides which property types are accepted (<see cref="GridColumnBinder"/>), which filter is used by
    /// default (<see cref="GridDefaults.FilterTypeFor"/>) and the alignment (numbers are right-aligned).
    /// </summary>
    public string DataType { get; set; } = "String";

    /// <summary>
    /// How the value is rendered: <c>Text</c>, <c>Number</c>, <c>Decimal</c>, <c>Currency</c>, <c>Date</c>,
    /// <c>DateTime</c>, <c>Checkbox</c>, <c>Select</c> or <c>DetailLink</c>. Every name must also have a
    /// renderer registered in <see cref="GridCellRenderers"/>.
    /// </summary>
    public string DisplayType { get; set; } = "Text";

    /// <summary>
    /// Filter control for this column: <c>TextBox</c>, <c>NumberRange</c>, <c>DateRange</c>,
    /// <c>CheckBox</c> or <c>Select</c>. Only meaningful when <see cref="Filterable"/> is true.
    /// When left empty, a default is derived from <see cref="DataType"/> (see <see cref="GridDefaults.FilterTypeFor"/>).
    /// </summary>
    public string? FilterType { get; set; }

    /// <summary>
    /// Optional .NET format string passed to <c>ToString(format)</c> by the Number, Decimal, Currency, Date
    /// and DateTime renderers (for example <c>"N2"</c> or <c>"dd.MM.yyyy"</c>). When empty, each renderer
    /// uses its own default. An invalid format does not break the grid: the raw value is shown instead.
    /// </summary>
    public string? Format { get; set; }

    /// <summary>
    /// Position of the column from left to right (ascending). Columns with equal values keep the order in
    /// which they appear in the file (the sort is stable).
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Whether the column is shown initially. A hidden column is still bound, can still be filtered
    /// and can be switched on by the user through the "Columns" panel.
    /// </summary>
    public bool Visible { get; set; } = true;

    /// <summary>Whether clicking the header sorts by this column. Off by default (opt-in).</summary>
    public bool Sortable { get; set; }

    /// <summary>Whether a filter control is rendered for this column. Off by default (opt-in).</summary>
    public bool Filterable { get; set; }

    /// <summary>
    /// Source of the values for a <c>Select</c> display or a <c>Select</c> filter.
    /// Required when either of them is used on this column.
    /// </summary>
    public GridOptionsConfig? Options { get; set; }
}

/// <summary>
/// Where the choices for a selection list come from. Used by the <c>Select</c> filter (the dropdown) and by
/// the <c>Select</c> renderer (to translate the stored value into a human-readable label).
/// </summary>
/// <remarks>
/// <list type="table">
/// <listheader><term>Source</term><description>Meaning and required settings</description></listheader>
/// <item><term>Static</term><description>Fixed list written in the JSON (<see cref="Items"/>, at least one, unique values).</description></item>
/// <item><term>Enum</term><description>All members of a registered C# enum (<see cref="EnumType"/>, registered in <c>GridEnums</c>); labels come from the application's central label table.</description></item>
/// <item><term>Distinct</term><description>Unique values found in the loaded data, sorted alphabetically. Needs no extra settings; filled after the data is loaded.</description></item>
/// <item><term>Lookup</term><description>Values supplied by a named provider (<see cref="Lookup"/>, registered in <c>GridLookups</c>), typically a list from a related table such as customers (id as value, name as label).</description></item>
/// </list>
/// The value of an option is compared with the <i>key</i> of the cell value (see <see cref="GridValue.Key"/>),
/// so for a foreign key the option value must be the id converted to text.
/// </remarks>
public class GridOptionsConfig
{
    /// <summary><c>Static</c>, <c>Enum</c>, <c>Distinct</c> or <c>Lookup</c> (case-insensitive). Required.</summary>
    public string Source { get; set; } = "";

    /// <summary>Registered enum name; required for <c>Enum</c>, ignored otherwise.</summary>
    public string? EnumType { get; set; }

    /// <summary>Registered lookup provider name; required for <c>Lookup</c>, ignored otherwise.</summary>
    public string? Lookup { get; set; }

    /// <summary>Inline choices; required (non-empty, unique values) for <c>Static</c>, ignored otherwise.</summary>
    public List<GridOptionItem> Items { get; set; } = [];
}

/// <summary>One inline choice of a <c>Static</c> option list.</summary>
public class GridOptionItem
{
    /// <summary>Technical value that is compared with the cell value's key.</summary>
    public string Value { get; set; } = "";

    /// <summary>Text shown to the user.</summary>
    public string Label { get; set; } = "";
}
