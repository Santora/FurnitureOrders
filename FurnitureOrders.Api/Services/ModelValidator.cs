using System.ComponentModel.DataAnnotations;

namespace FurnitureOrders.Api.Services;

public static class ModelValidator
{
    public static Dictionary<string, string[]> Validate(object model)
    {
        var results = new List<ValidationResult>();
        _ = Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);

        return results
            .SelectMany(r => (r.MemberNames.Any() ? r.MemberNames : [""])
                .Select(m => (Member: m, Message: r.ErrorMessage ?? "Neplatná hodnota.")))
            .GroupBy(x => x.Member)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Message).ToArray());
    }
}