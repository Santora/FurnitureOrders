using FurnitureOrders.Client.Services;
using FurnitureOrders.Shared.Enums;
using FurnitureOrders.Shared.Grid;

namespace FurnitureOrders.Client.Grid;

/// <summary>
/// Registry of the C# enums that a configuration may reference with
/// <c>"options": { "source": "Enum", "enumType": "OrderStatus" }</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why a registry instead of resolving the type by name.</b> Looking up a type from a string in the JSON
/// would need reflection over all loaded assemblies, would not survive IL trimming reliably, and would let a
/// configuration file instantiate any enum in the application. A short explicit list is safer, trim-safe and
/// shows at a glance what the grids can use.
/// </para>
/// <para>
/// The option <i>value</i> is the enum member name (e.g. <c>"Confirmed"</c>), which is exactly what
/// <see cref="GridValue.Key"/> produces for an enum cell, so the filter comparison works without conversion.
/// The <i>label</i> comes from the application's central label table (<c>Labels.Of</c>), so the grid shows the
/// same Czech wording as the rest of the UI.
/// </para>
/// <para>To make a new enum available, add one line to <c>Sources</c> (or call <see cref="Register"/>).</para>
/// </remarks>
public static class GridEnums
{
    // The values are factories (Func) rather than ready lists so labels are produced when requested
    // and the registry itself stays cheap to initialise.
    private static readonly Dictionary<string, Func<IReadOnlyList<GridOption>>> Sources = new(StringComparer.OrdinalIgnoreCase)
    {
        ["CustomerType"] = () => Build<CustomerType>(Labels.Of),
        ["MaterialType"] = () => Build<MaterialType>(Labels.Of),
        ["OrderStatus"] = () => Build<OrderStatus>(Labels.Of)
    };

    /// <summary>Adds or replaces an enum source under the name used in the configuration's <c>enumType</c>.</summary>
    public static void Register(string name, Func<IReadOnlyList<GridOption>> source)
    {
        Sources[name] = source;
    }

    /// <summary>Returns the options of the named enum, or <c>null</c> when the name is not registered.</summary>
    public static IReadOnlyList<GridOption>? Get(string name)
    {
        return Sources.TryGetValue(name, out var source) ? source() : null;
    }

    // All members in declaration order: that is the order they appear in the dropdown.
    private static IReadOnlyList<GridOption> Build<T>(Func<T, string> label) where T : struct, Enum
    {
        return Enum.GetValues<T>().Select(v => new GridOption(v.ToString(), label(v))).ToList();
    }
}
