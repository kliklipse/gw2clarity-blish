namespace GW2ClarityBlish.Services;

public interface IBuffEventSource
{
    event Action<uint, int>? BuffStackChanged;
}
