using System;
using System.Collections.Generic;

namespace Sprocket.Sim.Machine;

/// <summary>
/// One machine on a <see cref="Wire" />: the server, or a player's client.
/// </summary>
/// <remarks>
/// A machine in the game is a process of its own, with its own copy of the mod. Here every machine
/// shares one process, so a peer is what a test runs the mod's code as - see <see cref="Wire.As" />.
/// </remarks>
public sealed class Peer
{
    /// <summary>A machine known on the wire by <paramref name="id" />, the way the game knows a player by Steam id.</summary>
    public Peer(string name, ulong id, bool isServer, bool isHeadless)
    {
        Name = name;
        Id = id;
        IsServer = isServer;
        IsHeadless = isHeadless;
    }

    /// <summary>What the machine's player is called, which the game reports as the local name.</summary>
    public string Name { get; }

    /// <summary>What messages to and from it are addressed by.</summary>
    public ulong Id { get; }

    /// <summary>Whether it is the server, which is what simulates the grids and decides.</summary>
    public bool IsServer { get; }

    /// <summary>Whether it is a server with nobody at it, which has no screen and no player.</summary>
    public bool IsHeadless { get; }

    /// <summary>What listens on each channel on this machine.</summary>
    internal Dictionary<ushort, List<Action<ushort, byte[], ulong, bool>>> Handlers { get; } =
        new Dictionary<ushort, List<Action<ushort, byte[], ulong, bool>>>();

    /// <inheritdoc />
    public override string ToString()
    {
        return Name;
    }
}
