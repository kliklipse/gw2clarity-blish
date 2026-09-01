using System;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GW2ClarityBlish.Module.Rendering;

/// <summary>
/// Curseur custom qui suit la souris et se dessine par-dessus les Grids (ZIndex maximal).
/// Masque le curseur logiciel de Blish HUD tant qu'il est actif pour eviter le double-affichage.
/// </summary>
public class CursorControl : Control
{
    private readonly Texture2D _cursorTexture;
    private bool _cursorEnabled = true;

    /// <summary>
    /// Active/desactive l'affichage du curseur custom. A la desactivation, le curseur
    /// logiciel de Blish HUD est restaure pour ne pas laisser l'utilisateur sans curseur.
    /// </summary>
    public bool CursorEnabled
    {
        get => _cursorEnabled;
        set
        {
            _cursorEnabled = value;
            if (!value)
                Input.Mouse.CursorIsVisible = true;
        }
    }

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

        if (!_cursorEnabled)
            return;

        // Masque le curseur logiciel de Blish HUD pendant qu'on dessine le notre par-dessus.
        Input.Mouse.CursorIsVisible = false;

        var mouse = Input.Mouse.Position;
        var dest = new Rectangle(mouse.X, mouse.Y, _cursorTexture.Width, _cursorTexture.Height);

        spriteBatch.Draw(_cursorTexture, dest, Color.White);
    }
}
