using FurnitureOrders.Api.Services;
using FurnitureOrders.Shared.Common;
using FurnitureOrders.Shared.Customers;

namespace FurnitureOrders.Api.Endpoints;

public static class CustomerEndpoints
{
    public static IEndpointRouteBuilder MapCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/customers").WithTags("Customers");

        _ = g.MapGet("/", async ([AsParameters] CustomerQuery query, ICustomerService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAsync(query, ct)));

        _ = g.MapPost("/", async (CustomerEditDto dto, ICustomerService svc, CancellationToken ct) =>
            (await svc.CreateAsync(dto, ct)).ToHttp(c => Results.Created($"/api/customers/{c.Id}", c)));

        _ = g.MapPut("/{id:int}", async (int id, CustomerEditDto dto, ICustomerService svc, CancellationToken ct) =>
            (await svc.UpdateAsync(id, dto, ct)).ToHttp(Results.Ok));

        _ = g.MapPatch("/{id:int}/active", async (int id, SetActiveRequest req, ICustomerService svc, CancellationToken ct) =>
            (await svc.SetActiveAsync(id, req.IsActive, ct)).ToHttp(Results.Ok));

        _ = g.MapGet("/{id:int}", async (int id, ICustomerService svc, CancellationToken ct) =>
            await svc.GetAsync(id, ct) is { } c ? Results.Ok(c) : Results.NotFound());

        return app;
    }

}