using System;
using System.Collections.Generic;
using System.Linq;
using GW2ClarityBlish.Models;
using GW2ClarityBlish.Services;

namespace GW2ClarityBlish.Module.UI;

/// <summary>
/// Unite de travail en memoire pour les 3 panneaux de configuration (Grids/Layouts/Styles).
/// Ne reinvente pas la persistence : delegue entierement a GridStore/LayoutStore/StyleStore
/// (Load() a la construction, Save() apres chaque mutation).
/// </summary>
/// <remarks>
/// Les references croisees entre entites sont positionnelles (aucun Id stable dans les
/// modeles racine) : <see cref="GridItem.StyleId"/> est un index dans <see cref="Styles"/>
/// (voir GridRenderer.BuildInstances : <c>styles[(int)item.StyleId]</c>) et
/// <see cref="Layout.GridIds"/> contient des index dans <see cref="Grids"/>. Ce contexte est
/// donc responsable de corriger ces references (decalage d'index / suppression) quand une
/// Grid ou un Style est supprime, pour eviter qu'une suppression corrompe silencieusement
/// une autre entite (ex: un item qui se retrouve avec le mauvais Style parce que tous les
/// index ont glisse d'un cran).
/// </remarks>
public sealed class ConfigContext
{
    private readonly GridStore _gridStore;
    private readonly LayoutStore _layoutStore;
    private readonly StyleStore _styleStore;

    public List<Grid> Grids { get; }
    public List<Layout> Layouts { get; }
    public List<Style> Styles { get; }

    /// <summary>
    /// Index du Layout actif dans <see cref="Layouts"/>, ou null si aucun n'est actif.
    /// Purement en memoire : aucun champ prevu dans le modele <see cref="Layout"/> pour
    /// persister ce choix. A brancher sur un SettingEntry&lt;int&gt; (mecanisme standard
    /// Blish HUD pour l'etat persistant leger) par le wiring Module.cs (tache 11) si la
    /// persistance inter-session est souhaitee - <see cref="ActiveLayoutChanged"/> est fait
    /// pour ca.
    /// </summary>
    public int? ActiveLayoutIndex { get; private set; }

    /// <summary>
    /// Leve a chaque changement de Layout actif (y compris passage a null). Le wiring
    /// Module.cs (tache 11) peut s'y abonner pour persister le choix.
    /// </summary>
    public event Action<int?>? ActiveLayoutChanged;

    public ConfigContext(
        GridStore gridStore,
        LayoutStore layoutStore,
        StyleStore styleStore,
        int? initialActiveLayoutIndex = null)
    {
        _gridStore = gridStore ?? throw new ArgumentNullException(nameof(gridStore));
        _layoutStore = layoutStore ?? throw new ArgumentNullException(nameof(layoutStore));
        _styleStore = styleStore ?? throw new ArgumentNullException(nameof(styleStore));

        Grids = _gridStore.Load();
        Layouts = _layoutStore.Load();
        Styles = _styleStore.Load();

        ActiveLayoutIndex = initialActiveLayoutIndex is int i && i >= 0 && i < Layouts.Count
            ? i
            : null;
    }

    public void SaveGrids() => _gridStore.Save(Grids);

    public void SaveLayouts() => _layoutStore.Save(Layouts);

    public void SaveStyles() => _styleStore.Save(Styles);

    public void SetActiveLayout(int? index)
    {
        if (index is int i && (i < 0 || i >= Layouts.Count))
            index = null;

        if (ActiveLayoutIndex == index)
            return;

        ActiveLayoutIndex = index;
        ActiveLayoutChanged?.Invoke(index);
    }

    /// <summary>
    /// Supprime la Grid a <paramref name="index"/> et corrige toutes les references
    /// positionnelles qui pointent dessus ou au-dela (<see cref="Layout.GridIds"/>).
    /// Persiste Grids et Layouts.
    /// </summary>
    public void DeleteGrid(int index)
    {
        if (index < 0 || index >= Grids.Count)
            return;

        Grids.RemoveAt(index);

        foreach (var layout in Layouts)
        {
            if (layout.GridIds.Count == 0)
                continue;

            var shifted = new HashSet<int>(
                layout.GridIds
                    .Where(id => id != index)
                    .Select(id => id > index ? id - 1 : id));

            layout.GridIds.Clear();
            foreach (var id in shifted)
                layout.GridIds.Add(id);
        }

        SaveGrids();
        SaveLayouts();
    }

    /// <summary>
    /// Supprime le Style a <paramref name="index"/> et corrige toutes les references
    /// positionnelles (<see cref="GridItem.StyleId"/>) dans toutes les Grids. Les items qui
    /// pointaient exactement sur le Style supprime retombent sur StyleId 0 (visible
    /// immediatement en UI comme "premier style de la liste" apres rafraichissement -
    /// jamais un crash ni un index hors bornes muet). Persiste Styles et Grids.
    /// </summary>
    public void DeleteStyle(int index)
    {
        if (index < 0 || index >= Styles.Count)
            return;

        Styles.RemoveAt(index);

        foreach (var grid in Grids)
        {
            foreach (var item in grid.Items)
            {
                if (item.StyleId == (uint)index)
                    item.StyleId = 0;
                else if (item.StyleId > (uint)index)
                    item.StyleId -= 1;
            }
        }

        SaveStyles();
        SaveGrids();
    }

    /// <summary>
    /// Supprime le Layout a <paramref name="index"/> et corrige <see cref="ActiveLayoutIndex"/>
    /// s'il pointait sur l'entree supprimee ou au-dela. Persiste Layouts.
    /// </summary>
    public void DeleteLayout(int index)
    {
        if (index < 0 || index >= Layouts.Count)
            return;

        Layouts.RemoveAt(index);

        if (ActiveLayoutIndex is int active)
        {
            if (active == index)
                SetActiveLayout(null);
            else if (active > index)
                SetActiveLayout(active - 1);
        }

        SaveLayouts();
    }
}
