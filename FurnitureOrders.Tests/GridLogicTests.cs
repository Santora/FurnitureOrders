using System.Globalization;
using FurnitureOrders.Shared.Enums;
using FurnitureOrders.Shared.Grid;

namespace FurnitureOrders.Tests;

public class GridLogicTests
{
    public sealed record Row(int Id, string Name, decimal Price, DateOnly Created, bool Active, OrderStatus Status, int? CustomerId);

    private static GridColumnConfig Col(string field, string dataType, string display = "Text", int order = 1, bool filterable = false, string? filterType = null)
    {
        return new() { Field = field, Label = field, DataType = dataType, DisplayType = display, Order = order, Filterable = filterable, FilterType = filterType };
    }

    private static GridConfig Config(params GridColumnConfig[] columns)
    {
        return new() { Entity = "E", Title = "T", Columns = columns.ToList() };
    }

    private static void WithInvariantCulture(Action action)
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        try { action(); }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    private static string Render(string display, object? value, string? format = null, IReadOnlyDictionary<string, string>? labels = null)
    {
        var column = new GridColumnConfig { Field = "X", DisplayType = display, Format = format };
        return GridCellRenderers.Get(display).Text(new GridCellContext(value, column, labels ?? new Dictionary<string, string>()));
    }

    // ---------- vazba sloupců

    [Fact]
    public void Bind_MissingProperty_ReportsError()
    {
        var (columns, errors) = GridColumnBinder.Bind<Row>(Config(Col("Neexistuje", "String")));

        Assert.Empty(columns);
        Assert.Contains(errors, e => e.Contains("Neexistuje"));
    }

    [Fact]
    public void Bind_DataTypeMismatch_ReportsError()
    {
        var (_, errors) = GridColumnBinder.Bind<Row>(Config(Col("Price", "String")));

        Assert.Contains(errors, e => e.Contains("očekává"));
    }

    [Fact]
    public void Bind_NullableProperty_MatchesNonNullableDataType()
    {
        var (columns, errors) = GridColumnBinder.Bind<Row>(Config(Col("CustomerId", "Integer", "Number")));

        Assert.Empty(errors);
        _ = Assert.Single(columns);
    }

    [Fact]
    public void Bind_IsCaseInsensitive_AndOrdersByOrderProperty()
    {
        var (columns, errors) = GridColumnBinder.Bind<Row>(Config(Col("price", "Decimal", "Decimal", order: 2), Col("NAME", "String", order: 1)));

        Assert.Empty(errors);
        Assert.Equal(["NAME", "price"], columns.Select(c => c.Field));
    }

    [Fact]
    public void Bind_FilterTypeDefaultsFromDataType()
    {
        var (columns, _) = GridColumnBinder.Bind<Row>(Config(
            Col("Created", "Date", "Date", filterable: true),
            Col("Name", "String", filterable: true),
            Col("Id", "Integer", "Number")));

        Assert.Equal("DateRange", columns.Single(c => c.Field == "Created").FilterType);
        Assert.Equal("TextBox", columns.Single(c => c.Field == "Name").FilterType);
        Assert.Equal("", columns.Single(c => c.Field == "Id").FilterType);
    }

    [Fact]
    public void Getter_ReadsPropertyValue()
    {
        var getter = GridColumnBinder.TryGetGetter<Row>("price")!;

        Assert.Equal(12.5m, getter(new Row(1, "A", 12.5m, new DateOnly(2026, 1, 1), true, OrderStatus.New, null)));
        Assert.Null(GridColumnBinder.TryGetGetter<Row>("missing"));
    }

    // ---------- filtry

    [Theory]
    [InlineData("ALA", true)]
    [InlineData("xyz", false)]
    [InlineData("", true)]
    public void TextFilter_IsCaseInsensitiveContains(string text, bool expected)
    {
        Assert.Equal(expected, GridFiltering.Matches("Alabama", "TextBox", new FilterState { Text = text }));
    }

    [Fact]
    public void NumberRange_IsInclusive_AndRejectsNulls()
    {
        var f = new FilterState { Min = 10, Max = 20 };

        Assert.True(GridFiltering.Matches(10m, "NumberRange", f));
        Assert.True(GridFiltering.Matches(20, "NumberRange", f));
        Assert.False(GridFiltering.Matches(9.99m, "NumberRange", f));
        Assert.False(GridFiltering.Matches(21m, "NumberRange", f));
        Assert.False(GridFiltering.Matches(null, "NumberRange", f));
    }

