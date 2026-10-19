using Microsoft.EntityFrameworkCore;

namespace FurnitureOrders.Api.Services;

public static class NumberSequence
{
    /// Název sekvence se dosazuje do SQL, proto se smí předávat jen jako konstanta v kódu.
    public static async Task<int> NextAsync(DbContext db, string sequenceName, CancellationToken ct)
    {
        await db.Database.OpenConnectionAsync(ct);
        try
        {
            await using var cmd = db.Database.GetDbConnection().CreateCommand();
            cmd.CommandText = $"SELECT NEXT VALUE FOR {sequenceName}";
            return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }
}