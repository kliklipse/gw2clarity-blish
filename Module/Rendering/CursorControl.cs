using System;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GW2ClarityBlish.Module.Rendering;

/// <summary>
/// Curseur custom qui suit la souris et se dessine par-dessus les Grids (ZIndex maximal).
/// </summary>
/// <remarks>
/// Ne masque PAS le curseur logiciel (<c>Input.Mouse.CursorIsVisible</c>) : Blish HUD relit cet
/// etat systeme chaque frame dans son propre <c>MouseHandler.Update()</c> pour decider si les
/// clics doivent etre routes vers son UI (<c>if (!CursorIsVisible) return false;</c>, cf. code
/// source officiel). Le masquer en continu depuis ce control cassait le clic sur TOUTE
/// l'interface Blish HUD (la notre et la sienne) - constate en test reel le 2026-09-01, symptome :
/// clics ignores, necessitant plusieurs tentatives ou un alt-tab pour se debloquer. Desactive par
/// defaut (<see cref="CursorEnabled"/> = false) tant qu'aucun mode d'edition ne l'utilise
/// reellement (l'original GW2Clarity ne l'affichait que pendant l'edition d'une Grid attachee a
/// la souris, pas en permanence).
/// </remarks>
public class CursorControl : Control
{
    private readonly Texture2D _cursorTexture;

    /// <summary>
    /// Active/desactive l'affichage du curseur custom (superpose au curseur systeme, qui reste
    /// toujours visible - voir remarque de la classe).
    /// </summary>
    public bool CursorEnabled { get; set; }

    public CursorControl(Texture2D cursorTexture)
    {
        _cursorTexture = cursorTexture ?? throw new ArgumentNullException(nameof(cursorTexture));

        // Toujours au-dessus de tout le reste (Grids compris).
        ZIndex = int.MaxValue;
        ClipsBounds = false;
    }

    /// <summary>
    /// Sans cet override, ce control (plein ecran + ZIndex maximal, donc verifie en premier
    /// par le hit-testing de Blish HUD) capturait par defaut tout l'input souris de l'ecran
    /// entier (CaptureType.Mouse, comportement par defaut de tout Control) et bloquait
    /// litteralement tous les clics vers le reste de l'overlay - fenetres non deplacables,
    /// boutons inertes. Constate en test reel le 2026-09-01, cf. meme fix sur GridRendererControl.
    /// </summary>
    protected override CaptureType CapturesInput() => CaptureType.None;

    protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds)
    {
        var screen = Graphics.SpriteScreen;
        Location = Point.Zero;
        Size = screen.Size;

        if (!CursorEnabled)
            return;

        var mouse = Input.Mouse.Position;
        var dest = new Rectangle(mouse.X, mouse.Y, _cursorTexture.Width, _cursorTexture.Height);

        spriteBatch.Draw(_cursorTexture, dest, Color.White);
    }
}
