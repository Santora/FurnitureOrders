using System.Net;
using System.Net.Http.Json;
using FurnitureOrders.Shared.Common;
using FurnitureOrders.Shared.Customers;

namespace FurnitureOrders.Client.Services;

public class CustomerApiClient(HttpClient http)
{
    public async Task<IReadOnlyList<CustomerListItemDto>> ListAsync(CustomerQuery q, CancellationToken ct = default)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(q.Search)) parts.Add($"search={Uri.EscapeDataString(q.Search.Trim())}");
        if (q.IsActive is { } a) parts.Add($"isActive={a.ToString().ToLowerInvariant()}");
        if (!string.IsNullOrWhiteSpace(q.SortBy)) parts.Add($"sortBy={Uri.EscapeDataString(q.SortBy)}");
        if (q.Desc) parts.Add("desc=true");

        var url = "api/customers" + (parts.Count > 0 ? "?" + string.Join("&", parts) : "");
        return await http.GetFromJsonAsync<List<CustomerListItemDto>>(url, ct) ?? [];
    }

    public Task<ApiResult<CustomerEditDto>> GetAsync(int id)
    {
        return SendAsync<CustomerEditDto>(() => http.GetAsync($"api/customers/{id}"));
    }

    public Task<ApiResult<CustomerEditDto>> CreateAsync(CustomerEditDto dto)
    {
        return SendAsync<CustomerEditDto>(() => http.PostAsJsonAsync("api/customers", dto));
    }

    public Task<ApiResult<CustomerEditDto>> UpdateAsync(int id, CustomerEditDto dto)
    {
        return SendAsync<CustomerEditDto>(() => http.PutAsJsonAsync($"api/customers/{id}", dto));
    }

    public Task<ApiResult<CustomerEditDto>> SetActiveAsync(int id, bool isActive)
    {
        return SendAsync<CustomerEditDto>(() => http.PatchAsJsonAsync($"api/customers/{id}/active", new SetActiveRequest(isActive)));
    }

    private static async Task<ApiResult<T>> SendAsync<T>(Func<Task<HttpResponseMessage>> send)
    {
        try
        {
            using var resp = await send();

            if (resp.IsSuccessStatusCode)
                return new ApiResult<T> { Value = await resp.Content.ReadFromJsonAsync<T>() };

            if (resp.StatusCode == HttpStatusCode.BadRequest)
            {
                var problem = await resp.Content.ReadFromJsonAsync<ValidationProblemResponse>();
                return problem?.Errors is { Count: > 0 } errors
                    ? new ApiResult<T> { Errors = errors }
                    : new ApiResult<T> { Message = "Neplatný požadavek." };
            }

            return new ApiResult<T>
            {
                Message = resp.StatusCode == HttpStatusCode.NotFound
                    ? "Záznam nebyl nalezen."
                    : $"Server vrátil chybu {(int)resp.StatusCode}."
            };
        }
        catch (HttpRequestException)
        {
            return new ApiResult<T> { Message = "Server není dostupný. Zkuste to prosím později." };
        }
    }
}