using System.Collections.Generic;
using System.Numerics;

namespace GW2ClarityBlish.Models;

public class Grid
{
    public string Name { get; set; } = "New Grid";
    public (int X, int Y) Spacing { get; set; } = (64, 64);
    public Vector2 Offset { get; set; } = Vector2.Zero;
    public float CentralWeight { get; set; }
    public Vector2? MouseClipMin { get; set; }
    public Vector2? MouseClipMax { get; set; }
    public bool TrackMouseWhileHeld { get; set; } = true;
    public bool Attached { get; set; }
    public bool Square { get; set; } = true;
    public List<GridItem> Items { get; set; } = new();

    public Vector2 ComputeOrigin(Vector2 screen, Vector2 mouse, Vector2? heldMousePos)
    {
        if (!Attached)
            return screen * 0.5f + Offset;

        Vector2 origin;
        if (!TrackMouseWhileHeld && heldMousePos is not null)
            origin = heldMousePos.Value;
        else
            origin = mouse;

        if (MouseClipMin is not null && MouseClipMax is not null)
        {
            origin = Vector2.Max(origin, MouseClipMin.Value);
            origin = Vector2.Min(origin, MouseClipMax.Value);
        }

        if (CentralWeight > 0f)
            origin = Vector2.Lerp(origin, screen * 0.5f, CentralWeight);

        return origin;
    }
}
