using System.ComponentModel.DataAnnotations;

namespace FurnitureOrders.Shared.Validation;

[AttributeUsage(AttributeTargets.Property)]
public sealed class OptionalEmailAttribute : ValidationAttribute
{
    private static readonly EmailAddressAttribute Inner = new();

    public OptionalEmailAttribute()
    {
        ErrorMessage = "E-mail nemá platný formát.";
    }

    public override bool IsValid(object? value)
    {
        return value is not string s || string.IsNullOrWhiteSpace(s) || Inner.IsValid(s.Trim());
    }
}