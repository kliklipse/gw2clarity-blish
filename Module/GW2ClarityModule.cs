using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Blish_HUD.Modules;
using Blish_HUD.Settings;
using GW2ClarityBlish.Models;
using GW2ClarityBlish.Module.Rendering;
using GW2ClarityBlish.Module.Services;
using GW2ClarityBlish.Module.UI;
using GW2ClarityBlish.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace GW2ClarityBlish.Module;

/// <summary>
/// Point d'entree du module Blish HUD : cablage du cycle de vie
/// (<c>Blish_HUD.Modules.Module</c>), du raccourci clavier, du bridge ArcDPS reel et de
/// l'affichage des Grids/curseur par-dessus le jeu.
/// </summary>
/// <remarks>
/// Nomme <c>GW2ClarityModule</c> plutot que <c>Module</c> pour eviter tout conflit avec le
/// namespace <c>Blish_HUD.Modules.Module</c> dont cette classe herite (le <c>using</c> de ce
/// namespace + une classe du meme nom que le type de base auraient ete ambigus).
/// </remarks>
[Export(typeof(Blish_HUD.Modules.Module))]
public sealed class GW2ClarityModule : Blish_HUD.Modules.Module
{
    private const string StorageDirectoryName = "gw2clarity";

    // Reprend l'icone deja utilisee et verifiee pour l'onglet "Grids" de ConfigWindow (asset
    // 156909, cf. Module/UI/ConfigWindow.cs) plutot que de deviner un nouvel id d'asset pour le
    // CornerIcon - meme logique de fond que le reste de la fenetre de configuration.
    private const int CornerIconAssetId = 156909;

    private readonly Logger _logger = Logger.GetLogger<GW2ClarityModule>();

    private SettingEntry<KeyBinding> _keyBindOpenPanel = null!;
    private SettingEntry<int> _activeLayoutIndexSetting = null!;

    private GridStore _gridStore = null!;
    private LayoutStore _layoutStore = null!;
    private StyleStore _styleStore = null!;

    private ArcdpsBridgeBuffEventSource _buffEventSource = null!;
    private BuffStateService _buffStateService = null!;
    private ArcdpsBuffTracker _arcdpsBuffTracker = null!;

    private ConfigWindow _configWindow = null!;
    private CornerIcon? _cornerIcon;
    private GridRendererControl? _gridRenderer;
    private CursorControl? _cursorControl;
    private BuffIconProvider _buffIconProvider = null!;

    private bool _isInCombat;
    private bool _isInCompetitiveMode;

    [ImportingConstructor]
    public GW2ClarityModule([Import("ModuleParameters")] ModuleParameters moduleParameters)
        : base(moduleParameters)
    {
    }

    protected override void DefineSettings(SettingCollection settings)
    {
        _keyBindOpenPanel = settings.DefineSetting(
            "OpenPanel",
            new KeyBinding(ModifierKeys.Alt | ModifierKeys.Shift, Keys.P),
            () => "Ouvrir GW2 Clarity",
            () => "Affiche ou masque la fenetre de configuration des grilles de buffs.");

        // -1 = aucun Layout actif. Cf. remarque de ConfigContext.ActiveLayoutIndex : c'est
        // exactement le point d'extension prevu pour la persistance inter-session, ici branche
        // via un SettingEntry<int> (mecanisme standard Blish HUD pour l'etat leger persistant).
        _activeLayoutIndexSetting = settings.DefineSetting(
            "ActiveLayoutIndex",
            -1,
            () => "Layout actif",
            () => "Usage interne : index du Layout actif entre deux sessions.");
    }

