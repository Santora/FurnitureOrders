namespace FurnitureOrders.Client.Services;

public sealed class ApiResult<T>
{
    public T? Value { get; init; }
    public Dictionary<string, string[]> Errors { get; init; } = [];
    public string? Message { get; init; }   // síťová nebo neočekávaná chyba

    public bool IsSuccess => Message is null && Errors.Count == 0 && Value is not null;
}

public sealed record ValidationProblemResponse(Dictionary<string, string[]>? Errors);