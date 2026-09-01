using System.IO;
using Xunit;
using GW2ClarityBlish.Models;
using GW2ClarityBlish.Services;

namespace GW2ClarityBlish.Tests;

public class GridStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "gw2clarity-tests-" + Guid.NewGuid());

    public GridStoreTests() => Directory.CreateDirectory(_dir);
    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Fact]
    public void SaveThenLoad_RoundTripsGrids()
    {
        var store = new GridStore(_dir);
        var grids = new List<Grid> { new() { Name = "DPS Rotation" } };

        store.Save(grids);
        var loaded = store.Load();

        Assert.Single(loaded);
        Assert.Equal("DPS Rotation", loaded[0].Name);
    }

    [Fact]
    public void Load_ReturnsEmptyList_WhenFileMissing()
    {
        var store = new GridStore(_dir);

        Assert.Empty(store.Load());
    }

    [Fact]
    public void Load_BacksUpCorruptFileAndReturnsEmptyList_WhenJsonInvalid()
    {
        var path = Path.Combine(_dir, "grids.json");
        File.WriteAllText(path, "{ not valid json ");
        var store = new GridStore(_dir);

        var loaded = store.Load();

        Assert.Empty(loaded);
        Assert.True(Directory.GetFiles(_dir, "grids.json.corrupt-*").Length == 1);
    }
}
