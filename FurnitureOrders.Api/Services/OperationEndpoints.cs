using FurnitureOrders.Api.Endpoints;
using FurnitureOrders.Shared.Common;
using FurnitureOrders.Shared.Operations;

namespace FurnitureOrders.Api.Services;

public static class OperationEndpoints
{
    public static IEndpointRouteBuilder MapOperationEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/operations").WithTags("Operations");

        _ = g.MapGet("/", async ([AsParameters] OperationQuery query, IOperationService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAsync(query, ct)));

        _ = g.MapGet("/workplaces", async (IOperationService svc, CancellationToken ct) =>
            Results.Ok(await svc.WorkplacesAsync(ct)));

        _ = g.MapGet("/{id:int}", async (int id, IOperationService svc, CancellationToken ct) =>
            await svc.GetAsync(id, ct) is { } o ? Results.Ok(o) : Results.NotFound());

        _ = g.MapPost("/", async (OperationEditDto dto, IOperationService svc, CancellationToken ct) =>
            (await svc.CreateAsync(dto, ct)).ToHttp(o => Results.Created($"/api/operations/{o.Id}", o)));

        _ = g.MapPut("/{id:int}", async (int id, OperationEditDto dto, IOperationService svc, CancellationToken ct) =>
            (await svc.UpdateAsync(id, dto, ct)).ToHttp(Results.Ok));

        _ = g.MapPatch("/{id:int}/active", async (int id, SetActiveRequest req, IOperationService svc, CancellationToken ct) =>
            (await svc.SetActiveAsync(id, req.IsActive, ct)).ToHttp(Results.Ok));

        return app;
    }
}