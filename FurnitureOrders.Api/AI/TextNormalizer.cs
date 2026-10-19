using System.Globalization;
using System.Text;

namespace FurnitureOrders.Api.Ai;

public static class TextNormalizer
{
    /// malá písmena, bez diakritiky, jen písmena a číslice oddělené mezerou
    public static string Normalize(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";

        var sb = new StringBuilder();
        foreach (var c in s.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            _ = sb.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : ' ');
        }
        return string.Join(' ', sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    public static IReadOnlyList<string> Tokens(string? s)
    {
        return Normalize(s).Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length >= 2).Distinct().ToList();
    }

    /// odstraní řídicí znaky, ořízne a zkrátí; používá se na vše, co přišlo od modelu
    public static string? Sanitize(string? s, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;

        var cleaned = new string(s.Where(c => !char.IsControl(c) || c is '\n').ToArray()).Trim();
        if (cleaned.Length == 0) return null;
        return cleaned.Length <= maxLength ? cleaned : cleaned[..maxLength];
    }
}