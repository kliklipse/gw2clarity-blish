using System.Collections.Generic;

namespace GW2ClarityBlish.Models;

public class GridItem
{
    public (int X, int Y) Position { get; set; }
    public Buff Buff { get; set; } = Buff.Unknown;
    public uint StyleId { get; set; }
    public List<Buff> AdditionalBuffs { get; set; } = new();
}
