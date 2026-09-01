namespace GW2ClarityBlish.Tests;

using GW2ClarityBlish.Models;
using System.Collections.Generic;

public class BuffTests
{
    [Fact]
    public void GetStacks_SumsMainIdAndExtraIds()
    {
        var buff = new Buff(id: 100, name: "Might", extraIds: new HashSet<uint> { 101, 102 });
        var active = new Dictionary<uint, int> { [100] = 3, [101] = 2, [102] = 0 };

        Assert.Equal(5, buff.GetStacks(active));
    }

    [Fact]
    public void GetStacks_ReturnsZero_WhenBuffNotInActiveMap()
    {
        var buff = new Buff(id: 200, name: "Quickness");
        var active = new Dictionary<uint, int>();

        Assert.Equal(0, buff.GetStacks(active));
    }

    [Fact]
    public void ShowNumber_TrueOnlyWhenMultiStackAndCountAboveOne()
    {
        var stackable = new Buff(id: 1, name: "Bleeding", maxStacks: 25);
        var singleStack = new Buff(id: 2, name: "Protection", maxStacks: 1);

        Assert.True(stackable.ShowNumber(3));
        Assert.False(stackable.ShowNumber(1));
        Assert.False(singleStack.ShowNumber(5));
    }
}
