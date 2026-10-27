using System.Globalization;

namespace FurnitureOrders.Shared.Grid;

/// <summary>One choice of a selection list: the technical <see cref="Value"/> and the text shown to the user.</summary>
/// <param name="Value">Compared with the cell value's key (see <see cref="GridValue.Key"/>).</param>
/// <param name="Label">Shown in the dropdown and, for Select cells, in the table.</param>
public sealed record GridOption(string Value, string Label);

/// <summary>
/// Conversions from a boxed cell value to a comparable form, shared by filtering, option lists and rendering.
/// </summary>
/// <remarks>
/// All conversions are <b>culture-invariant on purpose</b>. The values are used as identity (is this the selected
/// option?) and for comparison (is this within the range?), and that must not change with the user's locale.
/// A decimal id or a price would otherwise become "1,5" for one user and "1.5" for another. Culture-sensitive
/// output is the job of <see cref="GridCellRenderers"/>, which formats for display only.
/// </remarks>
public static class GridValue
{
    /// <summary>
    /// Text key of a value, used to compare against option values and for the text filter. Numbers and
    /// enum members are formatted with the invariant culture (an <c>int</c> 5 becomes <c>"5"</c>, an enum member its name).
    /// Returns <c>null</c> for <c>null</c>.
    /// </summary>
    public static string? Key(object? value)
    {
        return value switch
        {
            null => null,
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString()
        };
    }

    /// <summary>
    /// Converts any numeric value to <c>decimal</c> so ranges can be evaluated uniformly.
    /// Returns <c>null</c> for <c>null</c> or for a value that is not numeric or does not fit into a decimal
    /// (for example a very large <c>double</c>); it never throws.
    /// </summary>
    public static decimal? ToDecimal(object? value)
    {
        if (value is null) return null;
        try
        {
            return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
        {
            return null;
        }
    }

    /// <summary>
    /// Reduces a date or date-time value to its calendar day. A <see cref="DateTimeOffset"/> is first converted
    /// to local time, matching how the Date renderer displays it. Returns <c>null</c> for anything else.
    /// </summary>
    public static DateOnly? ToDate(object? value)
    {
        return value switch
        {
            DateOnly d => d,
            DateTime dt => DateOnly.FromDateTime(dt),
            DateTimeOffset o => DateOnly.FromDateTime(o.LocalDateTime),
            _ => null
        };
    }
}
