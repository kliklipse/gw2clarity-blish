namespace GW2ClarityBlish.Models;

using System.Numerics;

public class Appearance
{
    public Vector4 Tint { get; init; } = Vector4.One;
    public Vector4 Border { get; init; } = Vector4.Zero;
    public Vector4 Glow { get; init; } = Vector4.Zero;
    public float BorderThickness { get; init; }
    public float GlowSize { get; init; }
    public Vector2 GlowPulse { get; init; } = Vector2.Zero; // x = amplitude [0,1], y = frequence Hz
}
