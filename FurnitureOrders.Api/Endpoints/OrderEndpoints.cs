using FurnitureOrders.Api.Services;
using FurnitureOrders.Shared.Orders;

namespace FurnitureOrders.Api.Endpoints;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/orders").WithTags("Orders");

        _ = g.MapGet("/", async ([AsParameters] OrderQuery query, IOrderService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAsync(query, ct)));

        _ = g.MapGet("/{id:int}", async (int id, IOrderService svc, CancellationToken ct) =>
            await svc.GetAsync(id, ct) is { } o ? Results.Ok(o) : Results.NotFound());

        _ = g.MapPost("/", async (OrderEditDto dto, IOrderService svc, CancellationToken ct) =>
            (await svc.CreateAsync(dto, ct)).ToHttp(o => Results.Created($"/api/orders/{o.Id}", o)));

        _ = g.MapPut("/{id:int}", async (int id, OrderEditDto dto, IOrderService svc, CancellationToken ct) =>
            (await svc.UpdateAsync(id, dto, ct)).ToHttp(Results.Ok));

        _ = g.MapPatch("/{id:int}/status", async (int id, SetOrderStatusRequest req, IOrderService svc, CancellationToken ct) =>
            (await svc.SetStatusAsync(id, req.Status, ct)).ToHttp(Results.Ok));

        return app;
    }
}