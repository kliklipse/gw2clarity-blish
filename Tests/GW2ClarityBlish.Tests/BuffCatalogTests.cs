namespace GW2ClarityBlish.Tests;

using GW2ClarityBlish.Models;
using System.Linq;

public class BuffCatalogTests
{
    [Fact]
    public void Entries_IsNotEmpty()
    {
        Assert.NotEmpty(BuffCatalog.Entries);
    }

    [Fact]
    public void Entries_ContainsMight_WithCorrectId()
    {
        var might = BuffCatalog.Entries.SingleOrDefault(e => e.Id == 740);

        Assert.NotNull(might);
        Assert.Equal("Might", might!.Name);
    }

    [Fact]
    public void Search_LowercaseQuery_FindsMight_CaseInsensitive()
    {
        var results = BuffCatalog.Search("might");

        Assert.Contains(results, e => e.Name == "Might");
    }

    [Fact]
    public void Search_UppercaseQuery_StillFindsMatch_CaseInsensitive()
    {
        var results = BuffCatalog.Search("FURY");

        Assert.Contains(results, e => e.Name == "Fury");
    }

    [Fact]
    public void Search_NoMatch_ReturnsEmpty()
    {
        var results = BuffCatalog.Search("zzz-nonexistent-buff-zzz");

        Assert.Empty(results);
    }
}
