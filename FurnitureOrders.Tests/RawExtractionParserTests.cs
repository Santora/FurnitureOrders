using System.Text.Json;
using FurnitureOrders.Api.Ai;

namespace FurnitureOrders.Tests;

public class RawExtractionParserTests
{
    private static JsonElement Json(string json)
    {
        return JsonDocument.Parse(json).RootElement;
    }

    [Fact]
    public void ValidOutput_IsParsed()
    {
        var raw = RawExtractionParser.Parse(Json("""
            { "items": [ { "mention": "stůl Alfa", "normalized_name": "psací stůl Alfa",
                           "quantity": 3, "candidate_codes": ["VYR-001"] } ],
              "required_date": "2026-11-30", "delivery_address": "Brno",
              "customer_notes": null, "price_or_discount_requests": null }
            """));

        var item = Assert.Single(raw.Items);
        Assert.Equal(3m, item.Quantity);
        Assert.Equal("VYR-001", Assert.Single(item.CandidateCodes));
        Assert.Equal("2026-11-30", raw.RequiredDate);
        Assert.Null(raw.CustomerNotes);
    }

    [Fact]
    public void NullQuantity_IsAllowed()
    {
        var raw = RawExtractionParser.Parse(Json("""{ "items": [ { "mention": "stůl", "quantity": null } ] }"""));

        Assert.Null(Assert.Single(raw.Items).Quantity);
    }

    [Theory]
    [InlineData("""{ "items": "není pole" }""")]
    [InlineData("""{ "items": [ { "quantity": 1 } ] }""")]                                  // chybí mention
    [InlineData("""{ "items": [ { "mention": "x", "quantity": "tři" } ] }""")]              // množství není číslo
    [InlineData("""{ "items": [ { "mention": "x", "candidate_codes": "VYR-001" } ] }""")]   // kandidáti nejsou pole
    [InlineData("""{ "items": [], "required_date": 5 }""")]                                  // datum není text
    [InlineData("[]")]                                                                       // výstup není objekt
    public void MalformedOutput_IsRejected(string json)
    {
        var ex = Assert.Throws<AiException>(() => RawExtractionParser.Parse(Json(json)));

        Assert.Equal(AiErrorKind.InvalidResponse, ex.Kind);
    }

    [Fact]
    public void LongTextsFromModel_AreTruncated()
    {
        var raw = RawExtractionParser.Parse(Json($$"""{ "items": [ { "mention": "{{new string('a', 500)}}" } ] }"""));

        Assert.Equal(200, raw.Items[0].Mention.Length);
    }
}

public class TextNormalizerTests
{
    [Fact]
    public void Normalize_RemovesDiacriticsAndPunctuation()
    {
        Assert.Equal("psaci stul alfa", TextNormalizer.Normalize("Psací Stůl-Alfa!"));
    }

    [Fact]
    public void Tokens_IncludeCodeParts()
    {
        Assert.Contains("002", TextNormalizer.Tokens("Skříňka Beta VYR-002"));
    }

    [Fact]
    public void Sanitize_RemovesControlCharsAndTruncates()
    {
        Assert.Equal("ab", TextNormalizer.Sanitize("a\u0007b", 10));
        Assert.Equal("abc", TextNormalizer.Sanitize("abcdef", 3));
        Assert.Null(TextNormalizer.Sanitize("   ", 10));
    }
}