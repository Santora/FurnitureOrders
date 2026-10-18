using System.Net.Http.Json;
using FurnitureOrders.Shared.Operations;

namespace FurnitureOrders.Client.Services;

public class OperationApiClient(HttpClient http)
    : CrudApiClient<OperationListItemDto, OperationEditDto>(http, "api/operations")
{
    private readonly HttpClient _http = http;

    public Task<IReadOnlyList<OperationListItemDto>> ListAsync(OperationQuery q, CancellationToken ct = default)
    {
        return ListRawAsync(QueryString.Build(
            ("search", q.Search), ("isActive", q.IsActive), ("workplace", q.Workplace),
            ("sortBy", q.SortBy), ("desc", q.Desc ? true : null)), ct);
    }

    public async Task<IReadOnlyList<string>> WorkplacesAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<string>>("api/operations/workplaces") ?? [];
        }
        catch (HttpRequestException)
        {
            return [];
        }
    }
}