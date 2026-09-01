namespace GW2ClarityBlish.Services;

public class BuffStateService
{
    private readonly Dictionary<uint, int> _activeStacks = new();

    public event Action? BuffsChanged;

    public BuffStateService(IBuffEventSource source)
    {
        source.BuffStackChanged += OnBuffStackChanged;
    }

    private void OnBuffStackChanged(uint buffId, int stacks)
    {
        _activeStacks[buffId] = stacks;
        BuffsChanged?.Invoke();
    }

    public int GetStacks(uint buffId) => _activeStacks.GetValueOrDefault(buffId, 0);
}
