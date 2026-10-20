using FurnitureOrders.Client.Services;
using FurnitureOrders.Shared.Enums;
using FurnitureOrders.Shared.Grid;

namespace FurnitureOrders.Client.Grid;

/// Registr výčtů pro options.source = "Enum". Nový výčet se přidá jedním řádkem.
public static class GridEnums
{
    private static readonly Dictionary<string, Func<IReadOnlyList<GridOption>>> Sources = new(StringComparer.OrdinalIgnoreCase)
    {
        ["CustomerType"] = () => Build<CustomerType>(Labels.Of),
        ["MaterialType"] = () => Build<MaterialType>(Labels.Of),
        ["OrderStatus"] = () => Build<OrderStatus>(Labels.Of)
    };

    public static void Register(string name, Func<IReadOnlyList<GridOption>> source)
    {
        Sources[name] = source;
    }

    public static IReadOnlyList<GridOption>? Get(string name)
    {
        return Sources.TryGetValue(name, out var source) ? source() : null;
    }

    private static IReadOnlyList<GridOption> Build<T>(Func<T, string> label) where T : struct, Enum
    {
        return Enum.GetValues<T>().Select(v => new GridOption(v.ToString(), label(v))).ToList();
    }
}