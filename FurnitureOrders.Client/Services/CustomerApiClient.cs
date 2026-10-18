using FurnitureOrders.Shared.Customers;

namespace FurnitureOrders.Client.Services;

public class CustomerApiClient(HttpClient http)
    : CrudApiClient<CustomerListItemDto, CustomerEditDto>(http, "api/customers")
{
    public Task<IReadOnlyList<CustomerListItemDto>> ListAsync(CustomerQuery q, CancellationToken ct = default)
    {
        return ListRawAsync(QueryString.Build(
            ("search", q.Search), ("isActive", q.IsActive),
            ("sortBy", q.SortBy), ("desc", q.Desc ? true : null)), ct);
    }
}