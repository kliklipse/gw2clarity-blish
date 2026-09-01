namespace GW2ClarityBlish.Models;

public class Buff
{
    public uint Id { get; }
    public string Name { get; }
    public int MaxStacks { get; }
    public IReadOnlySet<uint> ExtraIds { get; }
    public string Category { get; init; } = "";
    public string AtlasEntry { get; init; } = "";
    public System.Numerics.Vector2 Uv { get; init; }

    public static readonly Buff Unknown = new(0, "Unknown", 1);

    public Buff(uint id, string name, int maxStacks = int.MaxValue, IReadOnlySet<uint>? extraIds = null)
    {
        Id = id;
        Name = name;
        MaxStacks = maxStacks;
        ExtraIds = extraIds ?? new HashSet<uint>();
    }

    public int GetStacks(IReadOnlyDictionary<uint, int> activeBuffs)
    {
        int total = activeBuffs.GetValueOrDefault(Id, 0);
        foreach (var extraId in ExtraIds)
            total += activeBuffs.GetValueOrDefault(extraId, 0);
        return total;
    }

    public bool ShowNumber(int count) => MaxStacks > 1 && count > 1;
}
