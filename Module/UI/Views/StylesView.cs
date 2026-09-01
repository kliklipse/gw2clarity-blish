using System.Linq;
using Blish_HUD.Controls;
using Blish_HUD.Graphics.UI;
using GW2ClarityBlish.Models;
using Microsoft.Xna.Framework;

namespace GW2ClarityBlish.Module.UI.Views;

/// <summary>
/// Panneau de configuration des Styles : liste, edition des Threshold (tint/bordure/glow/
/// pulse via Appearance). Pas de rendu live (voir remarque sur le cache) : un apercu
/// textuel des valeurs (les champs eux-memes) suffit a verifier ce qui a ete tape.
/// </summary>
/// <remarks>
/// <see cref="Style"/> (projet racine, non modifiable ici) memorise un cache prive
/// d'apparences resolues (<c>_appearanceCache</c>) qui n'est reconstruit que lorsque
/// <c>_cacheBuilt</c> vaut encore false. Une fois qu'un rendu a appele
/// <c>Style.Resolve()</c> au moins une fois, muter <c>Style.Thresholds</c> en place n'aurait
/// plus aucun effet visible : le cache resterait perime pour toujours (pas d'API publique
/// pour l'invalider). Pour rester correct sans toucher au modele racine, ce panneau ne mute
/// jamais un Style existant : chaque commit remplace l'entree dans
/// <see cref="ConfigContext.Styles"/> par une instance neuve via le constructeur public
/// <c>Style(name, thresholds)</c>, qui appelle <c>BuildCache()</c> et laisse
/// <c>_cacheBuilt</c> a false (donc rebuild lazy garanti au prochain <c>Resolve()</c>).
/// </remarks>
public sealed class StylesView : View
{
    private readonly ConfigContext _context;
    private FlowPanel _listContainer = null!;

    public StylesView(ConfigContext context)
    {
        _context = context;
    }

    protected override void Build(Container buildPanel)
    {
        var root = new FlowPanel
        {
            FlowDirection = ControlFlowDirection.SingleTopToBottom,
            WidthSizingMode = SizingMode.Fill,
            HeightSizingMode = SizingMode.Fill,
            CanScroll = true,
            ControlPadding = new Vector2(4, 4),
            OuterControlPadding = new Vector2(8, 8),
            Parent = buildPanel,
        };

        var addButton = new StandardButton { Text = "+ Nouveau Style", Parent = root };
        addButton.Click += (_, _) =>
        {
            _context.Styles.Add(new Style { Name = "New Style" });
            _context.SaveStyles();
            RebuildList();
        };

        _listContainer = new FlowPanel
        {
            FlowDirection = ControlFlowDirection.SingleTopToBottom,
            WidthSizingMode = SizingMode.Fill,
            HeightSizingMode = SizingMode.AutoSize,
            ControlPadding = new Vector2(4, 4),
            Parent = root,
        };

        RebuildList();
    }

    private void RebuildList()
    {
        foreach (var child in _listContainer.Children.ToList())
            child.Dispose();

        for (var i = 0; i < _context.Styles.Count; i++)
            BuildStyleRow(i);
    }

    /// <summary>
    /// Remplace l'entree <paramref name="styleIndex"/> par une instance Style neuve
    /// construite a partir de <paramref name="style"/> (voir remarque de classe sur le
    /// cache prive), puis persiste.
    /// </summary>
    private void PersistStyle(Style style, int styleIndex)
    {
        _context.Styles[styleIndex] = new Style(style.Name, style.Thresholds);
        _context.SaveStyles();
    }

    private void BuildStyleRow(int index)
    {
        var style = _context.Styles[index];

        var panel = new Panel
        {
            Title = string.IsNullOrWhiteSpace(style.Name) ? $"Style #{index}" : style.Name,
            CanCollapse = true,
            Collapsed = true,
            ShowBorder = true,
            WidthSizingMode = SizingMode.Fill,
            HeightSizingMode = SizingMode.AutoSize,
            Parent = _listContainer,
        };

        var body = new FlowPanel
        {
            FlowDirection = ControlFlowDirection.SingleTopToBottom,
            WidthSizingMode = SizingMode.Fill,
            HeightSizingMode = SizingMode.AutoSize,
            ControlPadding = new Vector2(2, 2),
            Parent = panel,
        };

        UiHelpers.AddLabeledTextBox(body, "Nom", style.Name, v =>
        {
            style.Name = v;
            panel.Title = string.IsNullOrWhiteSpace(v) ? $"Style #{index}" : v;
            PersistStyle(style, index);
        });

        UiHelpers.AddLabel(body, "Thresholds (par nombre de stacks)");

        var thresholdsContainer = new FlowPanel
        {
            FlowDirection = ControlFlowDirection.SingleTopToBottom,
            WidthSizingMode = SizingMode.Fill,
            HeightSizingMode = SizingMode.AutoSize,
            ControlPadding = new Vector2(2, 2),
            Parent = body,
        };

        var addThresholdButton = new StandardButton { Text = "+ Threshold", Parent = body };
        addThresholdButton.Click += (_, _) =>
        {
            style.Thresholds.Add(new Threshold(0, 0, new Appearance()));
            PersistStyle(style, index);
            RebuildList();
        };

        for (var ti = 0; ti < style.Thresholds.Count; ti++)
            BuildThresholdRow(thresholdsContainer, style, index, ti);

        UiHelpers.AddConfirmButton(body, "Supprimer ce Style", () =>
        {
            _context.DeleteStyle(index);
            RebuildList();
        });
    }

