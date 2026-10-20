using System.Globalization;

namespace FurnitureOrders.Shared.Grid;

public sealed record GridOption(string Value, string Label);

public static class GridValue
{
    /// textový klíč hodnoty (pro filtry výběrem): čísla a výčty bez vlivu kultury
    public static string? Key(object? value)
    {
        return value switch
        {
            null => null,
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString()
        };
    }

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