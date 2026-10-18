using Microsoft.AspNetCore.Components.Forms;

namespace FurnitureOrders.Client.Services;

public sealed class ServerValidation
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
        var model = _ctx.Model;
        var props = model.GetType().GetProperties();

        foreach (var (key, messages) in errors)
        {
            var prop = props.FirstOrDefault(p => string.Equals(p.Name, key, StringComparison.OrdinalIgnoreCase));
            if (prop is null) { General.AddRange(messages); continue; }

            foreach (var m in messages)
                _store.Add(new FieldIdentifier(model, prop.Name), m);
        }
        _ctx.NotifyValidationStateChanged();
    }
}