using System.Collections.Generic;

namespace GW2ClarityBlish.Models;

public class Layout
{
    public string Name { get; set; } = "New Set";
    public HashSet<int> GridIds { get; set; } = new();
    public bool CombatOnly { get; set; }

    public bool ShouldRender(bool isInCombat, bool isInCompetitiveMode)
    {
        if (isInCompetitiveMode)
            return false;

        return !CombatOnly || isInCombat;
    }
}
