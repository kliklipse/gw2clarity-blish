using System.Numerics;
using GW2ClarityBlish.Models;

namespace GW2ClarityBlish.Rendering;

public static class GridRenderer
{
    public static List<GridInstanceData> BuildInstances(
        Grid grid, IReadOnlyList<Style> styles, IReadOnlyDictionary<uint, int> activeStacks,
        Vector2 screen, Vector2 mouse, long nowMs)
    {
        var origin = grid.ComputeOrigin(screen, mouse, heldMousePos: null);
        var result = new List<GridInstanceData>();

        foreach (var item in grid.Items)
        {
            var style = styles.Count > (int)item.StyleId ? styles[(int)item.StyleId] : null;
            if (style is null)
                continue;

            var count = item.Buff.GetStacks(activeStacks);
            var resolved = style.Resolve(count, nowMs);
            if (resolved is null)
                continue;

            var pos = origin + new Vector2(item.Position.X * grid.Spacing.X, item.Position.Y * grid.Spacing.Y);

            result.Add(new GridInstanceData
            {
                PosDims = new Vector4(pos / screen, grid.Spacing.X / screen.X, grid.Spacing.Y / screen.Y),
                Uv = item.Buff.Uv,
                ShowNumber = item.Buff.ShowNumber(count),
                Stacks = count,
                Tint = resolved.Tint,
                BorderColor = resolved.BorderColor,
                BorderThickness = resolved.BorderThickness,
                GlowColor = resolved.GlowColor,
                GlowSize = new Vector2(resolved.GlowSizeX, resolved.GlowSizeY),
            });
        }

        return result;
    }
}
