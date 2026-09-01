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
    public void SaveThenLoad_RoundTripsVector2AndTupleFields()
    {
        // Regression : System.Numerics.Vector2 et les tuples (int X, int Y) exposent leurs
        // donnees comme des CHAMPS publics (X/Y, Item1/Item2), pas des proprietes -
        // System.Text.Json les ignore silencieusement sans IncludeFields=true, ce qui
        // serialisait Spacing/Offset/Position/Buff.Uv en "{}" (tout a zero). Constate en test
        // reel le 2026-09-02 : grille rendue en 1x1 pixel, invisible a l'ecran.
        var store = new GridStore(_dir);
        var grid = new Grid
        {
            Name = "Test",
            Spacing = (80, 40),
            Offset = new System.Numerics.Vector2(12, -7),
        };
        grid.Items.Add(new GridItem
        {
            Position = (3, -2),
            Buff = new Buff(30328, "Alacrity") { Uv = new System.Numerics.Vector2(0.5f, 0.25f) },
        });

        store.Save(new List<Grid> { grid });
        var loaded = store.Load();

        Assert.Equal((80, 40), loaded[0].Spacing);
        Assert.Equal(new System.Numerics.Vector2(12, -7), loaded[0].Offset);
        Assert.Equal((3, -2), loaded[0].Items[0].Position);
        Assert.Equal(new System.Numerics.Vector2(0.5f, 0.25f), loaded[0].Items[0].Buff.Uv);
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
