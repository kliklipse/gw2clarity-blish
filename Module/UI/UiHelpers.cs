using System;
using System.Globalization;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;

namespace GW2ClarityBlish.Module.UI;

/// <summary>
/// Petits constructeurs de controles Blish HUD partages par les 3 panneaux de configuration
/// (Grids/Layouts/Styles). Reste au niveau Controls simples (pas de pattern IView/Presenter
/// imbrique) : ces panneaux n'ont pas besoin d'abstraction supplementaire.
/// </summary>
internal static class UiHelpers
{
    /// <summary>Ligne horizontale generique (label + controle(s)), s'enroule si trop etroite.</summary>
    public static FlowPanel AddRow(Container parent)
        => new()
        {
            FlowDirection = ControlFlowDirection.LeftToRight,
            WidthSizingMode = SizingMode.Fill,
            HeightSizingMode = SizingMode.AutoSize,
            ControlPadding = new Vector2(6, 2),
            Parent = parent,
        };

    public static Label AddLabel(Container parent, string text)
        => new()
        {
            Text = text,
            AutoSizeWidth = true,
            AutoSizeHeight = true,
            Parent = parent,
        };

    /// <summary>
    /// Ligne "label + TextBox". Le commit (onCommit) se declenche sur Entree et sur perte de
    /// focus (blur) - jamais a chaque frappe, pour ne pas ecrire sur disque a chaque touche
    /// ni essayer de parser une valeur numerique a moitie tapee.
    /// </summary>
    public static TextBox AddLabeledTextBox(Container parent, string label, string initialValue, Action<string> onCommit, int textBoxWidth = 110)
    {
        var row = AddRow(parent);
        AddLabel(row, label);

        var textBox = new TextBox
        {
            Text = initialValue,
            Width = textBoxWidth,
            Parent = row,
        };

        void Commit() => onCommit(textBox.Text);

        textBox.EnterPressed += (_, _) => Commit();
        textBox.InputFocusChanged += (_, e) =>
        {
            if (!e.Value)
                Commit();
        };

        return textBox;
    }

    /// <summary>Ligne "checkbox" avec commit immediat (les toggles n'ont pas besoin de blur/enter).</summary>
    public static Checkbox AddLabeledCheckbox(Container parent, string label, bool initialValue, Action<bool> onCommit)
    {
        var row = AddRow(parent);
        var checkbox = new Checkbox
        {
            Text = label,
            Checked = initialValue,
            Parent = row,
        };

        checkbox.CheckedChanged += (_, _) => onCommit(checkbox.Checked);

        return checkbox;
    }

    /// <summary>
    /// Bouton a confirmation en 2 clics ("Supprimer" -> "Confirmer ?" -> execute). Evite une
    /// suppression accidentelle sans avoir besoin d'une fenetre modale. Ne se re-arme pas
    /// tout seul (pas de timer) - un rafraichissement de la liste (RebuildList) reconstruit
    /// le bouton desarme de toute facon.
    /// </summary>
    public static StandardButton AddConfirmButton(Container parent, string label, Action onConfirmed)
    {
        var button = new StandardButton { Text = label, Parent = parent };
        var armed = false;

        button.Click += (_, _) =>
        {
            if (!armed)
            {
                armed = true;
                button.Text = "Confirmer ?";
                return;
            }

            onConfirmed();
        };

        return button;
    }

    public static string FormatVector4(System.Numerics.Vector4 v)
        => string.Join(",",
            v.X.ToString(CultureInfo.InvariantCulture),
            v.Y.ToString(CultureInfo.InvariantCulture),
            v.Z.ToString(CultureInfo.InvariantCulture),
            v.W.ToString(CultureInfo.InvariantCulture));

    public static System.Numerics.Vector4 ParseVector4(string text, System.Numerics.Vector4 fallback)
    {
        var parts = text.Split(',');
        if (parts.Length != 4)
            return fallback;

        if (TryParseFloat(parts[0], out var x) && TryParseFloat(parts[1], out var y)
            && TryParseFloat(parts[2], out var z) && TryParseFloat(parts[3], out var w))
            return new System.Numerics.Vector4(x, y, z, w);

        return fallback;
    }

    public static string FormatVector2(System.Numerics.Vector2 v)
        => string.Join(",", v.X.ToString(CultureInfo.InvariantCulture), v.Y.ToString(CultureInfo.InvariantCulture));

    public static System.Numerics.Vector2 ParseVector2(string text, System.Numerics.Vector2 fallback)
    {
        var parts = text.Split(',');
        if (parts.Length != 2)
            return fallback;

        if (TryParseFloat(parts[0], out var x) && TryParseFloat(parts[1], out var y))
            return new System.Numerics.Vector2(x, y);

        return fallback;
    }

    public static bool TryParseFloat(string text, out float value)
        => float.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);

    public static bool TryParseUInt(string text, out uint value)
        => uint.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);

    public static bool TryParseInt(string text, out int value)
        => int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
}
