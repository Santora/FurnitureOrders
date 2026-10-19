using FurnitureOrders.Shared.Enums;

namespace FurnitureOrders.Client.Services;

public static class Labels
{
    public static string Of(CustomerType t)
    {
        return t switch
        {
            CustomerType.Company => "Firma",
            CustomerType.Individual => "Fyzická osoba",
            _ => t.ToString()
        };
    }

    public static string Of(MaterialType t)
    {
        return t switch
        {
            MaterialType.Sheet => "Deskový materiál",
            MaterialType.Hardware => "Kování",
            MaterialType.Fastener => "Spojovací materiál",
            MaterialType.Packaging => "Obalový materiál",
            MaterialType.Other => "Ostatní",
            _ => t.ToString()
        };
    }

    public static string Of(OrderStatus s)
    {
        return s switch
        {
            OrderStatus.New => "Nová",
            OrderStatus.Confirmed => "Potvrzená",
            OrderStatus.InProduction => "Ve výrobě",
            OrderStatus.Completed => "Dokončená",
            OrderStatus.Cancelled => "Zrušená",
            _ => s.ToString()
        };
    }
}