using System.Globalization;

namespace FurnitureOrders.Shared.Grid;

public enum CellKind { Text, Check, Link }

public sealed record GridCellContext(object? Value, GridColumnConfig Column, IReadOnlyDictionary<string, string> OptionLabels);

public sealed record CellRenderer(CellKind Kind, Func<GridCellContext, string> Text);

/// Nový typ zobrazení: Register("Badge", ...) a přidání názvu do GridVocabulary.DisplayTypes.
public static class GridCellRenderers
{
    private static readonly Dictionary<string, CellRenderer> Renderers = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Text"] = new(CellKind.Text, c => c.Value?.ToString() ?? ""),
        ["Number"] = new(CellKind.Text, c => Number(c, "N0")),
        ["Decimal"] = new(CellKind.Text, c => Number(c, "N2")),
        ["Currency"] = new(CellKind.Text, c => { var s = Number(c, "N2"); return s.Length == 0 ? s : s + " Kč"; }),
        ["Date"] = new(CellKind.Text, c => Date(c, "dd.MM.yyyy")),
        ["DateTime"] = new(CellKind.Text, c => Date(c, "dd.MM.yyyy HH:mm")),
        ["Checkbox"] = new(CellKind.Check, c => c.Value is bool b ? (b ? "Ano" : "Ne") : ""),
        ["Select"] = new(CellKind.Text, Select),
        ["DetailLink"] = new(CellKind.Link, c => c.Value?.ToString() ?? "")
    };

    public static void Register(string displayType, CellRenderer renderer)
    {
        Renderers[displayType] = renderer;
    }

    public static bool IsRegistered(string displayType)
    {
        return Renderers.ContainsKey(displayType ?? "");
    }

    public static CellRenderer Get(string displayType)
    {
        return Renderers.TryGetValue(displayType ?? "", out var r) ? r : Renderers["Text"];
    }

    private static string FormatOf(GridCellContext c, string fallback)
    {
        return string.IsNullOrWhiteSpace(c.Column.Format) ? fallback : c.Column.Format;
    }

    private static string Number(GridCellContext c, string defaultFormat)
    {
        if (c.Value is not IFormattable f) return c.Value?.ToString() ?? "";
        try
        {
            return f.ToString(FormatOf(c, defaultFormat), CultureInfo.CurrentCulture);
        }
        catch (FormatException)
        {
            return c.Value.ToString() ?? "";   // chybný formát v konfiguraci nesmí shodit grid
        }
    }

    private static string Date(GridCellContext c, string defaultFormat)
    {
        var format = FormatOf(c, defaultFormat);
        var culture = CultureInfo.CurrentCulture;
        try
        {
            return c.Value switch
            {
                DateOnly d => d.ToString(format, culture),
                DateTime dt => dt.ToString(format, culture),
                DateTimeOffset o => o.LocalDateTime.ToString(format, culture),
                _ => c.Value?.ToString() ?? ""
            };
        }
        catch (FormatException)
        {
            return c.Value?.ToString() ?? "";
        }
    }

    private static string Select(GridCellContext c)
    {
        var key = GridValue.Key(c.Value);
        return key is null ? "" : c.OptionLabels.TryGetValue(key, out var label) ? label : key;
    }
}