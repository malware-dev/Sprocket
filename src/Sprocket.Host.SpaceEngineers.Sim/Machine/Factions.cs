using System.Collections.Generic;

namespace Sprocket.Sim.Machine;

/// <summary>Which faction each player is in, by the identity the game knows them by.</summary>
/// <remarks>What the game's own access test reads for a block shared with its owner's faction.</remarks>
public sealed class Factions
{
    readonly Dictionary<long, string> _tags = new Dictionary<long, string>();

    /// <summary>Puts <paramref name="identity" /> in the faction <paramref name="tag" />, out of any other.</summary>
    public void Join(long identity, string tag)
    {
        _tags[identity] = tag;
    }

    /// <summary>Takes <paramref name="identity" /> out of its faction.</summary>
    public void Leave(long identity)
    {
        _tags.Remove(identity);
    }

    /// <summary>Whether <paramref name="one" /> and <paramref name="other" /> are in the same faction.</summary>
    public bool Same(long one, long other)
    {
        string a;
        string b;
        return _tags.TryGetValue(one, out a) && _tags.TryGetValue(other, out b) && a == b;
    }
}
