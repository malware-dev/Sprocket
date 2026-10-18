extern alias DoorMod;
using DoorMod::Sprocket.DoorAutoClose;
using Sandbox.ModAPI;
using Sprocket.Sim.Fakes;
using Sprocket.Sim.Machine;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using Xunit;
using DoorNetwork = DoorMod::Sprocket.Network;

namespace Sprocket.DoorAutoClose.Tests;

/// <summary>
/// A delay set on a client is kept in its own copy at once and sent to the server, which keeps it where the player may
/// change the door and tells every client; a client takes what the server tells it.
/// </summary>
public class AutoCloseNetworkTests : IDisposable
{
    /// <summary>The player who sets the delay, and their identity in the world.</summary>
    const ulong Player = 76561197970740585UL;
    const long PlayerIdentity = 144115188075855901L;

    readonly Rig _rig = new Rig();
    readonly Door _door;
    readonly Network _network = new Network();
    readonly IMyEntities _entities;
    readonly IMyPlayerCollection _players;

    public AutoCloseNetworkTests()
    {
        _rig.Install();
        _door = _rig.Door(50, "Hall Door");
        _entities = MyAPIGateway.Entities;
        _players = MyAPIGateway.Players;
        MyAPIGateway.Entities = Pretend.To<IMyEntities>()
            .Does("GetEntityById", args => (long)args[0] == _door.Id ? _door.Block : null!)
            .Done();
        MyAPIGateway.Players = Pretend.To<IMyPlayerCollection>()
            .Does("TryGetIdentityId", args => (ulong)args[0] == Player ? PlayerIdentity : 0L)
            .Done();
        DoorNetwork.NetworkChannel.Network = _network;
    }

    public void Dispose()
    {
        DoorNetwork.NetworkChannel.Network = null;
        MyAPIGateway.Entities = _entities;
        MyAPIGateway.Players = _players;
    }

    IMyDoor Block => (IMyDoor)_door.Block;

    /// <summary>Auto-close on a channel of its own, as one machine's session builds it.</summary>
    AutoClose Listening()
    {
        var doors = new AutoClose(_ => null);
        doors.Listen(AutoClose.Carry(DoorNetwork.NetworkChannel.Create("door-auto-close-test")).Build());
        return doors;
    }

    [Fact]
    public void AClientKeepsTheDelayAndAsksTheServer()
    {
        _network.IsServer = false;
        Listening().Set(Block, 5);

        Assert.Equal(5, CloseDelay.Stored(_door.Block));
        Assert.Single(_network.ToServer);
        Assert.Empty(_network.ToOthers);
    }

    [Fact]
    public void TheServerKeepsADelayFromAPlayerWhoMayChangeTheDoor()
    {
        _network.IsServer = false;
        Listening().Set(Block, 5);
        var asked = _network.ToServer[0];
        CloseDelay.Store(_door.Block, 0);

        _network.IsServer = true;
        _network.Deliver(asked, Player, false);

        Assert.Equal(5, CloseDelay.Stored(_door.Block));
        Assert.Single(_network.ToOthers);
    }

    [Fact]
    public void TheServerIgnoresADelayFromAPlayerWhoMayNot()
    {
        _door.Owner = 7;
        _network.IsServer = false;
        Listening().Set(Block, 5);
        var asked = _network.ToServer[0];
        CloseDelay.Store(_door.Block, 0);

        _network.IsServer = true;
        _network.Deliver(asked, Player, false);

        Assert.Equal(0, CloseDelay.Stored(_door.Block));
        Assert.Empty(_network.ToOthers);
    }

    [Fact]
    public void AClientTakesWhatTheServerTellsIt()
    {
        _network.IsServer = true;
        Listening().Set(Block, 30);
        var told = _network.ToOthers[0];
        CloseDelay.Store(_door.Block, 0);

        _network.IsServer = false;
        _network.Deliver(told, 0, true);

        Assert.Equal(30, CloseDelay.Stored(_door.Block));
    }

    /// <summary>The game's messaging, with what is sent kept and handed back on request.</summary>
    sealed class Network : DoorNetwork.INetworkProxy
    {
        readonly List<Action<ushort, byte[], ulong, bool>> _handlers = new();

        public bool IsServer { get; set; }

        public bool IsHeadlessServer => false;

        public bool IsClient => !IsServer;

        /// <summary>Each message sent to the server, as the channel and its bytes.</summary>
        public List<(ushort Channel, byte[] Data)> ToServer { get; } = new();

        /// <summary>Each message sent to everybody else, as the channel and its bytes.</summary>
        public List<(ushort Channel, byte[] Data)> ToOthers { get; } = new();

        public void RegisterSecureMessageHandler(ushort id, Action<ushort, byte[], ulong, bool> messageHandler)
        {
            _handlers.Add(messageHandler);
        }

        public void UnregisterSecureMessageHandler(ushort id, Action<ushort, byte[], ulong, bool> messageHandler)
        {
            _handlers.Remove(messageHandler);
        }

        public void SendMessageTo(ushort channelId, byte[] data, ulong recipientId, bool reliable)
        {
            ToOthers.Add((channelId, data));
        }

        public void SendMessageToOthers(ushort channelId, byte[] data, bool reliable)
        {
            ToOthers.Add((channelId, data));
        }

        public void SendMessageToServer(ushort channelId, byte[] data, bool reliable)
        {
            ToServer.Add((channelId, data));
        }

        /// <summary>Hands <paramref name="message" /> to every listener, as arriving from <paramref name="sender" />.</summary>
        public void Deliver((ushort Channel, byte[] Data) message, ulong sender, bool fromServer)
        {
            foreach (var handler in _handlers.ToList())
                handler(message.Channel, message.Data, sender, fromServer);
        }
    }
}
