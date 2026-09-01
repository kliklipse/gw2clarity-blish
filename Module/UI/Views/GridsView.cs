using System.Collections.Generic;
using System.Linq;
using Blish_HUD.Controls;
using Blish_HUD.Graphics.UI;
using GW2ClarityBlish.Models;
using Microsoft.Xna.Framework;

namespace GW2ClarityBlish.Module.UI.Views;

/// <summary>
/// Panneau de configuration des Grids : liste, edition basique (nom, spacing, offset,
/// attach, central weight) et gestion des GridItem (buff, StyleId, suppression).
/// </summary>
/// <remarks>
/// Vue "controles simples" (pas de Presenter dedie) : <see cref="View"/> fournit deja un
/// <c>NullPresenter</c> par defaut, ce qui est le pattern officiel pour une vue qui n'a pas
/// besoin de logique separee de sa presentation (voir Blish_HUD.Graphics.UI.View source).
/// </remarks>
public sealed class GridsView : View
{
    private readonly ConfigContext _context;
    private FlowPanel _listContainer = null!;

    public GridsView(ConfigContext context)
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

        var addButton = new StandardButton { Text = "+ Nouvelle Grid", Parent = root };
        addButton.Click += (_, _) =>
        {
            _context.Grids.Add(new Grid());
            _context.SaveGrids();
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

        for (var i = 0; i < _context.Grids.Count; i++)
            BuildGridRow(i);
    }

