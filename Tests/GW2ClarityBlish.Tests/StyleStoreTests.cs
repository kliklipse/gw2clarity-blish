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
    public void SaveThenLoad_RoundTripsAppearanceVectors()
    {
        // Regression : Vector4/Vector2 (System.Numerics) exposent leurs donnees comme des
        // CHAMPS publics (X/Y/Z/W), pas des proprietes - sans IncludeFields=true sur
        // JsonSerializerOptions, Tint/Border/Glow/GlowPulse se serialisaient en "{}" (tout a
        // zero, donc totalement transparent). Constate en test reel le 2026-09-02.
        var store = new StyleStore(_dir);
        var appearance = new Appearance
        {
            Tint = new System.Numerics.Vector4(1f, 0.5f, 0.2f, 0.9f),
            Border = new System.Numerics.Vector4(0, 0, 0, 1),
            GlowSize = 6f,
            GlowPulse = new System.Numerics.Vector2(0.3f, 2f),
        };
        var style = new Style("Test", new[] { new Threshold(0, 99, appearance) });

        store.Save(new List<Style> { style });
        var loaded = store.Load();

        var resolved = loaded[0].Resolve(count: 0, nowMs: 0);
        Assert.NotNull(resolved);
        Assert.Equal(new System.Numerics.Vector4(1f, 0.5f, 0.2f, 0.9f), resolved!.Tint);
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
