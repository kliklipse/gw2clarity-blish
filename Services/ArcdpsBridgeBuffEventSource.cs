namespace GW2ClarityBlish.Services;

/// <summary>
/// Adapter concret vers l'integration ArcDPS native du SDK Blish HUD (<c>GameService.ArcDpsV2</c>,
/// verifiee par reflexion sur le package NuGet BlishHUD 1.3.0 - PAS un module "ArcDPS Bridge"
/// separe : ArcDPS V2 est integre au coeur du SDK depuis 1.3.0, cf. commentaire manifest.json).
/// </summary>
/// <remarks>
/// Ce projet racine est <c>netstandard2.0</c> et ne reference volontairement ni BlishHUD ni
/// MonoGame (voir commentaire de GW2ClarityBlish.csproj : "compatible a la fois avec les tests
/// xUnit (net8.0) et un futur host de module Blish HUD reel (net472)"). Les types du flux ArcDPS
/// V2 (<c>Blish_HUD.GameServices.ArcDps.V2.Models.CombatCallback</c>,
/// <c>MessageType</c>, etc.) ne sont definis que dans l'assembly BlishHUD (net472) : cette classe
/// ne peut donc pas s'abonner elle-meme a <c>GameService.ArcDpsV2.RegisterMessageType</c>.
/// <para>
/// Le cablage reel vit cote host : <c>Module/Services/ArcdpsBuffTracker.cs</c> (net472) s'abonne
/// au vrai flux ArcDPS V2 (<c>GameService.ArcDpsV2.RegisterMessageType&lt;CombatCallback&gt;</c>
/// avec <c>MessageType.CombatEventArea</c>), filtre sur le joueur local
/// (<c>Agent.Self == 1</c>), simule un compteur de stacks par buff a partir des
/// <c>CombatEvent</c> bruts (champs <c>Buff</c>/<c>IsBuffRemoved</c>/<c>SkillId</c> - voir
/// commentaires de ce fichier host pour le detail et les limites de la simulation), et appelle
/// <see cref="ReportBuffStackChanged"/> ici a chaque changement reel.
/// </para>
/// <para>
/// Tant que rien n'a ete rapporte (ArcDPS absent, module non demarre, joueur jamais buffe),
/// <see cref="BuffStateService.GetStacks"/> reste a 0 : jamais de stack invente, jamais de "actif"
/// par defaut - meme principe que les sondes de sante forge-app (ok/ko/indetermine, jamais de vert
/// par defaut).
/// </para>
/// </remarks>
public class ArcdpsBridgeBuffEventSource : IBuffEventSource
{
    public event Action<uint, int>? BuffStackChanged;

    /// <summary>
    /// Point d'entree appele par le host Module (net472, <c>ArcdpsBuffTracker</c>) a chaque
    /// changement reel de stacks detecte depuis le flux ArcDPS V2. Ne fait aucune validation
    /// metier ici : la logique de simulation (apply/remove, clamp a 0) vit entierement cote
    /// host, la ou les types ArcDPS reels sont disponibles pour l'ecrire correctement.
    /// </summary>
    public void ReportBuffStackChanged(uint buffId, int stacks) => BuffStackChanged?.Invoke(buffId, stacks);
}
