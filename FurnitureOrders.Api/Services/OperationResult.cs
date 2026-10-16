namespace FurnitureOrders.Api.Services;

public enum OperationStatus { Ok, NotFound, Invalid }

public sealed record OperationResult<T>(
    OperationStatus Status,
    T? Value = default,
    IReadOnlyDictionary<string, string[]>? Errors = null)
{
    public static OperationResult<T> Ok(T value)
    {
        return new(OperationStatus.Ok, value);
    }

    public static OperationResult<T> NotFound()
    {
        return new(OperationStatus.NotFound);
    }

    public static OperationResult<T> Invalid(IReadOnlyDictionary<string, string[]> errors)
    {
        return new(OperationStatus.Invalid, default, errors);
    }
}