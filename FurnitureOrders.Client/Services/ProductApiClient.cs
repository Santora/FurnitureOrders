using System.Net.Http.Json;
using FurnitureOrders.Shared.Products;

namespace FurnitureOrders.Client.Services;

public class ProductApiClient(HttpClient http)
    : CrudApiClient<ProductListItemDto, ProductEditDto>(http, "api/products")
{
    private readonly HttpClient _http = http;

    public Task<IReadOnlyList<ProductListItemDto>> ListAsync(ProductQuery q, CancellationToken ct = default)
    {
        return ListRawAsync(QueryString.Build(
            ("search", q.Search), ("isActive", q.IsActive), ("group", q.Group),
            ("sortBy", q.SortBy), ("desc", q.Desc ? true : null)), ct);
    }

    public async Task<IReadOnlyList<string>> GroupsAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<string>>("api/products/groups") ?? [];
        }
        catch (HttpRequestException)
        {
            return [];
        }
    }
}