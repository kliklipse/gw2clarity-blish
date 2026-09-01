namespace GW2ClarityBlish.Models;

/// <summary>
/// Une entree du catalogue statique de buffs GW2 courants (boons, conditions,
/// quelques buffs mecaniques utiles en raid/strike).
/// </summary>
/// <param name="Id">
/// Identifiant numerique interne arcdps/client du jeu (PAS un id de l'API officielle
/// GW2 <c>/v2/skills</c>, qui rejette ces ids avec "no such id" - verifie manuellement).
/// </param>
/// <param name="Name">Nom lisible affiche a l'utilisateur.</param>
/// <param name="Category">Categorie grossiere : "Boon", "Condition" ou "Autre".</param>
/// <param name="IconFileName">
/// Nom du fichier PNG bundle dans <c>Module/ref/icons/</c> (sans chemin), ou null si
/// aucune icone fiable n'a ete trouvee - retombe alors sur le rectangle tinte existant.
/// </param>
public record BuffCatalogEntry(uint Id, string Name, string Category, string? IconFileName);

/// <summary>
/// Catalogue statique de buffs GW2 courants, pour remplacer le champ "Buff Id" en
/// saisie libre par une recherche par nom.
///
/// Aucun id de ce catalogue n'est invente : chaque entree est verifiee contre au moins
/// deux sources independantes avant d'etre ajoutee. Une entree qui ne peut pas etre
/// confirmee par deux sources concordantes est omise plutot que devinee.
///
/// Sources consultees (2026-09-02) :
/// - GW2-Elite-Insights-Parser (baaron4/GW2-Elite-Insights-Parser), fichier source
///   "GW2EIEvtcParser/ParserHelpers/IDs/SkillIDs.cs" (constantes numeriques des boons/
///   conditions communs) et "GW2EIEvtcParser/EIData/Buffs/CommonBuffs.cs" (classification
///   Boon/Condition/Support). C'est la reference que le parseur de logs de combat evtc le
///   plus utilise par la communaute raid/strike maintient et met a jour a chaque patch.
///   https://github.com/baaron4/GW2-Elite-Insights-Parser
/// - GW2Clarity original (kaesekaiser/GW2Clarity, module C++ predecesseur direct de ce
///   module Blish HUD), fichier "GW2Clarity/src/BuffsList.inc" - liste deja verifiee et
///   maintenue independamment par l'auteur du module d'origine, elle-meme sourcee sur
///   GW2-Elite-Insights-Parser mais recoupee/corrigee au fil des patches.
/// - wiki.guildwars2.com, page "User:Frvwfr2/buffids" (wiki communautaire GW2) - confirme
///   independamment les ids des boons de groupe et de plusieurs debuffs mecaniques
///   (Fear=791, Stun=872, Daze=833, Taunt=27705, Superspeed=5974, etc).
///
/// Note historique : l'id 873 s'appelait "Retaliation" avant le remaniement des
/// competences Gardien de mai 2021 ; il porte depuis le nom "Resolution". Les trois
/// sources s'accordent sur l'id, seul le nom differe selon la date de la source - le nom
/// courant "Resolution" est retenu ici.
///
/// Icones : telechargees depuis wiki.guildwars2.com (licence de contenu fan permissive
/// pour ce type d'usage, meme pratique que le module GW2Clarity d'origine) via l'API
/// MediaWiki (resolution du nom de fichier -> URL reelle), puis bundlees localement dans
/// <c>Module/ref/icons/</c> - aucun hotlink au runtime.
/// </summary>
public static class BuffCatalog
{
    private const string Boon = "Boon";
    private const string Condition = "Condition";
    private const string Other = "Autre";

    public static readonly IReadOnlyList<BuffCatalogEntry> Entries = new List<BuffCatalogEntry>
    {
        // Boons de groupe (les 5 boons "de raid" partages par les supports)
        new(740, "Might", Boon, "might.png"),
        new(725, "Fury", Boon, "fury.png"),
        new(1187, "Quickness", Boon, "quickness.png"),
        new(30328, "Alacrity", Boon, "alacrity.png"),
        new(717, "Protection", Boon, "protection.png"),

        // Boons individuels
        new(718, "Regeneration", Boon, "regeneration.png"),
        new(726, "Vigor", Boon, "vigor.png"),
        new(719, "Swiftness", Boon, "swiftness.png"),
        new(1122, "Stability", Boon, "stability.png"),
        new(743, "Aegis", Boon, "aegis.png"),
        new(873, "Resolution", Boon, "resolution.png"),
        new(26980, "Resistance", Boon, "resistance.png"),

        // Conditions courantes
        new(738, "Vulnerability", Condition, "vulnerability.png"),
        new(742, "Weakness", Condition, "weakness.png"),
        new(723, "Poison", Condition, "poison.png"),
        new(736, "Bleeding", Condition, "bleeding.png"),
        new(737, "Burning", Condition, "burning.png"),
        new(861, "Confusion", Condition, "confusion.png"),
        new(19426, "Torment", Condition, "torment.png"),
        new(722, "Chilled", Condition, "chilled.png"),
        new(721, "Crippled", Condition, "crippled.png"),
        new(727, "Immobile", Condition, "immobile.png"),
        new(791, "Fear", Condition, "fear.png"),
        new(720, "Blinded", Condition, "blinded.png"),
        new(26766, "Slow", Condition, "slow.png"),
        new(27705, "Taunt", Condition, "taunt.png"),

        // Buffs mecaniques utiles en raid/strike
        new(5974, "Superspeed", Other, "superspeed.png"),
        new(13017, "Stealth", Other, "stealth.png"),
    };

    /// <summary>
    /// Recherche par sous-chaine du nom, insensible a la casse.
    /// </summary>
    public static IReadOnlyList<BuffCatalogEntry> Search(string query)
    {
        if (string.IsNullOrEmpty(query))
            return Entries;

        return Entries
            .Where(e => e.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
            .ToList();
    }
}
