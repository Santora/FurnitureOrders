using System.Globalization;

namespace FurnitureOrders.Shared.Grid;

/// <summary>What kind of markup the component must produce for a cell.</summary>
public enum CellKind
{
    /// <summary>Plain text, rendered as a span.</summary>
    Text,
    /// <summary>A check box icon (the text is only used for export).</summary>
    Check,
    /// <summary>A link to the row's detail page, labelled with the text.</summary>
    Link
}

/// <summary>Everything a renderer needs to turn one cell value into text.</summary>
/// <param name="Value">The raw value read from the row (boxed; may be <c>null</c>).</param>
/// <param name="Column">The column definition (gives access to <see cref="GridColumnConfig.Format"/>).</param>
/// <param name="OptionLabels">Value-to-label map of the column's options, used by the Select renderer.</param>
public sealed record GridCellContext(object? Value, GridColumnConfig Column, IReadOnlyDictionary<string, string> OptionLabels);

/// <summary>
/// A cell renderer: the <see cref="Kind"/> tells the component what element to build, and
/// <see cref="Text"/> supplies the text for it.
/// </summary>
/// <param name="Kind">Which markup the component should produce.</param>
/// <param name="Text">Function producing the display text. It must not throw for any value.</param>
public sealed record CellRenderer(CellKind Kind, Func<GridCellContext, string> Text);

/// <summary>
/// Registry of the available <c>displayType</c> values and the logic that formats a value for each of them.
/// </summary>
/// <remarks>
/// <para>
/// <b>Design.</b> A renderer is split into a <i>kind</i> (what to draw) and a <i>text</i> function (what to
/// write). Because the text function is plain C# on <see cref="GridCellContext"/> and returns a string, the same
/// code feeds both the on-screen cell and the CSV export, and it can be unit tested without Blazor. Only the
/// kind is interpreted by the component, so adding a text-only display type needs no component change.
/// </para>
/// <para>
/// <b>Adding a display type</b> (for example a coloured badge): call <see cref="Register"/> with a name and a
/// <see cref="CellRenderer"/>, and add the same name to <see cref="GridVocabulary.DisplayTypes"/> so the
/// validator accepts it. If the new type needs a data-type restriction, add it to the validator's compatibility table.
/// A type that needs special markup (not text, check or link) additionally needs a new <see cref="CellKind"/> and
/// a branch in the component; for one-off markup on a single column use the component's <c>CellTemplates</c> parameter instead.
/// </para>
/// <para>
/// <b>Robustness rule.</b> Rendering must never throw because of a configuration or data oddity: an invalid
/// format string, a missing option label or an unexpected value type falls back to showing the raw value.
/// Formatting uses <see cref="CultureInfo.CurrentCulture"/> so numbers and dates follow the user's locale
/// (as opposed to <see cref="GridValue"/>, which is culture-invariant on purpose).
/// </para>
/// </remarks>
public static class GridCellRenderers
{
    // Case-insensitive so that "currency" and "Currency" in a hand-edited file both work.
    private static readonly Dictionary<string, CellRenderer> Renderers = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Text"] = new(CellKind.Text, c => c.Value?.ToString() ?? ""),
        ["Number"] = new(CellKind.Text, c => Number(c, "N0")),
        ["Decimal"] = new(CellKind.Text, c => Number(c, "N2")),
        // The currency symbol is appended as literal text (the application prices everything in Czech crowns),
        // so a custom Format such as "N0" controls only the number part.
        ["Currency"] = new(CellKind.Text, c => { var s = Number(c, "N2"); return s.Length == 0 ? s : s + " Kč"; }),
        ["Date"] = new(CellKind.Text, c => Date(c, "dd.MM.yyyy")),
        ["DateTime"] = new(CellKind.Text, c => Date(c, "dd.MM.yyyy HH:mm")),
        ["Checkbox"] = new(CellKind.Check, c => c.Value is bool b ? (b ? "Ano" : "Ne") : ""),
        ["Select"] = new(CellKind.Text, Select),
        // The text of a link is simply the value; the target comes from GridConfig.DetailUrl.
        ["DetailLink"] = new(CellKind.Link, c => c.Value?.ToString() ?? "")
    };

    /// <summary>
    /// Adds or replaces the renderer for a display type. Remember to add the name to
    /// <see cref="GridVocabulary.DisplayTypes"/> as well, otherwise the validator rejects it.
    /// </summary>
    public static void Register(string displayType, CellRenderer renderer)
    {
        Renderers[displayType] = renderer;
    }

    /// <summary>Whether a renderer exists for the display type. The component uses this to report an unregistered name.</summary>
    public static bool IsRegistered(string displayType)
    {
        return Renderers.ContainsKey(displayType ?? "");
    }

    /// <summary>Returns the renderer for the display type, falling back to plain <c>Text</c> for an unknown name.</summary>
    public static CellRenderer Get(string displayType)
    {
        return Renderers.TryGetValue(displayType ?? "", out var r) ? r : Renderers["Text"];
    }

    // The column's Format wins; otherwise the renderer's own default is used.
    private static string FormatOf(GridCellContext c, string fallback)
    {
        return string.IsNullOrWhiteSpace(c.Column.Format) ? fallback : c.Column.Format;
    }

    // Numbers: every numeric CLR type implements IFormattable. A null or non-numeric value is shown as is.
    private static string Number(GridCellContext c, string defaultFormat)
    {
        if (c.Value is not IFormattable f) return c.Value?.ToString() ?? "";
        try
        {
            return f.ToString(FormatOf(c, defaultFormat), CultureInfo.CurrentCulture);
        }
        catch (FormatException)
        {
            return c.Value.ToString() ?? "";   // an invalid format in the configuration must not bring the grid down
        }
    }

    // Dates: DateOnly, DateTime and DateTimeOffset have no common formatting base type that suits here, hence the switch.
    // DateTimeOffset is shown in the viewer's local time, consistent with GridValue.ToDate used by the date filter.
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

    // Select: translate the value's text key into its label. For a value without a matching option
    // (for example data newer than the list) the key itself is shown instead of an empty cell.
    private static string Select(GridCellContext c)
    {
        var key = GridValue.Key(c.Value);
        return key is null ? "" : c.OptionLabels.TryGetValue(key, out var label) ? label : key;
    }
}
