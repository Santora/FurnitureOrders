using System.Net.Http.Json;
using FurnitureOrders.Shared.Orders;

namespace FurnitureOrders.Client.Services;

public class OrderApiClient(HttpClient http)
    : CrudApiClient<OrderListItemDto, OrderEditDto>(http, "api/orders")
{
    private readonly HttpClient _http = http;

    public Task<IReadOnlyList<OrderListItemDto>> ListAsync(OrderQuery q, CancellationToken ct = default)
    {
        return ListRawAsync(QueryString.Build(
            ("search", q.Search),
            ("status", q.Status is { } s ? (int)s : null),
            ("customerId", q.CustomerId),
            ("from", q.From), ("to", q.To),
            ("sortBy", q.SortBy), ("desc", q.Desc ? true : null)), ct);
    }

    public Task<ApiResult<OrderEditDto>> SetStatusAsync(int id, Shared.Enums.OrderStatus status)
    {
        return ApiHttp.SendAsync<OrderEditDto>(() =>
            _http.PatchAsJsonAsync($"api/orders/{id}/status", new SetOrderStatusRequest(status)));
    }
    public async Task<AiStatusDto?> GetAiStatusAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<AiStatusDto>("api/orders/ai-status");
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public Task<ApiResult<OrderDraftDto>> CreateDraftAsync(OrderDraftRequest request, CancellationToken ct = default)
    {
        return ApiHttp.SendAsync<OrderDraftDto>(() => _http.PostAsJsonAsync("api/orders/ai-draft", request, ct));
    }
}