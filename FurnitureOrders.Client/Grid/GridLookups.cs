using FurnitureOrders.Client.Services;
using FurnitureOrders.Shared.Customers;
using FurnitureOrders.Shared.Grid;

namespace FurnitureOrders.Client.Grid;

/// <summary>
/// Registry of named providers for <c>"options": { "source": "Lookup", "lookup": "customers" }</c>:
/// choices that come from another entity (for example a customer dropdown on the orders grid).
/// </summary>
/// <remarks>
/// <para>
/// A lookup returns <see cref="GridOption"/> pairs where the value is the related entity's id as text,
/// so that it matches the key of a foreign-key column (<see cref="GridValue.Key"/>) and the filter can
/// compare them. The label is what the user sees.
/// </para>
/// <para>
/// This is a scoped service (registered in <c>Program.cs</c>) because providers use API clients. Like
/// <see cref="GridEnums"/>, it is an explicit list on purpose: a configuration file can only trigger the
/// loaders that a developer has deliberately exposed here. Each lookup is loaded once per grid initialisation;
/// there is no cache, which is adequate while a grid is opened occasionally.
/// </para>
/// <para>To add a lookup, add an entry to the dictionary that loads the list and maps it to options.</para>
/// </remarks>
public sealed class GridLookups(CustomerApiClient customers)
{
    private readonly Dictionary<string, Func<Task<IReadOnlyList<GridOption>>>> _providers = new(StringComparer.OrdinalIgnoreCase)
    {
        ["customers"] = async () =>
        {
            // Sorted by name for a usable dropdown; the label combines the number and the name
            // because two customers can share a name but never a customer number.
            var items = await customers.ListAsync(new CustomerQuery(null, null, "name", false));
            return items.Select(c => new GridOption(c.Id.ToString(), $"{c.CustomerNumber} – {c.Name}")).ToList();
        }
    };

    /// <summary>Whether a provider with this name exists (used to report a bad configuration early).</summary>
    public bool IsRegistered(string name)
    {
        return _providers.ContainsKey(name);
    }

    /// <summary>
    /// Runs the provider. Call only after <see cref="IsRegistered"/> returned true.
    /// May throw <see cref="HttpRequestException"/> when the API is unreachable; the component handles that
    /// and reports it as a configuration-level error for the affected column.
    /// </summary>
    public Task<IReadOnlyList<GridOption>> GetAsync(string name)
    {
        return _providers[name]();
    }
}
