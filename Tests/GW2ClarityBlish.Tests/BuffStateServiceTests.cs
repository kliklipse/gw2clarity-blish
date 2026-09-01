using Xunit;
using GW2ClarityBlish.Services;

namespace GW2ClarityBlish.Tests;

public class FakeBuffEventSource : IBuffEventSource
{
    public event Action<uint, int>? BuffStackChanged;

    public void Emit(uint buffId, int stacks) => BuffStackChanged?.Invoke(buffId, stacks);
}

public class BuffStateServiceTests
{
    [Fact]
    public void ActiveStacks_UpdatesOnApplyEvent()
    {
        var source = new FakeBuffEventSource();
        var service = new BuffStateService(source);

        source.Emit(buffId: 100, stacks: 3);

        Assert.Equal(3, service.GetStacks(100));
    }

    [Fact]
    public void ActiveStacks_ReturnsZero_OnRemoveEvent()
    {
        var source = new FakeBuffEventSource();
        var service = new BuffStateService(source);

        source.Emit(buffId: 100, stacks: 3);
        source.Emit(buffId: 100, stacks: 0);

        Assert.Equal(0, service.GetStacks(100));
    }

    [Fact]
    public void GetStacks_ReturnsZero_ForUnknownBuff_NeverThrows()
    {
        var source = new FakeBuffEventSource();
        var service = new BuffStateService(source);

        Assert.Equal(0, service.GetStacks(999999));
    }

    [Fact]
    public void BuffsChanged_RaisedOnEveryUpdate()
    {
        var source = new FakeBuffEventSource();
        var service = new BuffStateService(source);
        var raised = 0;
        service.BuffsChanged += () => raised++;

        source.Emit(100, 1);
        source.Emit(100, 2);

        Assert.Equal(2, raised);
    }
}
