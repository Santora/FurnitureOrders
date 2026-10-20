namespace FurnitureOrders.Shared.Grid;

public enum GridSortDirection { Ascending, Descending }

public class GridConfig
{
    public string Entity { get; set; } = "";
    public string Title { get; set; } = "";
    public int PageSize { get; set; } = 20;
    public string? DetailUrl { get; set; }              // např. "orders/{Id}"
    public GridSortConfig? DefaultSort { get; set; }
    public List<GridColumnConfig> Columns { get; set; } = [];
}

public class GridSortConfig
{
    public string Field { get; set; } = "";
    public GridSortDirection Direction { get; set; } = GridSortDirection.Ascending;
}

public class GridColumnConfig
{
    public string Field { get; set; } = "";
    public string Label { get; set; } = "";
    public string DataType { get; set; } = "String";
    public string DisplayType { get; set; } = "Text";
    public string? FilterType { get; set; }
    public string? Format { get; set; }
    public int Order { get; set; }
    public bool Visible { get; set; } = true;
    public bool Sortable { get; set; }
    public bool Filterable { get; set; }
    public GridOptionsConfig? Options { get; set; }
}

/// Zdroj hodnot pro výběrový seznam: Static | Enum | Distinct | Lookup
public class GridOptionsConfig
{
    public string Source { get; set; } = "";
    public string? EnumType { get; set; }
    public string? Lookup { get; set; }
    public List<GridOptionItem> Items { get; set; } = [];
}

public class GridOptionItem
{
    public string Value { get; set; } = "";
    public string Label { get; set; } = "";
}