using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace FurnitureOrders.Client.Services;

public static class ApiHttp
{
    public static async Task<ApiResult<T>> SendAsync<T>(Func<Task<HttpResponseMessage>> send)
    {
        try
        {
            using var resp = await send();

            if (resp.IsSuccessStatusCode)
                return new ApiResult<T> { Value = await resp.Content.ReadFromJsonAsync<T>() };

            return await FailureAsync<T>(resp);
        }
        catch (HttpRequestException)
        {
            return new ApiResult<T> { Message = "Server není dostupný. Zkuste to prosím později." };
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            return new ApiResult<T> { Message = "Server vrátil neplatnou odpověď." };
        }
    }

    public static async Task<ApiResult<bool>> SendNoContentAsync(Func<Task<HttpResponseMessage>> send)
    {
        try
        {
            using var resp = await send();

            return resp.IsSuccessStatusCode
                ? new ApiResult<bool> { Value = true }
                : await FailureAsync<bool>(resp);
        }
        catch (HttpRequestException)
        {
            return new ApiResult<bool> { Message = "Server není dostupný. Zkuste to prosím později." };
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            return new ApiResult<bool> { Message = "Server vrátil neplatnou odpověď." };
        }
    }

    private static async Task<ApiResult<T>> FailureAsync<T>(HttpResponseMessage resp)
    {
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
}