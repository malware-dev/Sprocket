using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using Mal.MdkModMixin.Network;
using Sprocket.Sim.Fakes;
using VRage.Game.ModAPI;

namespace Sprocket.Sim.Machine;

/// <summary>
/// The network between the machines of one test: a server and its clients, in one process, with a
/// delay a test can set.
/// </summary>
/// <remarks>
/// The mod reaches the network through <see cref="NetworkChannel.Network" /> and asks the game whose
/// machine it is on through <c>MyAPIGateway.Multiplayer</c>. Both are statics, one for the process, so
/// the wire answers both for whichever machine is running now: a test runs the mod as a machine with
/// <see cref="As" />, and a message arriving runs its handler as the machine it arrived at.
/// <para>
/// Messages wait on the wire until it is ticked past their delay, so a test can hold a client's edit
/// in flight and look at both sides while it is. A message is copied the moment it is sent, as the
/// game does: the channel writes every message of one kind into the same buffer.
/// </para>
/// </remarks>
public sealed class Wire : INetworkProxy
{
    readonly List<Peer> _peers = new List<Peer>();
    readonly List<Parcel> _flying = new List<Parcel>();

    int _now;

    ulong _next = 76561190000000001UL;

    IMyMultiplayer _was;

    sealed class Parcel
    {
        public ushort Channel;
        public byte[] Data;
        public Peer From;
        public Peer To;
        public int Due;
    }

    /// <summary>How many ticks a message takes to arrive. Nought delivers it on the next tick.</summary>
    public int Latency { get; set; }

    /// <summary>The machine whose code is running, or null between runs.</summary>
    public Peer? Current { get; private set; }

    /// <summary>The server, where there is one yet.</summary>
    public Peer? Host { get; private set; }

    /// <summary>How many messages are waiting to arrive.</summary>
    public int Flying => _flying.Count;

    /// <summary>How many messages have arrived, over the life of the wire.</summary>
    public int Delivered { get; private set; }

    /// <inheritdoc />
    public bool IsHeadlessServer => Running.IsHeadless;

    /// <inheritdoc />
    public bool IsServer => Running.IsServer;

    /// <inheritdoc />
    public bool IsClient => !Running.IsHeadless;

    Peer Running => Current ?? throw new InvalidOperationException(
        "Nothing is running as a machine on the wire: run the mod's code inside Wire.As.");

    /// <summary>Adds the server, dedicated unless <paramref name="headless" /> says it is somebody's own game.</summary>
    public Peer Server(string name = "Server", bool headless = true)
    {
        if (Host != null)
            throw new InvalidOperationException("The wire has a server already.");

        Host = Add(new Peer(name, _next++, true, headless));
        return Host;
    }

    /// <summary>Adds a player's client.</summary>
    public Peer Client(string name)
    {
        return Add(new Peer(name, _next++, false, false));
    }

    Peer Add(Peer peer)
    {
        _peers.Add(peer);
        return peer;
    }

    /// <summary>
    /// Puts the wire behind the network and the multiplayer gateway the mod reads.
    /// </summary>
    /// <remarks>
    /// Undone by <see cref="Uninstall" />, which a test does on the way out so the next one starts on a
    /// single machine again.
    /// </remarks>
    public void Install()
    {
        NetworkChannel.Network = this;

        _was = MyAPIGateway.Multiplayer;
        MyAPIGateway.Multiplayer = Pretend.To<IMyMultiplayer>()
            .Gives("MyName", () => Running.Name)
            .Gives("MyId", () => Running.Id)
            .Gives("IsServer", () => Running.IsServer)
            .Gives("MultiplayerActive", () => true)
            .Gives("ServerId", () => Host?.Id ?? 0UL)
            .Done();
    }

    /// <summary>Puts the game's own network back behind the channel, and the gateway that was there before.</summary>
    public void Uninstall()
    {
        NetworkChannel.Network = new NetworkChannel.DefaultNetwork();
        MyAPIGateway.Multiplayer = _was;
    }

    /// <summary>Runs <paramref name="act" /> as <paramref name="peer" />, the way that machine's copy of the mod would.</summary>
    public void As(Peer peer, Action act)
    {
        var was = Current;
        Current = peer;
        try
        {
            act();
        }
        finally
        {
            Current = was;
        }
    }

    /// <summary>
    /// One tick on: every message whose delay has passed arrives, in the order it was sent, and runs
    /// its handlers as the machine it arrived at.
    /// </summary>
    public void Tick()
    {
        _now++;

        // Taken off before any handler runs, because a handler may send, and what it sends waits its
        // own delay rather than arriving in the same tick.
        var due = _flying.FindAll(parcel => parcel.Due <= _now);
        _flying.RemoveAll(parcel => parcel.Due <= _now);

        foreach (var parcel in due)
            Arrive(parcel);
    }

    /// <summary>Ticks until nothing is on the wire, or <paramref name="most" /> ticks have gone.</summary>
    public void Settle(int most = 100)
    {
        for (var i = 0; i < most && _flying.Count > 0; i++)
            Tick();
    }

    void Arrive(Parcel parcel)
    {
        List<Action<ushort, byte[], ulong, bool>> handlers;
        if (!parcel.To.Handlers.TryGetValue(parcel.Channel, out handlers))
            return;

        Delivered++;
        As(parcel.To, () =>
        {
            foreach (var handler in handlers.ToArray())
                handler(parcel.Channel, parcel.Data, parcel.From.Id, parcel.From.IsServer);
        });
    }

    void Send(ushort channel, byte[] data, Peer to)
    {
        _flying.Add(new Parcel
        {
            Channel = channel,
            Data = (byte[])data.Clone(),
            From = Running,
            To = to,
            Due = _now + Latency + 1
        });
    }

    /// <inheritdoc />
    public void RegisterSecureMessageHandler(ushort id, Action<ushort, byte[], ulong, bool> messageHandler)
    {
        List<Action<ushort, byte[], ulong, bool>> handlers;
        if (!Running.Handlers.TryGetValue(id, out handlers))
        {
            handlers = new List<Action<ushort, byte[], ulong, bool>>();
            Running.Handlers[id] = handlers;
        }

        handlers.Add(messageHandler);
    }

    /// <inheritdoc />
    public void UnregisterSecureMessageHandler(ushort id, Action<ushort, byte[], ulong, bool> messageHandler)
    {
        List<Action<ushort, byte[], ulong, bool>> handlers;
        if (Running.Handlers.TryGetValue(id, out handlers))
            handlers.Remove(messageHandler);
    }

    /// <inheritdoc />
    public void SendMessageTo(ushort channelId, byte[] data, ulong recipientId, bool reliable)
    {
        var to = _peers.Find(peer => peer.Id == recipientId);
        if (to != null)
            Send(channelId, data, to);
    }

    /// <inheritdoc />
    public void SendMessageToOthers(ushort channelId, byte[] data, bool reliable)
    {
        var from = Running;
        foreach (var peer in _peers)
        {
            if (peer != from)
                Send(channelId, data, peer);
        }
    }

    /// <inheritdoc />
    public void SendMessageToServer(ushort channelId, byte[] data, bool reliable)
    {
        if (Host == null)
            throw new InvalidOperationException("The wire has no server to send to.");

        Send(channelId, data, Host);
    }

    /// <inheritdoc />
    /// <remarks>Adds none: the peers on the wire are machines, with no player standing anywhere in a world.</remarks>
    public void GetPlayers(List<IMyPlayer> players)
    {
    }
}
