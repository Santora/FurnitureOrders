using FurnitureOrders.Api.Services;
using FurnitureOrders.Shared.Common;
using FurnitureOrders.Shared.Products;

namespace FurnitureOrders.Api.Endpoints;

public static class ProductRoutingEndpoints
{
    public static IEndpointRouteBuilder MapProductRoutingEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/products/{productId:int}").WithTags("Product routing");

        // materiály výrobku
        _ = g.MapGet("/materials", async (int productId, IProductRoutingService svc, CancellationToken ct) =>
            (await svc.ListMaterialsAsync(productId, ct)).ToHttp(x => Results.Ok(x)));

        _ = g.MapPost("/materials", async (int productId, ProductMaterialEditDto dto, IProductRoutingService svc, CancellationToken ct) =>
            (await svc.CreateMaterialAsync(productId, dto, ct))
                .ToHttp(x => Results.Created($"/api/products/{productId}/materials/{x.Id}", x)));

        _ = g.MapPut("/materials/{id:int}", async (int productId, int id, ProductMaterialEditDto dto, IProductRoutingService svc, CancellationToken ct) =>
            (await svc.UpdateMaterialAsync(productId, id, dto, ct)).ToHttp(x => Results.Ok(x)));

        _ = g.MapDelete("/materials/{id:int}", async (int productId, int id, IProductRoutingService svc, CancellationToken ct) =>
            (await svc.DeleteMaterialAsync(productId, id, ct)).ToHttp(_ => Results.NoContent()));

        // operace výrobku
        _ = g.MapGet("/operations", async (int productId, IProductRoutingService svc, CancellationToken ct) =>
            (await svc.ListOperationsAsync(productId, ct)).ToHttp(x => Results.Ok(x)));

        _ = g.MapPost("/operations", async (int productId, ProductOperationEditDto dto, IProductRoutingService svc, CancellationToken ct) =>
            (await svc.CreateOperationAsync(productId, dto, ct))
                .ToHttp(x => Results.Created($"/api/products/{productId}/operations/{x.Id}", x)));

        _ = g.MapPut("/operations/{id:int}", async (int productId, int id, ProductOperationEditDto dto, IProductRoutingService svc, CancellationToken ct) =>
            (await svc.UpdateOperationAsync(productId, id, dto, ct)).ToHttp(x => Results.Ok(x)));

        _ = g.MapDelete("/operations/{id:int}", async (int productId, int id, IProductRoutingService svc, CancellationToken ct) =>
            (await svc.DeleteOperationAsync(productId, id, ct)).ToHttp(_ => Results.NoContent()));

        _ = g.MapPost("/operations/{id:int}/move", async (int productId, int id, MoveRequest req, IProductRoutingService svc, CancellationToken ct) =>
            (await svc.MoveOperationAsync(productId, id, req.Direction, ct)).ToHttp(_ => Results.NoContent()));

        return app;
    }
}