    [Fact]
    public void DateRange_WorksForDateOnlyAndDateTime()
    {
        var f = new FilterState { From = new DateOnly(2026, 10, 1), To = new DateOnly(2026, 10, 31) };

        Assert.True(GridFiltering.Matches(new DateOnly(2026, 10, 15), "DateRange", f));
        Assert.True(GridFiltering.Matches(new DateTime(2026, 10, 31, 23, 0, 0), "DateRange", f));
        Assert.False(GridFiltering.Matches(new DateOnly(2026, 11, 1), "DateRange", f));
    }

    [Fact]
    public void CheckboxFilter_ComparesBoolean()
    {
        Assert.True(GridFiltering.Matches(true, "CheckBox", new FilterState { Flag = true }));
        Assert.False(GridFiltering.Matches(false, "CheckBox", new FilterState { Flag = true }));
        Assert.True(GridFiltering.Matches(false, "CheckBox", new FilterState()));
    }

    [Fact]
    public void SelectFilter_MatchesEnumNameAndNumber()
    {
        Assert.True(GridFiltering.Matches(OrderStatus.Confirmed, "Select", new FilterState { Selected = "Confirmed" }));
        Assert.False(GridFiltering.Matches(OrderStatus.New, "Select", new FilterState { Selected = "Confirmed" }));
        Assert.True(GridFiltering.Matches(5, "Select", new FilterState { Selected = "5" }));
    }

    [Fact]
    public void Comparer_SortsNullsFirst_AndStringsIgnoringCase()
    {
        var sorted = new object?[] { "b", null, "A", "c" }.OrderBy(x => x, GridFiltering.Comparer).ToList();

        Assert.Equal(new object?[] { null, "A", "b", "c" }, sorted);
    }

    // ---------- zobrazení

    [Fact]
    public void Currency_AndNumberFormats()
    {
        WithInvariantCulture(() =>
    {
        Assert.Equal("3,601.00 Kč", Render("Currency", 3601m));
        Assert.Equal("1,234", Render("Number", 1234));
        Assert.Equal("3601.0", Render("Decimal", 3601m, "0.0"));
    });
    }

    [Fact]
    public void Date_UsesConfiguredOrDefaultFormat()
    {
        WithInvariantCulture(() =>
    {
        Assert.Equal("30.11.2026", Render("Date", new DateOnly(2026, 11, 30)));
        Assert.Equal("2026-11-30", Render("Date", new DateOnly(2026, 11, 30), "yyyy-MM-dd"));
        Assert.Equal("30.11.2026 14:05", Render("DateTime", new DateTime(2026, 11, 30, 14, 5, 0)));
    });
    }

    [Fact]
    public void InvalidFormat_FallsBackInsteadOfThrowing()
    {
        WithInvariantCulture(() =>
    {
        Assert.Equal("3601", Render("Decimal", 3601m, "Q"));
    });
    }

    [Fact]
    public void Checkbox_Select_AndEmptyValues()
    {
        Assert.Equal("Ano", Render("Checkbox", true));
        Assert.Equal("Ne", Render("Checkbox", false));
        Assert.Equal("", Render("Checkbox", null));
        Assert.Equal("Potvrzená", Render("Select", OrderStatus.Confirmed, labels: new Dictionary<string, string> { ["Confirmed"] = "Potvrzená" }));
        Assert.Equal("New", Render("Select", OrderStatus.New));
        Assert.Equal("", Render("Currency", null));
    }

    [Fact]
    public void UnknownDisplayType_FallsBackToText_AndIsNotRegistered()
    {
        Assert.False(GridCellRenderers.IsRegistered("NeexistujiciTyp"));
        Assert.Equal("abc", Render("NeexistujiciTyp", "abc"));
    }

    [Fact]
    public void CustomRenderer_CanBeRegistered()
    {
        GridCellRenderers.Register("TestBadge", new CellRenderer(CellKind.Text, c => $"[{c.Value}]"));

        Assert.True(GridCellRenderers.IsRegistered("TestBadge"));
        Assert.Equal("[x]", Render("TestBadge", "x"));
    }

    // ---------- hodnoty

    [Fact]
    public void GridValue_ConvertsInvariantly()
    {
        Assert.Equal("1.5", GridValue.Key(1.5m));
        Assert.Null(GridValue.Key(null));
        Assert.Equal(2m, GridValue.ToDecimal(2));
        Assert.Null(GridValue.ToDecimal("abc"));
        Assert.Equal(new DateOnly(2026, 1, 2), GridValue.ToDate(new DateTime(2026, 1, 2, 5, 0, 0)));
    }
}