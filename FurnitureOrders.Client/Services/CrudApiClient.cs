using System.Net.Http.Json;
using FurnitureOrders.Shared.Common;

namespace FurnitureOrders.Client.Services;

public abstract class CrudApiClient<TList, TEdit>(HttpClient http, string path)
{
    protected async Task<IReadOnlyList<TList>> ListRawAsync(string queryString, CancellationToken ct)
    {
        return await http.GetFromJsonAsync<List<TList>>(path + queryString, ct) ?? [];
    }

    public Task<ApiResult<TEdit>> GetAsync(int id)
    {
        return SendAsync(() => http.GetAsync($"{path}/{id}"));
    }

    public Task<ApiResult<TEdit>> CreateAsync(TEdit dto)
    {
        return SendAsync(() => http.PostAsJsonAsync(path, dto));
    }

    public Task<ApiResult<TEdit>> UpdateAsync(int id, TEdit dto)
    {
        return SendAsync(() => http.PutAsJsonAsync($"{path}/{id}", dto));
    }

    public Task<ApiResult<TEdit>> SetActiveAsync(int id, bool isActive)
    {
        return SendAsync(() => http.PatchAsJsonAsync($"{path}/{id}/active", new SetActiveRequest(isActive)));
    }

    private static Task<ApiResult<TEdit>> SendAsync(Func<Task<HttpResponseMessage>> send)
    {
        return ApiHttp.SendAsync<TEdit>(send);
    }
}