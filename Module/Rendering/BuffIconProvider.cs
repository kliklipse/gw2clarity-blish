using System;
using System.Collections.Generic;
using System.Linq;
using Blish_HUD;
using Blish_HUD.Modules.Managers;
using GW2ClarityBlish.Models;
using Microsoft.Xna.Framework.Graphics;

namespace GW2ClarityBlish.Module.Rendering;

/// <summary>
/// Resout un id de buff vers sa texture d'icone individuelle, chargee depuis
/// <c>Module/ref/icons/*.png</c> via <see cref="ContentsManager"/>. Remplace l'ancien modele
/// d'atlas (jamais reellement branche - <c>IconAtlas</c> restait toujours null) : chaque buff du
/// catalogue a son propre fichier PNG, pas de tuile dans une texture partagee.
/// </summary>
/// <remarks>
/// Chaque texture n'est chargee qu'une seule fois (cache par buff id) : <see cref="GetIcon"/> est
/// appelee a chaque frame depuis <see cref="GridRendererControl.DrawIcon"/>, donc le cout d'IO de
/// <see cref="ContentsManager.GetTexture(string)"/> ne doit pas etre paye a repetition.
///
/// Panne silencieuse a eviter : si <see cref="ContentsManager.GetTexture(string)"/> leve une
/// exception (fichier absent/corrompu), on catch et on cache <c>null</c> POUR CE BUFF PRECIS -
/// jamais de propagation qui ferait planter le module ou empecherait le chargement des icones des
/// autres buffs deja en cache ou a venir.
/// </remarks>
public sealed class BuffIconProvider
{
    private readonly Logger _logger = Logger.GetLogger<BuffIconProvider>();
    private readonly ContentsManager _contentsManager;
    private readonly Dictionary<uint, Texture2D?> _cache = new();

    public BuffIconProvider(ContentsManager contentsManager)
    {
        _contentsManager = contentsManager ?? throw new ArgumentNullException(nameof(contentsManager));
    }

    /// <summary>
    /// Retourne la texture d'icone pour <paramref name="buffId"/>, ou <c>null</c> si ce buff
    /// n'est pas dans <see cref="BuffCatalog"/>, n'a pas d'<c>IconFileName</c> connu, ou si le
    /// chargement de son fichier a echoue. <c>null</c> signale a l'appelant de retomber sur le
    /// rectangle tinte existant.
    /// </summary>
    public Texture2D? GetIcon(uint buffId)
    {
        if (_cache.TryGetValue(buffId, out var cached))
            return cached;

        var texture = LoadIcon(buffId);
        _cache[buffId] = texture;
        return texture;
    }

    private Texture2D? LoadIcon(uint buffId)
    {
        var entry = BuffCatalog.Entries.FirstOrDefault(e => e.Id == buffId);
        if (entry?.IconFileName is null)
            return null;

        try
        {
            // ContentsManager resout ses chemins relativement a la racine du dossier ref/ du
            // module (confirme via TryLoadBorderGlowEffect / "rendering/GridEffect.mgfx") : les
            // icones vivant dans Module/ref/icons/, le chemin attendu ici est "icons/<fichier>",
            // pas "ref/icons/<fichier>".
            return _contentsManager.GetTexture($"icons/{entry.IconFileName}");
        }
        catch (Exception ex)
        {
            _logger.Warn(ex,
                $"Icone du buff '{entry.Name}' (id {buffId}, fichier {entry.IconFileName}) " +
                "introuvable ou invalide : rectangle tinte utilise a la place pour ce buff.");
            return null;
        }
    }
}
