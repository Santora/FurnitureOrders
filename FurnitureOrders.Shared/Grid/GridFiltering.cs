namespace FurnitureOrders.Shared.Grid;

public sealed class FilterState
{
    public string? Text { get; set; }
    public decimal? Min { get; set; }
    public decimal? Max { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public bool? Flag { get; set; }
    public string? Selected { get; set; }

    public bool IsActive =>
        !string.IsNullOrWhiteSpace(Text) || Min is not null || Max is not null ||
        From is not null || To is not null || Flag is not null || !string.IsNullOrEmpty(Selected);
}

public static class GridFiltering
{
    public static bool Matches(object? value, string filterType, FilterState f)
    {
        switch (filterType.ToLowerInvariant())
        {
            case "textbox":
                return string.IsNullOrWhiteSpace(f.Text)
                    || (GridValue.Key(value)?.Contains(f.Text.Trim(), StringComparison.OrdinalIgnoreCase) ?? false);

            case "numberrange":
                {
                    var n = GridValue.ToDecimal(value);
                    if (f.Min is { } min && (n is null || n < min)) return false;
                    if (f.Max is { } max && (n is null || n > max)) return false;
                    return true;
                }

            case "daterange":
                {
                    var d = GridValue.ToDate(value);
                    if (f.From is { } from && (d is null || d < from)) return false;
                    if (f.To is { } to && (d is null || d > to)) return false;
                    return true;
                }

            case "checkbox":
                return f.Flag is null || (value is bool b && b == f.Flag);

            case "select":
                return string.IsNullOrEmpty(f.Selected) || GridValue.Key(value) == f.Selected;

            default:
                return true;
        }
    }

    public static readonly IComparer<object?> Comparer = Comparer<object?>.Create(Compare);

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
                // různé typy: spadne na porovnání textu
            }
        }
        return string.Compare(a.ToString(), b.ToString(), StringComparison.CurrentCultureIgnoreCase);
    }
}