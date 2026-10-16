using FurnitureOrders.Api.Services;
using FurnitureOrders.Shared.Customers;

namespace FurnitureOrders.Api.Endpoints;

public static class CustomerEndpoints
{
    public static IEndpointRouteBuilder MapCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/customers").WithTags("Customers");

        _ = g.MapGet("/", async ([AsParameters] CustomerQuery query, ICustomerService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAsync(query, ct)));

        _ = g.MapGet("/{id:int}", async (int id, ICustomerService svc, CancellationToken ct) =>
            await svc.GetAsync(id, ct) is { } c ? Results.Ok(c) : Results.NotFound());

        _ = g.MapPost("/", async (CustomerEditDto dto, ICustomerService svc, CancellationToken ct) =>
            ToHttp(await svc.CreateAsync(dto, ct), c => Results.Created($"/api/customers/{c.Id}", c)));

        _ = g.MapPut("/{id:int}", async (int id, CustomerEditDto dto, ICustomerService svc, CancellationToken ct) =>
            ToHttp(await svc.UpdateAsync(id, dto, ct), Results.Ok));

        _ = g.MapPatch("/{id:int}/active", async (int id, SetActiveRequest req, ICustomerService svc, CancellationToken ct) =>
            ToHttp(await svc.SetActiveAsync(id, req.IsActive, ct), Results.Ok));

        return app;
    }

    private static IResult ToHttp<T>(OperationResult<T> r, Func<T, IResult> ok)
    {
        return r.Status switch
        {
            OperationStatus.Ok => ok(r.Value!),
            OperationStatus.NotFound => Results.NotFound(),
            _ => Results.ValidationProblem(r.Errors!.ToDictionary(e => e.Key, e => e.Value))
        };
    }
}