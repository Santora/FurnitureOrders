using FurnitureOrders.Api.Ai;
using FurnitureOrders.Api.Services;
using FurnitureOrders.Shared.Orders;

namespace FurnitureOrders.Api.Endpoints;

public static class OrderDraftEndpoints
{
    public static IEndpointRouteBuilder MapOrderDraftEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/orders").WithTags("Order AI draft");

        _ = g.MapGet("/ai-status", (IOrderDraftService svc) => Results.Ok(svc.Status()));

        // vrací pouze návrh; nic se neukládá
        _ = g.MapPost("/ai-draft", async (OrderDraftRequest request, IOrderDraftService svc, CancellationToken ct) =>
        {
            try
            {
                return (await svc.CreateDraftAsync(request, ct)).ToHttp(Results.Ok);
            }
            catch (AiException ex)
            {
                var (status, code) = ex.Kind switch
                {
                    AiErrorKind.NotConfigured => (503, "ai_not_configured"),
                    AiErrorKind.Timeout => (504, "ai_timeout"),
                    AiErrorKind.InvalidResponse => (502, "ai_invalid_response"),
                    _ => (503, "ai_unavailable")
                };
                return Results.Json(new AiErrorDto(code, ex.Message), statusCode: status);
            }
        });

        return app;
    }
}