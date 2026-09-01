namespace GW2ClarityBlish.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public class ResolvedAppearance
{
    public Vector4 Tint { get; init; }
    public Vector4 BorderColor { get; init; }
    public Vector4 GlowColor { get; init; }
    public float BorderThickness { get; init; }
    public float GlowSizeX { get; init; }
    public float GlowSizeY { get; init; }
}

public class Style
{
    private const int CacheSize = 100;
    public string Name { get; }
    public IReadOnlyList<Threshold> Thresholds { get; }

    private readonly Appearance?[] _appearanceCache = new Appearance?[CacheSize];
    private Appearance? _appearanceAbove;

    public Style(string name, IEnumerable<Threshold> thresholds)
    {
        Name = name;
        Thresholds = thresholds.ToList();
        BuildCache();
    }

    private void BuildCache()
    {
        Array.Clear(_appearanceCache);

        foreach (var t in Thresholds.AsEnumerable().Reverse())
        {
            var upper = (int)Math.Min(t.Max + 1, CacheSize);
            for (var i = (int)t.Min; i < upper; i++)
                _appearanceCache[i] = t.Appearance;
        }

        _appearanceAbove = Thresholds.FirstOrDefault(t => t.Max >= 100)?.Appearance;
    }

    public ResolvedAppearance? Resolve(int count, long nowMs)
    {
        if (count < 0)
            return null;

        var app = count < CacheSize ? _appearanceCache[count] : _appearanceAbove;
        if (app is null)
            return null;

        float glowX, glowY;
        if (app.GlowPulse.X > 0f)
        {
            var x = MathF.Sin((float)nowMs / 1000f * 2f * MathF.PI * app.GlowPulse.Y) * 0.5f + 0.5f;
            glowX = ((1f - app.GlowPulse.X) + app.GlowPulse.X * x) * app.GlowSize;
            glowY = app.GlowSize;
        }
        else
        {
            glowX = glowY = app.GlowSize;
        }

        return new ResolvedAppearance
        {
            Tint = app.Tint,
            BorderColor = app.Border,
            BorderThickness = app.BorderThickness,
            GlowColor = app.GlowSize > 0f ? app.Glow : Vector4.Zero,
            GlowSizeX = glowX,
            GlowSizeY = glowY,
        };
    }
}
