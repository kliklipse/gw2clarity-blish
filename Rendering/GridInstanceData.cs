using System.Numerics;

namespace GW2ClarityBlish.Rendering;

public struct GridInstanceData
{
    public Vector4 PosDims;
    public Vector2 Uv;
    public Vector2 NumberUv;
    public bool ShowNumber;
    public int Stacks;
    public Vector4 Tint;
    public Vector4 BorderColor;
    public float BorderThickness;
    public Vector4 GlowColor;
    public Vector2 GlowSize;
}
