using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using GW2ClarityBlish.Module.UI.Views;
using GW2ClarityBlish.Services;
using Microsoft.Xna.Framework;

namespace GW2ClarityBlish.Module.UI;

/// <summary>
/// Fenetre de configuration GW2Clarity : un TabbedWindow2 avec 3 onglets (Grids, Layouts,
/// Styles). Cree et possede son propre <see cref="UI.ConfigContext"/> a partir des stores
/// fournis - le wiring Module.cs (tache 11) n'a besoin de construire que les 3 Store et
/// d'appeler <c>ToggleWindow()</c>/<c>Show()</c> (herites de WindowBase2) depuis un
/// CornerIcon ou une commande clavier.
/// </summary>
/// <remarks>
/// Region de fond et rectangles repris tels quels d'un module Blish HUD reel en production
/// (blish-hud/Pathing, PathingModule.cs, SettingsWindow) plutot qu'inventes : l'asset
/// 156006 est le fond de fenetre standard "controls" utilise par de nombreux modules via
/// <see cref="AsyncTexture2D.FromAssetId(int)"/> (pas besoin d'empaqueter de texture dans le
/// module).
/// </remarks>
public sealed class ConfigWindow : TabbedWindow2
{
    public ConfigContext Context { get; }

    public ConfigWindow(GridStore gridStore, LayoutStore layoutStore, StyleStore styleStore, int? initialActiveLayoutIndex = null)
        : base(
            AsyncTexture2D.FromAssetId(156006),
            new Rectangle(35, 36, 900, 640),
            new Rectangle(95, 42, 783 + 38, 592))
    {
        Context = new ConfigContext(gridStore, layoutStore, styleStore, initialActiveLayoutIndex);

        Title = "GW2 Clarity";
        Parent = GameService.Graphics.SpriteScreen;
        Location = new Point(100, 100);
        SavesPosition = true;
        Id = "GW2ClarityBlish_ConfigWindow";

        Tabs.Add(new Tab(AsyncTexture2D.FromAssetId(156909), () => new GridsView(Context), "Grids"));
        Tabs.Add(new Tab(AsyncTexture2D.FromAssetId(156701), () => new LayoutsView(Context), "Layouts"));
        Tabs.Add(new Tab(AsyncTexture2D.FromAssetId(1654244), () => new StylesView(Context), "Styles"));
    }
}
