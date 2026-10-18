using FurnitureOrders.Api.Services;
using FurnitureOrders.Shared.Common;
using FurnitureOrders.Shared.Products;

namespace FurnitureOrders.Api.Endpoints;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/products").WithTags("Products");

        _ = g.MapGet("/", async ([AsParameters] ProductQuery query, IProductService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAsync(query, ct)));

        _ = g.MapGet("/groups", async (IProductService svc, CancellationToken ct) =>
            Results.Ok(await svc.GroupsAsync(ct)));

        _ = g.MapGet("/{id:int}", async (int id, IProductService svc, CancellationToken ct) =>
            await svc.GetAsync(id, ct) is { } p ? Results.Ok(p) : Results.NotFound());

        _ = g.MapPost("/", async (ProductEditDto dto, IProductService svc, CancellationToken ct) =>
            (await svc.CreateAsync(dto, ct)).ToHttp(p => Results.Created($"/api/products/{p.Id}", p)));

        _ = g.MapPut("/{id:int}", async (int id, ProductEditDto dto, IProductService svc, CancellationToken ct) =>
            (await svc.UpdateAsync(id, dto, ct)).ToHttp(Results.Ok));

        _ = g.MapPatch("/{id:int}/active", async (int id, SetActiveRequest req, IProductService svc, CancellationToken ct) =>
            (await svc.SetActiveAsync(id, req.IsActive, ct)).ToHttp(Results.Ok));

        return app;
    }
}