namespace FurnitureOrders.Api.Ai;

public class AiOptions
{
    public string Provider { get; set; } = "Anthropic";   // Anthropic | Fake | None
    public int MaxInputLength { get; set; } = 4000;
    public int MaxItems { get; set; } = 50;
    public AnthropicOptions Anthropic { get; set; } = new();
}

public class AnthropicOptions
{
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "claude-haiku-5-5";
    public string BaseUrl { get; set; } = "https://api.anthropic.com/";
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxTokens { get; set; } = 1500;
}