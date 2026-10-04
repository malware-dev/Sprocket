using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Features.Doors;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Tests;

/// <summary>
/// A door set to close by itself closes that long after it finishes opening: left alone where somebody closes it first,
/// and counting afresh where it is opened again. The slider that sets it runs as the timer block's delay does, with off
/// at its left end.
/// </summary>
public class AutoCloseTests
{
    /// <summary>The ticks a door on the rig takes to open, transcribed from the harness door.</summary>
    const int Travel = 45;

    readonly Rigged _rigged = new Rigged();
    readonly Rota _rota = new Rota();
    readonly Door _door;
    int _tick;

    public AutoCloseTests()
    {
        _door = _rigged.Door(50, "Hall Door");
        _door.Journal = new List<string>();
    }

    IMyDoor Block => (IMyDoor)_door.Block;

    /// <summary>How many times the door has been told to close, by hand or by itself.</summary>
    int Closes => _door.Journal.Count(line => line == "Hall Door:close");

    /// <summary>Runs the door and the rota on, in the order the game runs a block and then the session.</summary>
    void Run(int ticks)
    {
        for (var i = 0; i < ticks; i++)
        {
            _tick++;
            _door.Tick();
            _rota.Run(_tick);
        }
    }

    Closer Closer(int seconds)
    {
        var closer = new Closer(Block, _rota, () => _tick);
        closer.Set(seconds);
        return closer;
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

    /// <summary>A shut door with a delay puts nothing on the rota, and nothing stays there once it has closed.</summary>
    [Fact]
    public void NothingWaitsWhileTheDoorIsShut()
    {
        using (Closer(1))
        {
            Assert.Equal(0, _rota.Count);

            _door.Perform("Open_On");
            Run(Travel);
            Assert.Equal(1, _rota.Count);

            Run(CloseDelay.TicksPerSecond + Travel);
            Assert.Equal(0, _rota.Count);
        }
    }

    [Fact]
    public void AClosedCloserLetsTheDoorBe()
    {
        Closer(1).Dispose();

        _door.Perform("Open_On");
        Run(Travel + 10 * CloseDelay.TicksPerSecond);

        Assert.Equal(0, Closes);
        Assert.Equal(0, _rota.Count);
    }

    /// <summary>On the server, a door coming alive with a delay closes by it, and setting off lets it be.</summary>
    [Fact]
    public void TheServerClosesDoorsByWhatTheyKeep()
    {
        using (var doors = new AutoClose(_rota, () => _tick))
        {
            CloseDelay.Store(_door.Block, 1);
            doors.Arrived(Block);

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

    /// <summary>A door gone from the world stops being closed.</summary>
    [Fact]
    public void ADoorThatLeftIsLetBe()
    {
        using (var doors = new AutoClose(_rota, () => _tick))
        {
            doors.Set(Block, 1);
            doors.Left(Block);

            _door.Perform("Open_On");
            Run(Travel + 10 * CloseDelay.TicksPerSecond);
            Assert.Equal(0, Closes);
        }
    }
}
