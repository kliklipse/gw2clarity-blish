using Xunit;
using GW2ClarityBlish.Models;
using System.Numerics;

namespace GW2ClarityBlish.Tests;

public class GridTests
{
    [Fact]
    public void ComputeOrigin_Unattached_UsesScreenCenterPlusOffset()
    {
        var grid = new Grid { Attached = false, Offset = new Vector2(10, -20) };

        var origin = grid.ComputeOrigin(screen: new Vector2(1920, 1080), mouse: new Vector2(500, 500), heldMousePos: null);

        Assert.Equal(new Vector2(970, 520), origin);
    }

    [Fact]
    public void ComputeOrigin_AttachedFollowingMouse_UsesMousePosition()
    {
        var grid = new Grid { Attached = true, TrackMouseWhileHeld = true };

        var origin = grid.ComputeOrigin(screen: new Vector2(1920, 1080), mouse: new Vector2(300, 400), heldMousePos: null);

        Assert.Equal(new Vector2(300, 400), origin);
    }

    [Fact]
    public void ComputeOrigin_AttachedNotTrackingWhileHeld_UsesHeldPosition()
    {
        var grid = new Grid { Attached = true, TrackMouseWhileHeld = false };

        var origin = grid.ComputeOrigin(screen: new Vector2(1920, 1080), mouse: new Vector2(300, 400), heldMousePos: new Vector2(111, 222));

        Assert.Equal(new Vector2(111, 222), origin);
    }

    [Fact]
    public void ComputeOrigin_ClampsToMouseClipBounds()
    {
        var grid = new Grid
        {
            Attached = true,
            MouseClipMin = new Vector2(200, 200),
            MouseClipMax = new Vector2(800, 800),
        };

        var origin = grid.ComputeOrigin(screen: new Vector2(1920, 1080), mouse: new Vector2(50, 900), heldMousePos: null);

        Assert.Equal(new Vector2(200, 800), origin);
    }

    [Fact]
    public void ComputeOrigin_BlendsTowardScreenCenter_WithCentralWeight()
    {
        var grid = new Grid { Attached = true, CentralWeight = 1f };

        var origin = grid.ComputeOrigin(screen: new Vector2(1000, 1000), mouse: new Vector2(0, 0), heldMousePos: null);

        Assert.Equal(new Vector2(500, 500), origin);
    }
}
