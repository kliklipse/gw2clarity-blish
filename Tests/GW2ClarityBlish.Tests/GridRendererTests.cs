using System.Numerics;
using GW2ClarityBlish.Models;
using GW2ClarityBlish.Rendering;

namespace GW2ClarityBlish.Tests;

public class GridRendererTests
{
    [Fact]
    public void BuildInstances_OneInstancePerItem_WithComputedPosition()
    {
        var buff = new Buff(id: 1, name: "Might");
        var grid = new Grid { Attached = false, Offset = Vector2.Zero, Spacing = (64, 64) };
        grid.Items.Add(new GridItem { Position = (1, 0), Buff = buff, StyleId = 0 });

        var style = new Style("Default", new[] { new Threshold(0, 99, new Appearance()) });
        var activeStacks = new Dictionary<uint, int> { [1] = 2 };

        var instances = GridRenderer.BuildInstances(
            grid, new[] { style }, activeStacks, screen: new Vector2(1920, 1080), mouse: Vector2.Zero, nowMs: 0);

        Assert.Single(instances);
        Assert.Equal(2, instances[0].Stacks);
        Assert.Equal(1u, instances[0].BuffId);
    }

    [Fact]
    public void BuildInstances_SkipsItem_WhenStyleMissing()
    {
        var buff = new Buff(id: 1, name: "Might");
        var grid = new Grid();
        grid.Items.Add(new GridItem { Position = (0, 0), Buff = buff, StyleId = 42 }); // style inexistant

        var instances = GridRenderer.BuildInstances(
            grid, styles: new List<Style>(), activeStacks: new Dictionary<uint, int>(),
            screen: new Vector2(1920, 1080), mouse: Vector2.Zero, nowMs: 0);

        Assert.Empty(instances);
    }
}
