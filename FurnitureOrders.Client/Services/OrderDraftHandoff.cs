using FurnitureOrders.Shared.Customers;
using FurnitureOrders.Shared.Orders;
using FurnitureOrders.Shared.Products;

namespace FurnitureOrders.Client.Services;

/// Předává upravený návrh z obrazovky AI do formuláře nové zakázky. Nic se neukládá.
public sealed class OrderDraftHandoff
{
    public sealed record Draft(
        OrderEditDto Model,
        CustomerListItemDto Customer,
        IReadOnlyDictionary<OrderItemEditDto, ProductListItemDto?> Products);

    private Draft? _pending;

    public void Set(Draft draft)
    {
        _pending = draft;
    }

    public Draft? Take()
    {
        var draft = _pending;
        _pending = null;
        return draft;
    }
}