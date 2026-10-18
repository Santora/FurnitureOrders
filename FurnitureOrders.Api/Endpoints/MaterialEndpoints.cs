using FurnitureOrders.Api.Services;
using FurnitureOrders.Shared.Common;
using FurnitureOrders.Shared.Materials;

namespace FurnitureOrders.Api.Endpoints;

public static class MaterialEndpoints
{
    public static IEndpointRouteBuilder MapMaterialEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/materials").WithTags("Materials");

        _ = g.MapGet("/", async ([AsParameters] MaterialQuery query, IMaterialService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAsync(query, ct)));

        _ = g.MapGet("/{id:int}", async (int id, IMaterialService svc, CancellationToken ct) =>
            await svc.GetAsync(id, ct) is { } m ? Results.Ok(m) : Results.NotFound());

        _ = g.MapPost("/", async (MaterialEditDto dto, IMaterialService svc, CancellationToken ct) =>
            (await svc.CreateAsync(dto, ct)).ToHttp(m => Results.Created($"/api/materials/{m.Id}", m)));

        _ = g.MapPut("/{id:int}", async (int id, MaterialEditDto dto, IMaterialService svc, CancellationToken ct) =>
            (await svc.UpdateAsync(id, dto, ct)).ToHttp(Results.Ok));

        _ = g.MapPatch("/{id:int}/active", async (int id, SetActiveRequest req, IMaterialService svc, CancellationToken ct) =>
            (await svc.SetActiveAsync(id, req.IsActive, ct)).ToHttp(Results.Ok));

        return app;
    }
}