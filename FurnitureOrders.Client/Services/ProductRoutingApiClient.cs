using System.Net.Http.Json;
using FurnitureOrders.Shared.Common;
using FurnitureOrders.Shared.Products;

namespace FurnitureOrders.Client.Services;

public class ProductRoutingApiClient(HttpClient http)
{
    private static string Root(int productId)
    {
        return $"api/products/{productId}";
    }

    // materiály
    public Task<ApiResult<IReadOnlyList<ProductMaterialDto>>> ListMaterialsAsync(int productId)
    {
        return ApiHttp.SendAsync<IReadOnlyList<ProductMaterialDto>>(() => http.GetAsync($"{Root(productId)}/materials"));
    }

    public Task<ApiResult<ProductMaterialDto>> CreateMaterialAsync(int productId, ProductMaterialEditDto dto)
    {
        return ApiHttp.SendAsync<ProductMaterialDto>(() => http.PostAsJsonAsync($"{Root(productId)}/materials", dto));
    }

    public Task<ApiResult<ProductMaterialDto>> UpdateMaterialAsync(int productId, int id, ProductMaterialEditDto dto)
    {
        return ApiHttp.SendAsync<ProductMaterialDto>(() => http.PutAsJsonAsync($"{Root(productId)}/materials/{id}", dto));
    }

    public Task<ApiResult<bool>> DeleteMaterialAsync(int productId, int id)
    {
        return ApiHttp.SendNoContentAsync(() => http.DeleteAsync($"{Root(productId)}/materials/{id}"));
    }

    // operace
    public Task<ApiResult<IReadOnlyList<ProductOperationDto>>> ListOperationsAsync(int productId)
    {
        return ApiHttp.SendAsync<IReadOnlyList<ProductOperationDto>>(() => http.GetAsync($"{Root(productId)}/operations"));
    }

    public Task<ApiResult<ProductOperationDto>> CreateOperationAsync(int productId, ProductOperationEditDto dto)
    {
        return ApiHttp.SendAsync<ProductOperationDto>(() => http.PostAsJsonAsync($"{Root(productId)}/operations", dto));
    }

    public Task<ApiResult<ProductOperationDto>> UpdateOperationAsync(int productId, int id, ProductOperationEditDto dto)
    {
        return ApiHttp.SendAsync<ProductOperationDto>(() => http.PutAsJsonAsync($"{Root(productId)}/operations/{id}", dto));
    }

    public Task<ApiResult<bool>> DeleteOperationAsync(int productId, int id)
    {
        return ApiHttp.SendNoContentAsync(() => http.DeleteAsync($"{Root(productId)}/operations/{id}"));
    }

    public Task<ApiResult<bool>> MoveOperationAsync(int productId, int id, int direction)
    {
        return ApiHttp.SendNoContentAsync(() =>
            http.PostAsJsonAsync($"{Root(productId)}/operations/{id}/move", new MoveRequest(direction)));
    }
}