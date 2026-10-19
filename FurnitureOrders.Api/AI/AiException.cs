namespace FurnitureOrders.Api.Ai;

public enum AiErrorKind { NotConfigured, Unavailable, Timeout, InvalidResponse }

public class AiException(AiErrorKind kind, string message, Exception? inner = null) : Exception(message, inner)
{
    public AiErrorKind Kind { get; } = kind;
}