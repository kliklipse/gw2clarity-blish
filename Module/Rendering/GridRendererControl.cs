using System;
using System.Collections.Generic;
using Blish_HUD;
using Blish_HUD.Controls;
using GW2ClarityBlish.Models;
using GW2ClarityBlish.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.BitmapFonts;

namespace GW2ClarityBlish.Module.Rendering;

/// <summary>
/// Control Blish HUD qui dessine, chaque frame, toutes les Grid actives par-dessus le jeu.
/// La logique de calcul (position, tint, bordure, glow) vit entierement dans
/// <see cref="GridRenderer.BuildInstances"/> (projet racine, pure/testee) - cette classe ne
/// fait que convertir chaque <see cref="GridInstanceData"/> en appels SpriteBatch.
/// </summary>
/// <remarks>
/// Blish HUD gere lui-meme le SpriteBatch.Begin()/End() autour de Paint() (configurable via
/// <see cref="Control.SpriteBatchParameters"/>) : ce control ne doit donc jamais appeler
/// Begin()/End() lui-meme.
/// </remarks>
public class GridRendererControl : Control
{
    private readonly Func<IReadOnlyList<Grid>> _gridsProvider;
    private readonly Func<IReadOnlyList<Style>> _stylesProvider;
    private readonly Func<uint, int> _stackLookup;
    private readonly Func<uint, Texture2D?> _iconLookup;

    private Texture2D? _pixel;

    /// <summary>
    /// Shader optionnel (voir GridEffect.fx) pour la bordure/glow. Si null, la bordure et le
    /// glow sont dessines par simple composition de sprites (rectangles), ce qui reste
    /// visuellement correct meme sans shader compile.
    /// </summary>
    public Effect? BorderGlowEffect
    {
        get => SpriteBatchParameters?.Effect;
        set => SpriteBatchParameters = value is null ? null : new SpriteBatchParameters(effect: value);
    }

    public GridRendererControl(
        Func<IReadOnlyList<Grid>> gridsProvider,
        Func<IReadOnlyList<Style>> stylesProvider,
        Func<uint, int> stackLookup,
        Func<uint, Texture2D?> iconLookup)
    {
        _gridsProvider = gridsProvider ?? throw new ArgumentNullException(nameof(gridsProvider));
        _stylesProvider = stylesProvider ?? throw new ArgumentNullException(nameof(stylesProvider));
        _stackLookup = stackLookup ?? throw new ArgumentNullException(nameof(stackLookup));
        _iconLookup = iconLookup ?? throw new ArgumentNullException(nameof(iconLookup));

        // Overlay plein ecran : les Grid sont positionnees en coordonnees ecran absolues,
        // donc ce control ne doit pas etre clippe ni bloquer d'input.
        ClipsBounds = false;
    }

    /// <summary>
    /// Par defaut, un <see cref="Control"/> Blish HUD capture le clic de souris sur toute sa
    /// surface (<c>CaptureType.Mouse</c>) et bloque tout ce qui est derriere - y compris le jeu
    /// et les autres fenetres Blish HUD (voir doc XML du SDK). Ce control couvre l'ecran entier
    /// et n'a aucun element interactif : sans cet override, il rendait injouable tout le reste
    /// de l'overlay (fenetre de configuration non deplacable, boutons inertes) des qu'il etait
    /// ajoute a l'ecran - constate en test reel le 2026-09-01.
    /// </summary>
    protected override CaptureType CapturesInput() => CaptureType.None;

    protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds)
    {
        SyncToScreen();

        var screenSize = new System.Numerics.Vector2(bounds.Width, bounds.Height);
        if (screenSize.X <= 0f || screenSize.Y <= 0f)
            return;

        _pixel ??= CreatePixel(spriteBatch.GraphicsDevice);

        var mousePoint = Input.Mouse.Position;
        var mouse = new System.Numerics.Vector2(mousePoint.X, mousePoint.Y);
        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var grids = _gridsProvider();
        var styles = _stylesProvider();
        if (grids.Count == 0 || styles.Count == 0)
            return;

        foreach (var grid in grids)
        {
            var activeStacks = CollectStacks(grid);
            var instances = GridRenderer.BuildInstances(grid, styles, activeStacks, screenSize, mouse, nowMs);

            foreach (var instance in instances)
                DrawInstance(spriteBatch, instance, screenSize);
        }
    }

    /// <summary>
    /// Garde le control cale sur la resolution courante de GW2 (peut changer si le joueur
    /// redimensionne la fenetre ou change de resolution en cours de session).
    /// </summary>
    private void SyncToScreen()
    {
        var screen = Graphics.SpriteScreen;
        Location = Point.Zero;
        Size = screen.Size;
    }

    /// <summary>
    /// Reconstruit le dictionnaire (buffId -> stacks) attendu par
    /// <see cref="GridRenderer.BuildInstances"/>, a partir du lookup fourni par
    /// BuffStateService. On ne peuple que les buffs reellement references par cette Grid
    /// (Buff.GetStacks ne consulte que Id + ExtraIds, jamais GridItem.AdditionalBuffs).
    /// </summary>
    private Dictionary<uint, int> CollectStacks(Grid grid)
    {
        var stacks = new Dictionary<uint, int>();

        foreach (var item in grid.Items)
        {
            AddStack(stacks, item.Buff.Id);
            foreach (var extraId in item.Buff.ExtraIds)
                AddStack(stacks, extraId);
        }

        return stacks;
    }

    private void AddStack(Dictionary<uint, int> stacks, uint buffId)
    {
        if (stacks.ContainsKey(buffId))
            return;

        stacks[buffId] = _stackLookup(buffId);
    }

    private void DrawInstance(SpriteBatch spriteBatch, GridInstanceData instance, System.Numerics.Vector2 screenSize)
    {
        var x = instance.PosDims.X * screenSize.X;
        var y = instance.PosDims.Y * screenSize.Y;
        var w = instance.PosDims.Z * screenSize.X;
        var h = instance.PosDims.W * screenSize.Y;

        // GridItem.Position * Grid.Spacing donne le coin haut-gauche de la cellule : le
        // rendu suit la meme convention (pas de recentrage additionnel).
        var dest = new Rectangle((int)x, (int)y, (int)Math.Max(w, 1f), (int)Math.Max(h, 1f));

        if (instance.GlowColor.W > 0f && (instance.GlowSize.X > 0f || instance.GlowSize.Y > 0f))
            DrawGlow(spriteBatch, dest, instance.GlowColor, instance.GlowSize);

        DrawIcon(spriteBatch, instance, dest);

        if (instance.BorderThickness > 0f && instance.BorderColor.W > 0f)
            DrawBorder(spriteBatch, dest, instance.BorderThickness, ToColor(instance.BorderColor));

        DrawStackNumber(spriteBatch, instance, dest);
    }

    private void DrawIcon(SpriteBatch spriteBatch, GridInstanceData instance, Rectangle dest)
    {
        var tint = ToColor(instance.Tint);
        var icon = _iconLookup(instance.BuffId);

        if (icon is not null)
            spriteBatch.Draw(icon, dest, tint);
        else
            spriteBatch.Draw(_pixel, dest, tint); // pas d'icone connue pour ce buff : rectangle tinte, comme avant
    }

    private void DrawGlow(SpriteBatch spriteBatch, Rectangle dest, System.Numerics.Vector4 glowColor, System.Numerics.Vector2 glowSize)
    {
        var glowRect = dest;
        glowRect.Inflate((int)Math.Round(glowSize.X), (int)Math.Round(glowSize.Y));
        spriteBatch.Draw(_pixel, glowRect, ToColor(glowColor));
    }

    private void DrawBorder(SpriteBatch spriteBatch, Rectangle dest, float thickness, Color color)
    {
        var t = Math.Max(1, (int)Math.Round(thickness));

        spriteBatch.Draw(_pixel, new Rectangle(dest.X, dest.Y, dest.Width, t), color); // haut
        spriteBatch.Draw(_pixel, new Rectangle(dest.X, dest.Bottom - t, dest.Width, t), color); // bas
        spriteBatch.Draw(_pixel, new Rectangle(dest.X, dest.Y, t, dest.Height), color); // gauche
        spriteBatch.Draw(_pixel, new Rectangle(dest.Right - t, dest.Y, t, dest.Height), color); // droite
    }

    private void DrawStackNumber(SpriteBatch spriteBatch, GridInstanceData instance, Rectangle dest)
    {
        if (!instance.ShowNumber)
            return;

        var font = Content.DefaultFont14;
        if (font is null)
            return;

        var text = instance.Stacks.ToString();
        var size = font.MeasureString(text);
        var position = new Vector2(
            dest.Center.X - size.Width / 2f,
            dest.Center.Y - size.Height / 2f);

        spriteBatch.DrawString(font, text, position, Color.White, null);
    }

    private static Texture2D CreatePixel(GraphicsDevice device)
    {
        var pixel = new Texture2D(device, 1, 1);
        pixel.SetData(new[] { Color.White });
        return pixel;
    }

    private static Color ToColor(System.Numerics.Vector4 v) => new(v.X, v.Y, v.Z, v.W);
}