    private void BuildThresholdRow(Container parent, Style style, int styleIndex, int thresholdIndex)
    {
        var threshold = style.Thresholds[thresholdIndex];
        var appearance = threshold.Appearance;

        var row = new Panel
        {
            ShowBorder = true,
            WidthSizingMode = SizingMode.Fill,
            HeightSizingMode = SizingMode.AutoSize,
            Parent = parent,
        };

        var inner = new FlowPanel
        {
            FlowDirection = ControlFlowDirection.SingleTopToBottom,
            WidthSizingMode = SizingMode.Fill,
            HeightSizingMode = SizingMode.AutoSize,
            ControlPadding = new Vector2(2, 2),
            Parent = row,
        };

        var minBox = UiHelpers.AddLabeledTextBox(inner, "Min stacks", threshold.Min.ToString(), _ => { }, 60);
        var maxBox = UiHelpers.AddLabeledTextBox(inner, "Max stacks", threshold.Max.ToString(), _ => { }, 60);
        var tintBox = UiHelpers.AddLabeledTextBox(inner, "Tint (r,g,b,a)", UiHelpers.FormatVector4(appearance.Tint), _ => { }, 170);
        var borderBox = UiHelpers.AddLabeledTextBox(inner, "Bordure (r,g,b,a)", UiHelpers.FormatVector4(appearance.Border), _ => { }, 170);
        var glowBox = UiHelpers.AddLabeledTextBox(inner, "Glow (r,g,b,a)", UiHelpers.FormatVector4(appearance.Glow), _ => { }, 170);
        var borderThicknessBox = UiHelpers.AddLabeledTextBox(inner, "Epaisseur bordure", appearance.BorderThickness.ToString("0.###"), _ => { }, 70);
        var glowSizeBox = UiHelpers.AddLabeledTextBox(inner, "Taille glow", appearance.GlowSize.ToString("0.###"), _ => { }, 70);
        var glowPulseBox = UiHelpers.AddLabeledTextBox(inner, "Glow pulse (amplitude,freq Hz)", UiHelpers.FormatVector2(appearance.GlowPulse), _ => { }, 140);

        void Commit()
        {
            var min = UiHelpers.TryParseUInt(minBox.Text, out var mn) ? mn : threshold.Min;
            var max = UiHelpers.TryParseUInt(maxBox.Text, out var mx) ? mx : threshold.Max;

            var newAppearance = new Appearance
            {
                Tint = UiHelpers.ParseVector4(tintBox.Text, appearance.Tint),
                Border = UiHelpers.ParseVector4(borderBox.Text, appearance.Border),
                Glow = UiHelpers.ParseVector4(glowBox.Text, appearance.Glow),
                BorderThickness = UiHelpers.TryParseFloat(borderThicknessBox.Text, out var bt) ? bt : appearance.BorderThickness,
                GlowSize = UiHelpers.TryParseFloat(glowSizeBox.Text, out var gs) ? gs : appearance.GlowSize,
                GlowPulse = UiHelpers.ParseVector2(glowPulseBox.Text, appearance.GlowPulse),
            };

            style.Thresholds[thresholdIndex] = new Threshold(min, max, newAppearance);
            PersistStyle(style, styleIndex);
        }

        foreach (var box in new[] { minBox, maxBox, tintBox, borderBox, glowBox, borderThicknessBox, glowSizeBox, glowPulseBox })
        {
            box.EnterPressed += (_, _) => Commit();
            box.InputFocusChanged += (_, e) =>
            {
                if (!e.Value)
                    Commit();
            };
        }

        UiHelpers.AddConfirmButton(inner, "Supprimer ce seuil", () =>
        {
            style.Thresholds.RemoveAt(thresholdIndex);
            PersistStyle(style, styleIndex);
            RebuildList();
        });
    }
}
