using FurnitureOrders.Api.Services;

namespace FurnitureOrders.Api.Endpoints;

public static class ResultExtensions
{
    public static IResult ToHttp<T>(this OperationResult<T> r, Func<T, IResult> ok)
    {
        return r.Status switch
        {
            OperationStatus.Ok => ok(r.Value!),
            OperationStatus.NotFound => Results.NotFound(),
            _ => Results.ValidationProblem(r.Errors!.ToDictionary(e => e.Key, e => e.Value))
        };
    }
}