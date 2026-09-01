using System.IO;
using Xunit;
using GW2ClarityBlish.Models;
using GW2ClarityBlish.Services;

namespace GW2ClarityBlish.Tests;

public class LayoutStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "gw2clarity-tests-" + Guid.NewGuid());

    public LayoutStoreTests() => Directory.CreateDirectory(_dir);
    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Fact]
    public void SaveThenLoad_RoundTripsLayouts()
    {
        var store = new LayoutStore(_dir);
        var layouts = new List<Layout> { new() { Name = "Combat Set" } };

        store.Save(layouts);
        var loaded = store.Load();

        Assert.Single(loaded);
        Assert.Equal("Combat Set", loaded[0].Name);
    }

    [Fact]
    public void Load_ReturnsEmptyList_WhenFileMissing()
    {
        var store = new LayoutStore(_dir);

        Assert.Empty(store.Load());
    }

    [Fact]
    public void Load_BacksUpCorruptFileAndReturnsEmptyList_WhenJsonInvalid()
    {
        var path = Path.Combine(_dir, "layouts.json");
        File.WriteAllText(path, "{ broken json ");
        var store = new LayoutStore(_dir);

        var loaded = store.Load();

        Assert.Empty(loaded);
        Assert.True(Directory.GetFiles(_dir, "layouts.json.corrupt-*").Length == 1);
    }
}
