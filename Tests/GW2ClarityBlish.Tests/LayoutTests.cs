using Xunit;
using GW2ClarityBlish.Models;

namespace GW2ClarityBlish.Tests;

public class LayoutTests
{
    [Theory]
    [InlineData(false, false, false, true)]  // pas combatOnly, hors combat -> visible
    [InlineData(true, false, false, false)]  // combatOnly, hors combat -> masque
    [InlineData(true, true, false, true)]    // combatOnly, en combat -> visible
    [InlineData(true, true, true, false)]    // mode competitif -> toujours masque
    [InlineData(false, true, true, false)]   // mode competitif -> toujours masque, meme sans combatOnly
    public void ShouldRender_RespectsCombatOnlyAndCompetitiveMode(
        bool combatOnly, bool isInCombat, bool isInCompetitiveMode, bool expected)
    {
        var layout = new Layout { CombatOnly = combatOnly };

        Assert.Equal(expected, layout.ShouldRender(isInCombat, isInCompetitiveMode));
    }
}
