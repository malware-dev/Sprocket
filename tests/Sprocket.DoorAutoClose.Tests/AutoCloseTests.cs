extern alias DoorMod;
using DoorMod::Sprocket.DoorAutoClose;
using Sandbox.ModAPI;
using Sprocket.Sim.Machine;
using Xunit;
using DoorNetwork = DoorMod::Sprocket.Network;

namespace Sprocket.DoorAutoClose.Tests;

/// <summary>
/// A door set to close by itself closes that long after it finishes opening: left alone where somebody closes it first,
/// and counting afresh where it is opened again. The slider that sets it runs as the timer block's delay does, with off
/// at its left end.
/// </summary>
public class AutoCloseTests : IDisposable
{
    /// <summary>The ticks a door on the rig takes to open, transcribed from the harness door.</summary>
    const int Travel = 45;

    readonly Rig _rig = new Rig();
    readonly Door _door;
    DoorCloser? _closer;
    int _tick;
    int _changes;

    public AutoCloseTests()
    {
        _rig.Install();
        _door = _rig.Door(50, "Hall Door");
        _door.Journal = new List<string>();
        DoorNetwork.NetworkChannel.Network = new Server();
    }

    public void Dispose()
    {
        DoorNetwork.NetworkChannel.Network = null;
    }

    IMyDoor Block => (IMyDoor)_door.Block;

    /// <summary>How many times the door has been told to close, by hand or by itself.</summary>
    int Closes => _door.Journal.Count(line => line == "Hall Door:close");

    /// <summary>Runs the door and its closer on, the closer asked on every tick.</summary>
    void Run(int ticks)
    {
        for (var i = 0; i < ticks; i++)
        {
            _tick++;
            _door.Tick();
            _closer?.Check();
        }
    }

    /// <summary>A closer for the door, which keeps a delay of <paramref name="seconds" />.</summary>
    DoorCloser Closer(int seconds)
    {
        CloseDelay.Store(_door.Block, seconds);
        _closer = new DoorCloser(Block, () => _tick, () => _changes++);
        _closer.Set(seconds);
        return _closer;
    }

    [Theory]
    [InlineData(0.5f, 0)]
    [InlineData(0.99f, 0)]
    [InlineData(1f, 1)]
    [InlineData(59.9f, 59)]
    [InlineData(599.9999f, 600)]
    [InlineData(5000f, 600)]
    public void TheSliderCountsWholeSecondsWithOffBelowOne(float value, int seconds)
    {
        Assert.Equal(seconds, CloseDelay.FromSlider(value));
    }

    [Theory]
    [InlineData(0, "Off")]
    [InlineData(10, "00:10")]
    [InlineData(61, "01:01")]
    [InlineData(600, "10:00")]
    public void TheDelayIsWrittenInMinutesAndSeconds(int seconds, string written)
    {
        Assert.Equal(written, CloseDelay.Written(seconds, "Off"));
    }

    /// <summary>A delay is kept in the door's storage, and off takes it away again.</summary>
    [Fact]
    public void TheDoorKeepsItsDelay()
    {
        CloseDelay.Store(_door.Block, 30);
        Assert.Equal(30, CloseDelay.Stored(_door.Block));

        CloseDelay.Store(_door.Block, 0);
        Assert.Equal(0, CloseDelay.Stored(_door.Block));
        Assert.Empty(_door.Storage);
    }

    [Fact]
    public void ADoorClosesTheSetTimeAfterItFinishesOpening()
    {
        using (Closer(2))
        {
            _door.Perform("Open_On");
            Run(Travel + 2 * CloseDelay.TicksPerSecond - 1);
            Assert.Equal(0, Closes);

            Run(1);
            Assert.Equal(1, Closes);
        }
    }

    [Fact]
    public void ADoorClosedByHandIsLeftAlone()
    {
        using (var closer = Closer(2))
        {
            _door.Perform("Open_On");
            Run(Travel + 60);
            _door.Perform("Open_Off");
            Run(Travel);

            Assert.False(closer.Waiting);
            Run(10 * CloseDelay.TicksPerSecond);
            Assert.Equal(1, Closes);
        }
    }

    /// <summary>Closed partway and opened again before it shut, the door counts from when it is open the second time.</summary>
    [Fact]
    public void ADoorOpenedAgainCountsAfresh()
    {
        using (Closer(2))
        {
            _door.Perform("Open_On");
            Run(Travel + 100);
            _door.Perform("Open_Off");
            Run(10);
            _door.Perform("Open_On");
            Run(10);

            var reopened = _tick;
            Run(2 * CloseDelay.TicksPerSecond - 1);
            Assert.Equal(1, Closes);

            Run(1);
            Assert.Equal(reopened + 2 * CloseDelay.TicksPerSecond, _tick);
            Assert.Equal(2, Closes);
        }
    }