    protected override void Initialize()
    {
        var storageDirectory = ModuleParameters.DirectoriesManager.GetFullDirectoryPath(StorageDirectoryName);

        _gridStore = new GridStore(storageDirectory);
        _layoutStore = new LayoutStore(storageDirectory);
        _styleStore = new StyleStore(storageDirectory);

        // ArcdpsBridgeBuffEventSource ne fait que relayer un evenement : le vrai abonnement au
        // flux ArcDPS V2 (GameService.ArcDpsV2) vit dans ArcdpsBuffTracker, cote host net472 -
        // voir le commentaire de ces deux classes pour le detail de cette separation
        // (le projet racine Services/ est netstandard2.0 et ne reference pas BlishHUD).
        _buffEventSource = new ArcdpsBridgeBuffEventSource();
        _buffStateService = new BuffStateService(_buffEventSource);
        _arcdpsBuffTracker = new ArcdpsBuffTracker(_buffEventSource);

        var initialActiveLayoutIndex = _activeLayoutIndexSetting.Value >= 0
            ? _activeLayoutIndexSetting.Value
            : (int?)null;

        _configWindow = new ConfigWindow(_gridStore, _layoutStore, _styleStore, initialActiveLayoutIndex);
        _configWindow.Context.ActiveLayoutChanged += OnActiveLayoutChanged;

        _buffIconProvider = new BuffIconProvider(ModuleParameters.ContentsManager);
    }

    protected override Task LoadAsync()
    {
        _keyBindOpenPanel.Value.Activated += OnOpenPanelActivated;
        _keyBindOpenPanel.Value.Enabled = true;

        _arcdpsBuffTracker.Start();

        if (!GameService.ArcDpsV2.Running)
        {
            // Jamais de "ok" par defaut (meme principe que les sondes de sante forge-app) :
            // aucun stack ne sera simule tant qu'ArcDPS (+ le pont natif arcdps_bhud.dll) n'est
            // pas reellement connecte - les Grids pilotees par buff resteront simplement vides,
            // sans faire planter le module ni afficher un faux etat "actif".
            _logger.Warn(
                "ArcDPS n'est pas actif (GameService.ArcDpsV2.Running == false) : les grilles " +
                "pilotees par buff resteront vides jusqu'a ce qu'ArcDPS et son pont natif " +
                "arcdps_bhud.dll soient detectes. Aucune valeur n'est simulee par defaut.");
        }

        using (var graphicsContext = GameService.Graphics.LendGraphicsDeviceContext())
        {
            var cursorTexture = CreateCursorTexture(graphicsContext.GraphicsDevice);
            _cursorControl = new CursorControl(cursorTexture)
            {
                Parent = GameService.Graphics.SpriteScreen,
            };
        }

        _gridRenderer = new GridRendererControl(GetVisibleGrids, GetStyles, _buffStateService.GetStacks, _buffIconProvider.GetIcon)
        {
            Parent = GameService.Graphics.SpriteScreen,
            BorderGlowEffect = TryLoadBorderGlowEffect(),
        };

        _cornerIcon = new CornerIcon
        {
            Icon = AsyncTexture2D.FromAssetId(CornerIconAssetId),
            BasicTooltipText = "GW2 Clarity",
            Priority = int.MaxValue,
            Parent = GameService.Graphics.SpriteScreen,
        };
        _cornerIcon.Click += OnCornerIconClick;

        return Task.CompletedTask;
    }

    protected override void Update(GameTime gameTime)
    {
        // Rafraichi une fois par frame plutot qu'a chaque appel de GetVisibleGrids (invoque
        // depuis GridRendererControl.Paint) : evite de re-interroger GameService.Gw2Mumble
        // plusieurs fois par frame pour la meme valeur.
        _isInCombat = GameService.Gw2Mumble.PlayerCharacter.IsInCombat;
        _isInCompetitiveMode = GameService.Gw2Mumble.CurrentMap.IsCompetitiveMode;
    }

