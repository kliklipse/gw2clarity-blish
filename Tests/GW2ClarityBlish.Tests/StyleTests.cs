namespace GW2ClarityBlish.Tests;

using GW2ClarityBlish.Models;
using System.Numerics;

public class StyleTests
{
    [Fact]
    public void Resolve_PicksHighestPriorityThreshold_OnOverlap()
    {
        var highPriority = new Threshold(Min: 0, Max: 10, Appearance: new Appearance { Tint = new Vector4(1, 0, 0, 1) });
        var lowPriority = new Threshold(Min: 0, Max: 99, Appearance: new Appearance { Tint = new Vector4(0, 1, 0, 1) });
        var style = new Style("Test", new[] { highPriority, lowPriority });

        var resolved = style.Resolve(count: 5, nowMs: 0);

        Assert.Equal(new Vector4(1, 0, 0, 1), resolved!.Tint);
    }

    [Fact]
    public void Resolve_UsesAppearanceAbove_WhenCountAtOrAbove100()
    {
        var above100 = new Threshold(Min: 0, Max: 100, Appearance: new Appearance { Tint = new Vector4(0, 0, 1, 1) });
        var style = new Style("Test", new[] { above100 });

        var resolved = style.Resolve(count: 250, nowMs: 0);

        Assert.Equal(new Vector4(0, 0, 1, 1), resolved!.Tint);
    }

    [Fact]
    public void Resolve_ReturnsNull_WhenCountNegative()
    {
        var style = new Style("Test", new[] { new Threshold(0, 10, new Appearance()) });

        Assert.Null(style.Resolve(count: -1, nowMs: 0));
    }

    [Fact]
    public void Resolve_ComputesPulsingGlowSize_WhenGlowPulseSet()
    {
        var appearance = new Appearance { GlowSize = 10f, GlowPulse = new Vector2(0.5f, 1f) };
        var style = new Style("Test", new[] { new Threshold(Min: 0, Max: 99, Appearance: appearance) });

        var resolvedAtT0 = style.Resolve(count: 1, nowMs: 0)!;
        var resolvedAtQuarterCycle = style.Resolve(count: 1, nowMs: 250)!;

        Assert.NotEqual(resolvedAtT0.GlowSizeX, resolvedAtQuarterCycle.GlowSizeX);
    }
}
