using FurnitureOrders.Shared.Materials;

namespace FurnitureOrders.Client.Services;

public class MaterialApiClient(HttpClient http)
    : CrudApiClient<MaterialListItemDto, MaterialEditDto>(http, "api/materials")
{
    public Task<IReadOnlyList<MaterialListItemDto>> ListAsync(MaterialQuery q, CancellationToken ct = default)
    {
        return ListRawAsync(QueryString.Build(
            ("search", q.Search), ("isActive", q.IsActive), ("type", q.Type is { } t ? (int)t : null),
            ("sortBy", q.SortBy), ("desc", q.Desc ? true : null)), ct);
    }
}