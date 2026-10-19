namespace FurnitureOrders.Shared.Orders;

public enum DraftItemStatus
{
    Matched = 1,            // jednoznačně přiřazeno
    NeedsConfirmation = 2,  // více kandidátů nebo nejisté přiřazení: musí vybrat uživatel
    Unknown = 3             // výrobek v evidenci není: uživatel přiřadí ručně, nebo odstraní
}

public record OrderDraftRequest(int? CustomerId, string? Text);

public record DraftProductDto(int ProductId, string Code, string Name, string Unit, decimal UnitPrice);

public record OrderDraftItemDto(
    string Mention,
    decimal? Quantity,
    DraftItemStatus Status,
    DraftProductDto? Product,                    // vyplněno jen u Matched
    IReadOnlyList<DraftProductDto> Candidates,   // u NeedsConfirmation
    IReadOnlyList<string> Warnings);

public record OrderDraftDto(
    int CustomerId,
    IReadOnlyList<OrderDraftItemDto> Items,
    DateOnly? RequiredDeliveryDate,
    string? DeliveryAddress,
    string? Note,
    IReadOnlyList<string> Warnings,
    string Provider);

public record AiStatusDto(bool Enabled, string Provider, int MaxInputLength);

public record AiErrorDto(string Code, string Message);