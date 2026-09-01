using System.Collections.Generic;
using System.Linq;
using Blish_HUD.Controls;
using Blish_HUD.Graphics.UI;
using GW2ClarityBlish.Models;
using Microsoft.Xna.Framework;

namespace GW2ClarityBlish.Module.UI.Views;

/// <summary>
/// Panneau de configuration des Layouts (Sets) : liste, toggle CombatOnly, selection des
/// Grid incluses et selection du Layout actif (un seul a la fois - comportement "radio").
/// </summary>
public sealed class LayoutsView : View
{
    private readonly ConfigContext _context;
    private FlowPanel _listContainer = null!;

    /// <summary>
    /// Checkbox "Actif" de chaque ligne, pour pouvoir decocher les autres quand l'une
    /// d'elles est cochee (Blish HUD n'a pas de RadioButton natif dans les controles
    /// disponibles - voir Blish_HUD.Controls, seulement ICheckable/Checkbox).
    /// </summary>
    private readonly List<(int Index, Checkbox Checkbox)> _activeCheckboxes = new();

    public LayoutsView(ConfigContext context)
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

        var addButton = new StandardButton { Text = "+ Nouveau Layout", Parent = root };
        addButton.Click += (_, _) =>
        {
            _context.Layouts.Add(new Layout());
            _context.SaveLayouts();
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

        _activeCheckboxes.Clear();

        for (var i = 0; i < _context.Layouts.Count; i++)
            BuildLayoutRow(i);
    }

    private void BuildLayoutRow(int index)
    {
        var layout = _context.Layouts[index];

        var panel = new Panel
        {
            Title = string.IsNullOrWhiteSpace(layout.Name) ? $"Layout #{index}" : layout.Name,
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

        UiHelpers.AddLabeledTextBox(body, "Nom", layout.Name, v =>
        {
            layout.Name = v;
            panel.Title = string.IsNullOrWhiteSpace(v) ? $"Layout #{index}" : v;
            _context.SaveLayouts();
        });

        UiHelpers.AddLabeledCheckbox(body, "Combat only", layout.CombatOnly, v =>
        {
            layout.CombatOnly = v;
            _context.SaveLayouts();
        });

        var activeCheckbox = UiHelpers.AddLabeledCheckbox(body, "Actif", _context.ActiveLayoutIndex == index, _ => { });
        _activeCheckboxes.Add((index, activeCheckbox));

        activeCheckbox.CheckedChanged += (_, _) =>
        {
            if (activeCheckbox.Checked)
            {
                _context.SetActiveLayout(index);

                foreach (var (otherIndex, otherCheckbox) in _activeCheckboxes)
                {
                    if (otherIndex != index)
                        otherCheckbox.Checked = false;
                }
            }
            else if (_context.ActiveLayoutIndex == index)
            {
                _context.SetActiveLayout(null);
            }
        };

        UiHelpers.AddLabel(body, "Grids incluses");

        for (var gi = 0; gi < _context.Grids.Count; gi++)
        {
            var grid = _context.Grids[gi];
            var gridLabel = string.IsNullOrWhiteSpace(grid.Name) ? $"Grid #{gi}" : grid.Name;
            var gridIndex = gi;

            UiHelpers.AddLabeledCheckbox(body, gridLabel, layout.GridIds.Contains(gridIndex), included =>
            {
                if (included)
                    layout.GridIds.Add(gridIndex);
                else
                    layout.GridIds.Remove(gridIndex);

                _context.SaveLayouts();
            });
        }

        if (_context.Grids.Count == 0)
            UiHelpers.AddLabel(body, "(aucune Grid disponible)");

        UiHelpers.AddConfirmButton(body, "Supprimer ce Layout", () =>
        {
            _context.DeleteLayout(index);
            RebuildList();
        });
    }
}