    protected override void Unload()
    {
        if (_cornerIcon is not null)
        {
            _cornerIcon.Click -= OnCornerIconClick;
            _cornerIcon.Dispose();
            _cornerIcon = null;
        }

        _gridRenderer?.Dispose();
        _gridRenderer = null;

        _cursorControl?.Dispose();
        _cursorControl = null;

        _configWindow.Context.ActiveLayoutChanged -= OnActiveLayoutChanged;
        _configWindow.Dispose();

        if (_keyBindOpenPanel?.Value is not null)
            _keyBindOpenPanel.Value.Activated -= OnOpenPanelActivated;

        _arcdpsBuffTracker.Dispose();
    }

    private void OnOpenPanelActivated(object sender, EventArgs e) => _configWindow.ToggleWindow();

    private void OnCornerIconClick(object sender, Blish_HUD.Input.MouseEventArgs e) => _configWindow.ToggleWindow();

    private void OnActiveLayoutChanged(int? index) => _activeLayoutIndexSetting.Value = index ?? -1;

    private IReadOnlyList<Style> GetStyles() => _configWindow.Context.Styles;

    /// <summary>
    /// Determine les Grids a afficher pour le Layout actuellement actif dans
    /// <see cref="ConfigContext"/>, filtrees par <see cref="Layout.ShouldRender"/> (combat /
    /// mode competitif - rafraichis chaque frame par <see cref="Update"/>). Aucun Layout actif
    /// = aucune Grid affichee : pas d'affichage "tout par defaut" tant que l'utilisateur n'a pas
    /// explicitement choisi un Layout dans l'onglet Layouts.
    /// </summary>
    private IReadOnlyList<Grid> GetVisibleGrids()
    {
        var context = _configWindow.Context;

        if (context.ActiveLayoutIndex is not int activeIndex || activeIndex < 0 || activeIndex >= context.Layouts.Count)
            return Array.Empty<Grid>();

        var layout = context.Layouts[activeIndex];
        if (!layout.ShouldRender(_isInCombat, _isInCompetitiveMode))
            return Array.Empty<Grid>();

        var visible = new List<Grid>(layout.GridIds.Count);
        foreach (var gridIndex in layout.GridIds)
        {
            if (gridIndex >= 0 && gridIndex < context.Grids.Count)
                visible.Add(context.Grids[gridIndex]);
        }

        return visible;
    }

    /// <summary>
    /// Charge le shader optionnel de bordure/glow (voir Module/ref/rendering/GridEffect.mgfx
    /// et le commentaire de tete de Module/Rendering/GridEffect.fx pour le detail du wiring).
    /// Un echec ici (fichier .mgfx absent ou corrompu) ne doit jamais empecher le module de se
    /// charger : <see cref="GridRendererControl"/> retombe alors sur son rendu par composition
    /// de sprites (voir GridRendererControl.BorderGlowEffect) plutot que de planter.
    /// </summary>
    private Effect? TryLoadBorderGlowEffect()
    {
        try
        {
            return ModuleParameters.ContentsManager.GetEffect("rendering/GridEffect.mgfx");
        }
        catch (Exception ex)
        {
            _logger.Warn(ex, "Shader optionnel rendering/GridEffect.mgfx introuvable ou invalide : " +
                              "rendu par composition de sprites (sans shader) utilise a la place.");
            return null;
        }
    }

    /// <summary>
    /// Cree une texture de curseur minimale (petit carre plein) pour <see cref="CursorControl"/>,
    /// qui exige un <see cref="Texture2D"/> non-null des la construction (voir
    /// Module/Rendering/CursorControl.cs, Tache 10). Pas d'asset d'icone de curseur specifique
    /// verifie dans le SDK 1.3.0 pour ce module - TODO : remplacer par un vrai sprite si un
    /// asset dedie est ajoute plus tard, ceci reste fonctionnellement correct (position/visibilite
    /// verifiables) en attendant.
    /// </summary>
    private static Texture2D CreateCursorTexture(GraphicsDevice device)
    {
        const int size = 10;
        var texture = new Texture2D(device, size, size);

        var pixels = new Color[size * size];
        for (var i = 0; i < pixels.Length; i++)
            pixels[i] = Color.White;

        texture.SetData(pixels);
        return texture;
    }
}
