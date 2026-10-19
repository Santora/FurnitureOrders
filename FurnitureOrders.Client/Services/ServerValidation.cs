using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components.Forms;

namespace FurnitureOrders.Client.Services;

public sealed partial class ServerValidation
{
    private readonly EditContext _ctx;
    private readonly ValidationMessageStore _store;

    public List<string> General { get; } = [];

    public ServerValidation(EditContext ctx)
    {
        _ctx = ctx;
        _store = new ValidationMessageStore(ctx);
        ctx.OnFieldChanged += (_, e) => _store.Clear(e.FieldIdentifier);
        ctx.OnValidationRequested += (_, _) => { _store.Clear(); General.Clear(); };
    }

    public void Apply(IDictionary<string, string[]> errors)
    {
        foreach (var (key, messages) in errors)
        {
            var field = Resolve(_ctx.Model, key);
            if (field is null)
            {
                General.AddRange(messages);
                continue;
            }

            foreach (var m in messages)
                _store.Add(field.Value, m);
        }
        _ctx.NotifyValidationStateChanged();
    }

    /// Ověří prvky kolekce podle DataAnnotations a vrátí chyby ve tvaru "Items[0].Quantity".
    public static Dictionary<string, string[]> ValidateItems(string collection, IEnumerable<object> items)
    {
        var result = new Dictionary<string, string[]>();
        var index = 0;
        foreach (var item in items)
        {
            var results = new List<ValidationResult>();
            _ = Validator.TryValidateObject(item, new ValidationContext(item), results, validateAllProperties: true);

            foreach (var g in results
                .SelectMany(r => r.MemberNames.Select(n => (Name: n, Message: r.ErrorMessage ?? "Neplatná hodnota.")))
                .GroupBy(x => x.Name))
            {
                result[$"{collection}[{index}].{g.Key}"] = g.Select(x => x.Message).ToArray();
            }
            index++;
        }
        return result;
    }

    private static FieldIdentifier? Resolve(object model, string key)
    {
        var m = ItemKey().Match(key);
        if (m.Success)
        {
            var collection = FindProperty(model, m.Groups[1].Value)?.GetValue(model) as IList;
            var index = int.Parse(m.Groups[2].Value);
            if (collection is null || index >= collection.Count || collection[index] is not { } element)
                return null;

            var prop = FindProperty(element, m.Groups[3].Value);
            return prop is null ? null : new FieldIdentifier(element, prop.Name);
        }

        var p = FindProperty(model, key);
        return p is null ? null : new FieldIdentifier(model, p.Name);
    }

    private static PropertyInfo? FindProperty(object obj, string name)
    {
        return obj.GetType().GetProperties().FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    [GeneratedRegex(@"^(\w+)\[(\d+)\]\.(\w+)$")]
    private static partial Regex ItemKey();
}