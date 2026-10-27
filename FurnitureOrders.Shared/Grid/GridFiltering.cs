namespace FurnitureOrders.Shared.Grid;

/// <summary>
/// The current user input of one column's filter. A single flat class is used for all filter types;
/// each type reads only the members it needs, which keeps the component's state a simple
/// <c>Dictionary&lt;field, FilterState&gt;</c> without per-type classes.
/// </summary>
/// <remarks>
/// <list type="table">
/// <listheader><term>Filter type</term><description>Members used</description></listheader>
/// <item><term>TextBox</term><description><see cref="Text"/></description></item>
/// <item><term>NumberRange</term><description><see cref="Min"/>, <see cref="Max"/> (inclusive)</description></item>
/// <item><term>DateRange</term><description><see cref="From"/>, <see cref="To"/> (inclusive)</description></item>
/// <item><term>CheckBox</term><description><see cref="Flag"/> (<c>null</c> = all, otherwise exact match)</description></item>
/// <item><term>Select</term><description><see cref="Selected"/> (option value; empty = all)</description></item>
/// </list>
/// </remarks>
public sealed class FilterState
{
    /// <summary>Text to search for (contains, case-insensitive).</summary>
    public string? Text { get; set; }
    /// <summary>Lower bound of a numeric range, inclusive.</summary>
    public decimal? Min { get; set; }
    /// <summary>Upper bound of a numeric range, inclusive.</summary>
    public decimal? Max { get; set; }
    /// <summary>First day of a date range, inclusive.</summary>
    public DateOnly? From { get; set; }
    /// <summary>Last day of a date range, inclusive.</summary>
    public DateOnly? To { get; set; }
    /// <summary>Required boolean value, or <c>null</c> for "all".</summary>
    public bool? Flag { get; set; }
    /// <summary>Selected option value, or empty for "all".</summary>
    public string? Selected { get; set; }

    /// <summary>
    /// True when the user has set anything. The component skips inactive filters entirely, so an untouched
    /// column costs nothing while filtering.
    /// </summary>
    public bool IsActive =>
        !string.IsNullOrWhiteSpace(Text) || Min is not null || Max is not null ||
        From is not null || To is not null || Flag is not null || !string.IsNullOrEmpty(Selected);
}

/// <summary>
/// Pure filtering and sorting logic of the grid: no UI, no state, no I/O, so every rule can be unit tested directly.
/// </summary>
/// <remarks>
/// <b>Extending with a new filter type</b> is not config-only. It needs (1) a new <c>case</c> in
/// <see cref="Matches"/>, (2) the name in <see cref="GridVocabulary.FilterTypes"/>, optionally a default in
/// <see cref="GridDefaults.FilterTypeFor"/> and a compatibility entry in the validator, and (3) the matching input
/// control in the filter block of <c>DynamicGrid.razor</c>. This is listed as a known limitation.
/// </remarks>
public static class GridFiltering
{
    /// <summary>
    /// Decides whether a cell value passes a filter.
    /// </summary>
    /// <param name="value">The raw cell value (may be <c>null</c>).</param>
    /// <param name="filterType">Filter type name, case-insensitive.</param>
    /// <param name="f">The user's input for that column.</param>
    /// <returns>
    /// <c>true</c> when the row should stay visible. An empty input always matches. An unknown filter type
    /// also matches (it cannot reach this point for a validated configuration).
    /// </returns>
    /// <remarks>
    /// <b>Null handling.</b> Once a bound of a range is set, rows whose value is <c>null</c> (or not convertible)
    /// are excluded, because "unknown" cannot be shown to lie within a range.
    /// </remarks>
    public static bool Matches(object? value, string filterType, FilterState f)
    {
        switch (filterType.ToLowerInvariant())
        {
            case "textbox":
                // Searches the text key of the value, so it also works on numbers and enums.
                return string.IsNullOrWhiteSpace(f.Text)
                    || (GridValue.Key(value)?.Contains(f.Text.Trim(), StringComparison.OrdinalIgnoreCase) ?? false);

            case "numberrange":
                {
                    // Everything is compared as decimal, so int, long, double and decimal columns behave the same.
                    var n = GridValue.ToDecimal(value);
                    if (f.Min is { } min && (n is null || n < min)) return false;
                    if (f.Max is { } max && (n is null || n > max)) return false;
                    return true;
                }

            case "daterange":
                {
                    // Compared by calendar day: the time of day of a DateTime is ignored, so "to 31.10."
                    // includes everything that happened on 31.10.
                    var d = GridValue.ToDate(value);
                    if (f.From is { } from && (d is null || d < from)) return false;
                    if (f.To is { } to && (d is null || d > to)) return false;
                    return true;
                }

            case "checkbox":
                return f.Flag is null || (value is bool b && b == f.Flag);

            case "select":
                // Exact comparison of text keys. The option values must therefore be keys of the cell values
                // (an id as "5", an enum member as its name), see GridValue.Key.
                return string.IsNullOrEmpty(f.Selected) || GridValue.Key(value) == f.Selected;

            default:
                return true;
        }
    }

    /// <summary>
    /// Ordering used for column sorting. Handles <c>null</c>, strings and mixed types without throwing.
    /// </summary>
    public static readonly IComparer<object?> Comparer = Comparer<object?>.Create(Compare);

    // Rules, in order:
    //  1. null sorts first (so it ends up last in a descending sort);
    //  2. two strings: culture-aware and case-insensitive, so accented letters follow the rules of the current
    //     culture (in a Czech locale "č" sorts after "c", not at the end of the alphabet);
    //  3. otherwise the natural ordering of the type (numbers, dates, enums by their underlying value);
    //  4. if the two values are of different types and cannot be compared, compare their text.
    private static int Compare(object? a, object? b)
    {
        if (a is null) return b is null ? 0 : -1;
        if (b is null) return 1;

        if (a is string sa && b is string sb)
            return string.Compare(sa, sb, StringComparison.CurrentCultureIgnoreCase);

        if (a is IComparable ca)
        {
            try
            {
                return ca.CompareTo(b);
            }
            catch (ArgumentException)
            {
                // different types: falls through to the text comparison
            }
        }
        return string.Compare(a.ToString(), b.ToString(), StringComparison.CurrentCultureIgnoreCase);
    }
}
