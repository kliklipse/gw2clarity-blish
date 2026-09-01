using System.IO;
using Xunit;
using GW2ClarityBlish.Models;
using GW2ClarityBlish.Services;

namespace GW2ClarityBlish.Tests;

public class StyleStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "gw2clarity-tests-" + Guid.NewGuid());

    public StyleStoreTests() => Directory.CreateDirectory(_dir);
    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Fact]
    public void SaveThenLoad_RoundTripsStyles()
    {
        var store = new StyleStore(_dir);
        var styles = new List<Style> { new("Health Bar", new[] { new Threshold(0, 100, new Appearance()) }) };

        store.Save(styles);
        var loaded = store.Load();

        Assert.Single(loaded);
        Assert.Equal("Health Bar", loaded[0].Name);
    }

    [Fact]
    public void Load_ReturnsEmptyList_WhenFileMissing()
    {
        var store = new StyleStore(_dir);

        Assert.Empty(store.Load());
    }

    [Fact]
    public void Load_BacksUpCorruptFileAndReturnsEmptyList_WhenJsonInvalid()
    {
        var path = Path.Combine(_dir, "styles.json");
        File.WriteAllText(path, "[ invalid json }");
        var store = new StyleStore(_dir);

        var loaded = store.Load();

        Assert.Empty(loaded);
        Assert.True(Directory.GetFiles(_dir, "styles.json.corrupt-*").Length == 1);
    }
}
