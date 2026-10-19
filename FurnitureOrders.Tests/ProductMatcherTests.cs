using FurnitureOrders.Api.Ai;
using FurnitureOrders.Shared.Orders;

namespace FurnitureOrders.Tests;

public class ProductMatcherTests
{
    private static readonly Dictionary<string, CatalogEntry> Catalog = new(StringComparer.OrdinalIgnoreCase)
    {
        ["VYR-001"] = new(1, "VYR-001", "Psací stůl Alfa", "ks", 4990m),
        ["VYR-002"] = new(2, "VYR-002", "Skříňka Beta", "ks", 2490m),
        ["VYR-004"] = new(4, "VYR-004", "Police Delta", "ks", 1290m),
        ["VYR-005"] = new(5, "VYR-005", "Police Epsilon", "ks", 1590m)
    };

    private static RawItem Item(string mention, string? normalized, params string[] codes)
    {
        return new(mention, 1, normalized, codes);
    }

    [Fact]
    public void SingleCandidateMatchingAllWords_IsMatched()
    {
        var result = ProductMatcher.Match(Item("tři psací stoly Alfa", "psací stůl Alfa", "VYR-001"), Catalog);

        Assert.Equal(DraftItemStatus.Matched, result.Status);
        Assert.Equal("VYR-001", Assert.Single(result.Candidates).Code);
    }

    [Fact]
    public void MentionByCode_IsMatched()
    {
        var result = ProductMatcher.Match(Item("VYR-002", null, "VYR-002"), Catalog);

        Assert.Equal(DraftItemStatus.Matched, result.Status);
    }

    [Fact]
    public void MultipleCandidates_NeedConfirmation()
    {
        var result = ProductMatcher.Match(Item("police", "police", "VYR-004", "VYR-005"), Catalog);

        Assert.Equal(DraftItemStatus.NeedsConfirmation, result.Status);
        Assert.Equal(2, result.Candidates.Count);
    }

    [Fact]
    public void NoCandidates_IsUnknown()
    {
        var result = ProductMatcher.Match(Item("skříň Zeta", "skříň Zeta"), Catalog);

        Assert.Equal(DraftItemStatus.Unknown, result.Status);
        Assert.Empty(result.Candidates);
    }

    [Fact]
    public void CandidateSharingNoWordWithMention_IsRejected()
    {
        // model by "Zeta" tiše nahradil Skříňkou Beta: aplikace to nepřipustí
        var result = ProductMatcher.Match(Item("skříň Zeta", "skříň Zeta", "VYR-002"), Catalog);

        Assert.Equal(DraftItemStatus.Unknown, result.Status);
    }

    [Fact]
    public void PartialOverlap_NeverMatchesSilently()
    {
        // "psací stůl Gama" sdílí dvě ze tří slov s Alfou, ale to nestačí
        var result = ProductMatcher.Match(Item("psací stůl Gama", "psací stůl Gama", "VYR-001"), Catalog);

        Assert.Equal(DraftItemStatus.NeedsConfirmation, result.Status);
    }

    [Fact]
    public void CodeMissingInCatalog_IsIgnored()
    {
        var result = ProductMatcher.Match(Item("psací stůl Alfa", "psací stůl Alfa", "VYR-999"), Catalog);

        Assert.Equal(DraftItemStatus.Unknown, result.Status);
    }

    [Fact]
    public void EmptyMention_IsUnknown()
    {
        var result = ProductMatcher.Match(Item("  ", null, "VYR-001"), Catalog);

        Assert.Equal(DraftItemStatus.Unknown, result.Status);
    }
}