using FurnitureOrders.Shared.Grid;

namespace FurnitureOrders.Tests;

public class GridConfigTests
{
    private static GridConfigParseResult ParseColumn(string columnJson, string extra = "", GridVocabulary? vocabulary = null)
    {
        return GridConfigParser.Parse($$"""
            { "entity": "E", "title": "T", {{extra}} "columns": [ {{columnJson}} ] }
            """, vocabulary);
    }

    private const string TextColumn =
        """{ "field": "Name", "label": "Název", "dataType": "String", "displayType": "Text", "sortable": true, "filterable": true, "filterType": "TextBox" }""";

    private static void AssertError(GridConfigParseResult result, string fragment)
    {
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains(fragment));
    }

    [Fact]
    public void ValidConfiguration_IsParsed_WithDefaults()
    {
        var result = ParseColumn(TextColumn);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Equal(20, result.Config!.PageSize);
        Assert.True(result.Config.Columns[0].Visible);
    }

    [Fact]
    public void PropertyNames_AreCaseInsensitive()
    {
        var result = GridConfigParser.Parse("""
            { "Entity": "E", "Title": "T",
              "Columns": [ { "Field": "Name", "Label": "N", "DataType": "String", "DisplayType": "Text" } ] }
            """);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public void BrokenJson_ReturnsError_InsteadOfThrowing()
    {
        AssertError(GridConfigParser.Parse("{ \"entity\": "), "Neplatný JSON");
    }

    [Fact]
    public void EmptyDocument_ReturnsError()
    {
        AssertError(GridConfigParser.Parse("null"), "prázdná");
    }

    [Fact]
    public void NoColumns_IsRejected()
    {
        AssertError(GridConfigParser.Parse("""{ "entity": "E", "title": "T", "columns": [] }"""), "columns");
    }

    [Fact]
    public void DuplicateField_IsRejected()
    {
        AssertError(ParseColumn(TextColumn + "," + TextColumn), "vícekrát");
    }

    [Fact]
    public void UnknownDataType_IsRejected()
    {
        AssertError(ParseColumn("""{ "field": "X", "label": "X", "dataType": "Guid", "displayType": "Text" }"""), "dataType");
    }

    [Fact]
    public void UnknownDisplayType_IsRejected()
    {
        AssertError(ParseColumn("""{ "field": "X", "label": "X", "dataType": "String", "displayType": "Badge" }"""), "displayType");
    }

    [Theory]
    [InlineData("Currency", "String")]
    [InlineData("Date", "Integer")]
    [InlineData("Checkbox", "String")]
    public void IncompatibleDisplayType_IsRejected(string display, string data)
    {
        AssertError(ParseColumn($$"""{ "field": "X", "label": "X", "dataType": "{{data}}", "displayType": "{{display}}" }"""), "nelze použít");
    }

    [Fact]
    public void DetailLink_RequiresDetailUrl()
    {
        AssertError(ParseColumn("""{ "field": "X", "label": "X", "dataType": "String", "displayType": "DetailLink" }"""), "detailUrl");
    }

    [Fact]
    public void DetailLink_WithDetailUrl_IsValid()
    {
        var result = ParseColumn(
            """{ "field": "X", "label": "X", "dataType": "String", "displayType": "DetailLink" }""",
            """ "detailUrl": "orders/{Id}", """);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public void NumberRangeFilter_OnString_IsRejected()
    {
        AssertError(ParseColumn("""{ "field": "X", "label": "X", "dataType": "String", "displayType": "Text", "filterable": true, "filterType": "NumberRange" }"""), "filterType");
    }

    [Fact]
    public void FilterableWithoutFilterType_UsesDefaultForDataType()
    {
        var result = ParseColumn("""{ "field": "X", "label": "X", "dataType": "Date", "displayType": "Date", "filterable": true }""");

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Equal("DateRange", GridDefaults.FilterTypeFor("Date"));
    }

    [Fact]
    public void EnumFilterWithoutOptions_IsRejected()
    {
        AssertError(ParseColumn("""{ "field": "X", "label": "X", "dataType": "Enum", "displayType": "Text", "filterable": true }"""), "options");
    }

    [Theory]
    [InlineData("""{ "source": "Static", "items": [] }""", "items")]
    [InlineData("""{ "source": "Static", "items": [ { "value": "1", "label": "A" }, { "value": "1", "label": "B" } ] }""", "unikátní")]
    [InlineData("""{ "source": "Enum" }""", "enumType")]
    [InlineData("""{ "source": "Lookup" }""", "lookup")]
    [InlineData("""{ "source": "Nonsense" }""", "source")]
    public void InvalidOptions_AreRejected(string options, string expectedFragment)
    {
        AssertError(ParseColumn($$"""{ "field": "X", "label": "X", "dataType": "String", "displayType": "Text", "filterable": true, "filterType": "Select", "options": {{options}} }"""), expectedFragment);
    }

    [Fact]
    public void DefaultSort_MustReferenceSortableColumn()
    {
        AssertError(ParseColumn(TextColumn, """ "defaultSort": { "field": "Missing" }, """), "neexistuje");
        AssertError(ParseColumn(
            """{ "field": "X", "label": "X", "dataType": "String", "displayType": "Text" }""",
            """ "defaultSort": { "field": "X" }, """), "není řaditelný");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(500)]
    public void PageSize_MustBeInRange(int pageSize)
    {
        AssertError(ParseColumn(TextColumn, $$""" "pageSize": {{pageSize}}, """), "pageSize");
    }

    [Fact]
    public void CustomDisplayType_RequiresRegistrationInVocabulary()
    {
        const string column = """{ "field": "X", "label": "X", "dataType": "String", "displayType": "Badge" }""";

        AssertError(ParseColumn(column), "displayType");

        var vocabulary = new GridVocabulary();
        _ = vocabulary.DisplayTypes.Add("Badge");
        Assert.True(ParseColumn(column, vocabulary: vocabulary).IsValid);
    }

    [Fact]
    public void ShippedConfigurations_AreValid()
    {
        var directory = FindGridsDirectory();
        var files = Directory.GetFiles(directory, "*.json");

        Assert.NotEmpty(files);
        foreach (var file in files)
        {
            var result = GridConfigParser.Parse(File.ReadAllText(file));
            Assert.True(result.IsValid, $"{Path.GetFileName(file)}: {string.Join("; ", result.Errors)}");
        }
    }

    private static string FindGridsDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "FurnitureOrders.Client", "wwwroot", "grids")))
            dir = dir.Parent;

        return dir is null
            ? throw new DirectoryNotFoundException("Složka FurnitureOrders.Client/wwwroot/grids nebyla nalezena.")
            : Path.Combine(dir.FullName, "FurnitureOrders.Client", "wwwroot", "grids");
    }
}