    private void BuildGridRow(int index)
    {
        var grid = _context.Grids[index];

        var panel = new Panel
        {
            Title = string.IsNullOrWhiteSpace(grid.Name) ? $"Grid #{index}" : grid.Name,
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

        UiHelpers.AddLabeledTextBox(body, "Nom", grid.Name, v =>
        {
            grid.Name = v;
            panel.Title = string.IsNullOrWhiteSpace(v) ? $"Grid #{index}" : v;
            _context.SaveGrids();
        });

        UiHelpers.AddLabeledTextBox(body, "Espacement X", grid.Spacing.X.ToString(), v =>
        {
            if (UiHelpers.TryParseInt(v, out var n))
            {
                grid.Spacing = (n, grid.Spacing.Y);
                _context.SaveGrids();
            }
        });

        UiHelpers.AddLabeledTextBox(body, "Espacement Y", grid.Spacing.Y.ToString(), v =>
        {
            if (UiHelpers.TryParseInt(v, out var n))
            {
                grid.Spacing = (grid.Spacing.X, n);
                _context.SaveGrids();
            }
        });

        UiHelpers.AddLabeledTextBox(body, "Offset (x,y)", UiHelpers.FormatVector2(grid.Offset), v =>
        {
            grid.Offset = UiHelpers.ParseVector2(v, grid.Offset);
            _context.SaveGrids();
        }, textBoxWidth: 140);

        UiHelpers.AddLabeledTextBox(body, "Central weight", grid.CentralWeight.ToString("0.###"), v =>
        {
            if (UiHelpers.TryParseFloat(v, out var f))
            {
                grid.CentralWeight = f;
                _context.SaveGrids();
            }
        });

        UiHelpers.AddLabeledCheckbox(body, "Attached (suit la souris)", grid.Attached, v =>
        {
            grid.Attached = v;
            _context.SaveGrids();
        });

        UiHelpers.AddLabeledCheckbox(body, "Square", grid.Square, v =>
        {
            grid.Square = v;
            _context.SaveGrids();
        });

        UiHelpers.AddLabeledCheckbox(body, "Track mouse while held", grid.TrackMouseWhileHeld, v =>
        {
            grid.TrackMouseWhileHeld = v;
            _context.SaveGrids();
        });

        UiHelpers.AddLabel(body, "Items");

        var itemsContainer = new FlowPanel
        {
            FlowDirection = ControlFlowDirection.SingleTopToBottom,
            WidthSizingMode = SizingMode.Fill,
            HeightSizingMode = SizingMode.AutoSize,
            ControlPadding = new Vector2(2, 2),
            Parent = body,
        };

        var addItemButton = new StandardButton { Text = "+ Item", Parent = body };
        addItemButton.Click += (_, _) =>
        {
            grid.Items.Add(new GridItem());
            _context.SaveGrids();
            RebuildList();
        };

        for (var itemIndex = 0; itemIndex < grid.Items.Count; itemIndex++)
            BuildGridItemRow(itemsContainer, grid, itemIndex);

        UiHelpers.AddConfirmButton(body, "Supprimer cette Grid", () =>
        {
            _context.DeleteGrid(index);
            RebuildList();
        });
    }

    private void BuildGridItemRow(Container parent, Grid grid, int itemIndex)
    {
        var item = grid.Items[itemIndex];

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

        var posXBox = UiHelpers.AddLabeledTextBox(inner, "Position X", item.Position.X.ToString(), _ => { }, 60);
        var posYBox = UiHelpers.AddLabeledTextBox(inner, "Position Y", item.Position.Y.ToString(), _ => { }, 60);
        var buffIdBox = UiHelpers.AddLabeledTextBox(inner, "Buff Id", item.Buff.Id.ToString(), _ => { }, 90);
        var buffNameBox = UiHelpers.AddLabeledTextBox(inner, "Buff Name", item.Buff.Name, _ => { }, 140);
        var maxStacksBox = UiHelpers.AddLabeledTextBox(inner, "Max Stacks", item.Buff.MaxStacks == int.MaxValue ? "" : item.Buff.MaxStacks.ToString(), _ => { }, 70);
        var extraIdsBox = UiHelpers.AddLabeledTextBox(inner, "Extra Ids (,)", string.Join(",", item.Buff.ExtraIds), _ => { }, 140);

        // Position et Buff se reconstruisent atomiquement (tuple/record-like avec plusieurs
        // champs lies) : chaque champ commit relit l'etat courant de TOUS les champs de la
        // ligne plutot que d'ecrire une valeur partielle incoherente.
        void CommitPositionAndBuff()
        {
            if (UiHelpers.TryParseInt(posXBox.Text, out var px) && UiHelpers.TryParseInt(posYBox.Text, out var py))
                item.Position = (px, py);

            if (UiHelpers.TryParseUInt(buffIdBox.Text, out var buffId))
            {
                var maxStacks = UiHelpers.TryParseInt(maxStacksBox.Text, out var ms) ? ms : int.MaxValue;
                var extraIds = new HashSet<uint>(
                    extraIdsBox.Text
                        .Split(',')
                        .Select(s => s.Trim())
                        .Where(s => s.Length > 0)
                        .Select(s => UiHelpers.TryParseUInt(s, out var id) ? (uint?)id : null)
                        .Where(id => id.HasValue)
                        .Select(id => id!.Value));

                item.Buff = new Buff(buffId, buffNameBox.Text, maxStacks, extraIds);
            }

            _context.SaveGrids();
        }

        foreach (var box in new[] { posXBox, posYBox, buffIdBox, buffNameBox, maxStacksBox, extraIdsBox })
        {
            box.EnterPressed += (_, _) => CommitPositionAndBuff();
            box.InputFocusChanged += (_, e) =>
            {
                if (!e.Value)
                    CommitPositionAndBuff();
            };
        }

        var styleDropdown = new Dropdown { Parent = inner, Width = 220 };
        for (var i = 0; i < _context.Styles.Count; i++)
            styleDropdown.Items.Add($"{i}: {_context.Styles[i].Name}");

        if (item.StyleId < (uint)_context.Styles.Count)
            styleDropdown.SelectedItem = styleDropdown.Items[(int)item.StyleId];

        styleDropdown.ValueChanged += (_, e) =>
        {
            var idx = e.CurrentValue.Split(':')[0];
            if (UiHelpers.TryParseUInt(idx, out var styleId))
            {
                item.StyleId = styleId;
                _context.SaveGrids();
            }
        };

        UiHelpers.AddConfirmButton(inner, "Supprimer cet item", () =>
        {
            grid.Items.RemoveAt(itemIndex);
            _context.SaveGrids();
            RebuildList();
        });
    }
}
