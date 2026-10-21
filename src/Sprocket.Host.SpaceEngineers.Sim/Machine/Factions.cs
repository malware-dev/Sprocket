using System.Collections.Generic;
using Sprocket.Sim.Fakes;
using VRage.Collections;
using VRage.Game;
using VRage.Game.ModAPI;

namespace Sprocket.Sim.Machine;

/// <summary>Which faction each player is in, by the identity the game knows them by.</summary>
/// <remarks>What the game's own access test reads for a block shared with its owner's faction.</remarks>
public sealed class Factions
{
    readonly Dictionary<long, string> _tags = new Dictionary<long, string>();
    readonly Dictionary<string, IMyFaction> _factions = new Dictionary<string, IMyFaction>();
    IMyFactionCollection _collection;

    /// <summary>The factions of the rig installed last, which the session's faction collection answers for.</summary>
    /// <remarks>The game has one session per process, and the harness puts one rig behind it at a time.</remarks>
    public static Factions Installed { get; set; }

    /// <summary>These factions as the game's faction collection, which finds the faction a player is in.</summary>
    public IMyFactionCollection Collection => _collection ?? (_collection = Pretend.To<IMyFactionCollection>()
        .Does("TryGetPlayerFaction", args => Of((long)args[0]))
        .Done());

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

    /// <summary>The faction <paramref name="identity" /> is in, as the game's faction, or null where they are in none.</summary>
    IMyFaction Of(long identity)
    {
        string tag;
        if (!_tags.TryGetValue(identity, out tag))
            return null;

        IMyFaction faction;
        if (!_factions.TryGetValue(tag, out faction))
        {
            var id = 1000L + _factions.Count;
            faction = Pretend.To<IMyFaction>()
                .Gives("FactionId", () => id)
                .Gives("Tag", () => tag)
                .Gives("Members", () => Members(tag))
                .Done();
            _factions[tag] = faction;
        }

        return faction;
    }

    /// <summary>The players in the faction <paramref name="tag" />, as the game lists a faction's members.</summary>
    DictionaryReader<long, MyFactionMember> Members(string tag)
    {
        var members = new Dictionary<long, MyFactionMember>();
        foreach (var each in _tags)
        {
            if (each.Value == tag)
                members[each.Key] = new MyFactionMember(each.Key, false);
        }

        return new DictionaryReader<long, MyFactionMember>(members);
    }
}
