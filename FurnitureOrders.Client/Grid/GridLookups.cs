using FurnitureOrders.Client.Services;
using FurnitureOrders.Shared.Customers;
using FurnitureOrders.Shared.Grid;

namespace FurnitureOrders.Client.Grid;

/// Poskytovatelé hodnot pro options.source = "Lookup" (výběr ze související evidence).
public sealed class GridLookups(CustomerApiClient customers)
{
    private readonly Dictionary<string, Func<Task<IReadOnlyList<GridOption>>>> _providers = new(StringComparer.OrdinalIgnoreCase)
    {
        ["customers"] = async () =>
        {
            var items = await customers.ListAsync(new CustomerQuery(null, null, "name", false));
            return items.Select(c => new GridOption(c.Id.ToString(), $"{c.CustomerNumber} – {c.Name}")).ToList();
        }
    };

    public bool IsRegistered(string name)
    {
        return _providers.ContainsKey(name);
    }

    public Task<IReadOnlyList<GridOption>> GetAsync(string name)
    {
        return _providers[name]();
    }
}