using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Blish_HUD;
using Blish_HUD.GameServices.ArcDps.V2;
using Blish_HUD.GameServices.ArcDps.V2.Models;
using GW2ClarityBlish.Services;

namespace GW2ClarityBlish.Module.Services;

/// <summary>
/// Cote host reel (net472) du cablage ArcDPS : s'abonne au flux ArcDPS V2 integre au SDK Blish
/// HUD (<see cref="GameService.ArcDpsV2"/>) et traduit les <see cref="CombatEvent"/> bruts
/// touchant le joueur local en changements de stacks, relayes vers
/// <see cref="ArcdpsBridgeBuffEventSource"/> (projet racine, netstandard2.0, sans dependance
/// BlishHUD - voir le commentaire de ce fichier pour l'explication de cette separation).
/// </summary>
/// <remarks>
/// <para>
/// API verifiee par reflexion sur le package NuGet BlishHUD 1.3.0 (assembly "Blish HUD.exe") :
/// <see cref="ArcDpsServiceV2.RegisterMessageType{T}(IArcDpsMessageListener{T})"/>,
/// <see cref="ArcDpsMessageListener{T}"/> (ctor <c>(MessageType, Func&lt;T, CancellationToken, Task&gt;)</c>,
/// <see cref="IDisposable"/>), <see cref="MessageType.CombatEventArea"/>, et la forme exacte de
/// <see cref="CombatCallback"/>/<see cref="CombatEvent"/>/<see cref="Agent"/> (champs <c>Buff</c>,
/// <c>SkillId</c>, <c>IsBuffRemoved</c>, <c>BuffDamage</c>, <c>Value</c>, <c>Agent.Self</c>).
/// Rien de ce qui suit n'est invente - seule la LOGIQUE de simulation de stacks ci-dessous est
/// une simplification assumee (documentee point par point) : evtc/ArcDPS n'expose pas un compteur
/// de stacks directement, seulement des evenements d'application/retrait bruts.
/// </para>
/// <para>
/// <b>TODO simulation de stacks (raffinement futur, pas un blocage pour cette tache)</b> :
/// - Chaque evenement d'application (<c>Buff == true</c>, <c>IsBuffRemoved == None</c>,
///   <c>BuffDamage == 0</c>) incremente le compteur du buff de 1. C'est correct pour les buffs a
///   stacks d'intensite (plusieurs applications simultanees = plusieurs stacks), mais ne modelise
///   pas la duree individuelle de chaque stack (<c>CombatEvent.Value</c>/<c>OverstackValue</c>
///   sont ignores) : pas de decompte "stack qui expire seul" en dehors d'un evenement de retrait
///   explicite - hypothese habituellement valide car ArcDPS emet un evenement de retrait meme a
///   expiration naturelle, mais non verifiee en jeu par ce cablage.
/// - <see cref="BuffRemove.Single"/> decremente de 1 (retrait d'une seule stack, ex. une charge de
///   Might qui expire ou est purgee individuellement).
/// - <see cref="BuffRemove.All"/>, <see cref="BuffRemove.Manual"/> et <see cref="BuffRemove.Unknown"/>
///   remettent le compteur a 0 (retrait complet) - simplification prudente : mieux vaut un flash a
///   "0 stack" trop tot qu'un stack fantome jamais nettoye (voir principe "jamais de faux positif"
///   de <see cref="ArcdpsBridgeBuffEventSource"/>).
/// - Les evenements avec <c>BuffDamage != 0</c> (tick de degats de condition, pas un
///   changement de stack) sont ignores.
/// </para>
/// <para>
/// <see cref="MessageType.CombatEventArea"/> a ete choisi plutot que <c>CombatEventLocal</c> pour
/// couvrir tous les evenements pertinents pour le joueur local (le filtre
/// <c>Agent.Self == 1</c> ci-dessous s'applique dans les deux cas) ; la difference exacte entre
/// les deux n'est pas documentee dans le SDK 1.3.0 - a affiner en jeu si "Area" s'avere trop
/// bruyant en groupe/raid.
/// </para>
/// </remarks>
public sealed class ArcdpsBuffTracker : IDisposable
{
    private readonly ArcdpsBridgeBuffEventSource _sink;
    private readonly Logger _logger = Logger.GetLogger<ArcdpsBuffTracker>();
    private readonly ConcurrentDictionary<uint, int> _stacks = new();

    private ArcDpsMessageListener<CombatCallback>? _listener;

    public ArcdpsBuffTracker(ArcdpsBridgeBuffEventSource sink)
    {
        _sink = sink ?? throw new ArgumentNullException(nameof(sink));
    }

    /// <summary>
    /// Enregistre le listener aupres de <see cref="GameService.ArcDpsV2"/>. Sans effet si deja
    /// demarre. Ne verifie pas <see cref="ArcDpsServiceV2.Running"/> : l'enregistrement est
    /// idempotent et sans risque meme si ArcDPS/le bridge natif n'est pas (encore) connecte -
    /// les evenements ne feront simplement jamais parvenir de <see cref="CombatCallback"/> tant
    /// que ce n'est pas le cas, et aucun stack ne sera jamais rapporte (voir gating dans
    /// GW2ClarityModule.LoadAsync : avertissement logge si <see cref="ArcDpsServiceV2.Running"/>
    /// est faux, sans jamais faire planter le module).
    /// </summary>
    public void Start()
    {
        if (_listener is not null)
            return;

        _listener = new ArcDpsMessageListener<CombatCallback>(MessageType.CombatEventArea, OnCombatEventAsync);
        GameService.ArcDpsV2.RegisterMessageType(_listener);
    }

    private Task OnCombatEventAsync(CombatCallback callback, CancellationToken cancellationToken)
    {
        // On ne suit que les buffs qui s'appliquent/se retirent sur le joueur local : c'est un
        // tracker de grille personnelle (comme un tableau de boons perso), pas un log de combat
        // pour le groupe entier.
        if (callback.Destination.Self != 1)
            return Task.CompletedTask;

        var ev = callback.Event;

        // Buff == false : evenement de degats/activation/etat classique, hors sujet ici.
        // BuffDamage != 0 : tick de degats d'une condition (ex. brulure), pas un changement de
        // stack - voir TODO de simulation dans la remarque de classe.
        if (!ev.Buff || ev.BuffDamage != 0)
            return Task.CompletedTask;

        var buffId = ev.SkillId;
        if (buffId == 0)
            return Task.CompletedTask;

        var updated = ev.IsBuffRemoved switch
        {
            BuffRemove.None => _stacks.AddOrUpdate(buffId, 1, (_, current) => current + 1),
            BuffRemove.Single => _stacks.AddOrUpdate(buffId, 0, (_, current) => Math.Max(0, current - 1)),
            _ => _stacks.AddOrUpdate(buffId, 0, (_, _) => 0), // All / Manual / Unknown : retrait complet
        };

        _sink.ReportBuffStackChanged(buffId, updated);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _listener?.Dispose();
        _listener = null;
    }
}