    /// <summary>Set on a door standing open, the delay counts from the moment it was set.</summary>
    [Fact]
    public void SetOnAnOpenDoorItCountsFromNow()
    {
        _door.ForceOpen();
        Run(30);

        using (Closer(1))
        {
            Run(CloseDelay.TicksPerSecond - 1);
            Assert.Equal(0, Closes);

            Run(1);
            Assert.Equal(1, Closes);
        }
    }

    /// <summary>
    /// A shut door with a delay waits for nothing, so its block asks for no updates; it waits only from finishing opening to
    /// closing.
    /// </summary>
    [Fact]
    public void NothingWaitsWhileTheDoorIsShut()
    {
        using (var closer = Closer(1))
        {
            Assert.False(closer.Waiting);
            Assert.Equal(0, _changes);

            _door.Perform("Open_On");
            Run(Travel);
            Assert.True(closer.Waiting);
            Assert.Equal(1, _changes);

            Run(CloseDelay.TicksPerSecond + Travel);
            Assert.False(closer.Waiting);
            Assert.Equal(2, _changes);
        }
    }

    [Fact]
    public void AClosedCloserLetsTheDoorBe()
    {
        var closer = Closer(1);
        closer.Dispose();
        _closer = null;

        _door.Perform("Open_On");
        Run(Travel + 10 * CloseDelay.TicksPerSecond);

        Assert.Equal(0, Closes);
        Assert.False(closer.Waiting);
    }

    /// <summary>On the server, a delay set closes the door by it, and setting off lets it be.</summary>
    [Fact]
    public void TheServerClosesDoorsByWhatTheyKeep()
    {
        using (Closer(0))
        {
            var doors = new AutoClose(_ => _closer);
            doors.Set(Block, 1);
            Assert.Equal(1, CloseDelay.Stored(_door.Block));

            _door.Perform("Open_On");
            Run(Travel + CloseDelay.TicksPerSecond);
            Assert.Equal(1, Closes);

            Run(Travel);
            doors.Set(Block, 0);
            Assert.Equal(0, CloseDelay.Stored(_door.Block));

            _door.Perform("Open_On");
            Run(Travel + 10 * CloseDelay.TicksPerSecond);
            Assert.Equal(1, Closes);
        }
    }

    /// <summary>Set while the door stands open, a delay counts from the moment it was set.</summary>
    [Fact]
    public void SetOnTheServerWhileOpenItCountsFromNow()
    {
        using (Closer(0))
        {
            _door.ForceOpen();
            Run(30);

            new AutoClose(_ => _closer).Set(Block, 1);
            Run(CloseDelay.TicksPerSecond - 1);
            Assert.Equal(0, Closes);

            Run(1);
            Assert.Equal(1, Closes);
        }
    }

    /// <summary>A door with no closer, which is any door before it is alive, still keeps the delay it is given.</summary>
    [Fact]
    public void ADoorWithNoCloserKeepsItsDelay()
    {
        new AutoClose(_ => null).Set(Block, 5);
        Assert.Equal(5, CloseDelay.Stored(_door.Block));
    }

    /// <summary>A door saved while its close was coming closes on the tick it would have, once the world is loaded again.</summary>
    [Fact]
    public void ADoorSavedWhileCountingClosesWhenItWould()
    {
        int due;
        using (var closer = Closer(2))
        {
            _door.Perform("Open_On");
            Run(Travel + 30);
            due = closer.ClosesAt;
            CloseDue.Store(_door.Block, due - _tick);
        }

        Assert.Equal(due - _tick, CloseDue.Take(_door.Block));
        Assert.Equal(-1, CloseDue.Take(_door.Block));

        CloseDue.Store(_door.Block, due - _tick);
        _closer = new DoorCloser(Block, () => _tick, () => _changes++);
        using (_closer)
        {
            _closer.Start(_tick + CloseDue.Take(_door.Block));
            Run(due - _tick - 1);
            Assert.Equal(0, Closes);

            Run(1);
            Assert.Equal(1, Closes);
        }
    }

    /// <summary>A save with no close coming takes the kept one out of the door, leaving its storage as it was.</summary>
    [Fact]
    public void NoCloseComingKeepsNothing()
    {
        CloseDue.Store(_door.Block, 50);
        CloseDue.Store(_door.Block, -1);

        Assert.Equal(-1, CloseDue.Take(_door.Block));
        Assert.Empty(_door.Storage);
    }

    /// <summary>The network as a server with nobody connected sees it.</summary>
    sealed class Server : DoorNetwork.INetworkProxy
    {
        public bool IsHeadlessServer => false;

        public bool IsServer => true;

        public bool IsClient => false;

        public void RegisterSecureMessageHandler(ushort id, Action<ushort, byte[], ulong, bool> messageHandler)
        {
        }

        public void UnregisterSecureMessageHandler(ushort id, Action<ushort, byte[], ulong, bool> messageHandler)
        {
        }

        public void SendMessageTo(ushort channelId, byte[] data, ulong recipientId, bool reliable)
        {
        }

        public void SendMessageToOthers(ushort channelId, byte[] data, bool reliable)
        {
        }

        public void SendMessageToServer(ushort channelId, byte[] data, bool reliable)
        {
        }
    }
}
