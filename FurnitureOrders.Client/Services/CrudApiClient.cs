using System.Net;
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

    private static async Task<ApiResult<TEdit>> SendAsync(Func<Task<HttpResponseMessage>> send)
    {
        try
        {
            using var resp = await send();

            if (resp.IsSuccessStatusCode)
                return new ApiResult<TEdit> { Value = await resp.Content.ReadFromJsonAsync<TEdit>() };

            if (resp.StatusCode == HttpStatusCode.BadRequest)
            {
                var problem = await resp.Content.ReadFromJsonAsync<ValidationProblemResponse>();
                return problem?.Errors is { Count: > 0 } errors
                    ? new ApiResult<TEdit> { Errors = errors }
                    : new ApiResult<TEdit> { Message = "Neplatný požadavek." };
            }

            return new ApiResult<TEdit>
            {
                Message = resp.StatusCode == HttpStatusCode.NotFound
                    ? "Záznam nebyl nalezen."
                    : $"Server vrátil chybu {(int)resp.StatusCode}."
            };
        }
        catch (HttpRequestException)
        {
            return new ApiResult<TEdit> { Message = "Server není dostupný. Zkuste to prosím později." };
        }
    }
}