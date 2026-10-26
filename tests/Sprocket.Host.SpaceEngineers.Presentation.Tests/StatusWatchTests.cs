using Sprocket.Host.SpaceEngineers.LcdApps.StatusPanel;
using Sprocket.Host.SpaceEngineers.Network;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using Mal.MdkModMixin.Network;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// A client's status panel hears how a construct's automation stands from the server: it asks once, is answered at once,
/// hears every change and nothing else, and lets go once no panel has shown it for five seconds.
/// </summary>
public class StatusWatchTests : IDisposable
{
    const long Barge = 4711;

    readonly Wire _wire = new Wire();
    readonly Peer _server;
    readonly Peer _anna;
    readonly Rota _serverRota = new Rota();
    readonly Rota _annaRota = new Rota();
    readonly StatusWatch _annas;
    readonly Simulation _sim;
    readonly AutomationSystem _airlock;
    readonly List<ulong> _connected = new List<ulong>();
    int _tick;
    int _redrawn;

    public StatusWatchTests()
    {
        _sim = Simulation.On(new Rig());
        _sim.Machine.Door(2, "Inner Door");
        _sim.Tick();
        _airlock = _sim.Unit.Program.AddSystem();
        _airlock.Name = "Airlock";
        _airlock.Enabled = true;
        _sim.Tick();

        _wire.Install();
        _server = _wire.Server();
        _anna = _wire.Client("Anna");
        _connected.Add(_anna.Id);
        Watch(_server, _serverRota);
        _annas = Watch(_anna, _annaRota);
    }

    public void Dispose()
    {
        _wire.Uninstall();
    }

    StatusWatch Watch(Peer peer, Rota rota)
    {
        StatusWatch made = null!;
        _wire.As(peer, () =>
        {
            made = new StatusWatch(rota, () => _tick, (construct, into) =>
            {
                into.Clear();
                if (construct != Barge)
                    return null;

                into.Add(_sim.Unit);
                return "Mining Barge";
            }, client => _connected.Contains(client));
            made.Listen(StatusWatch.Carry(NetworkChannel.Create("Sprocket.StatusWatchTests")).Build());
        });

        return made;
    }

    ConstructStatus Watching()
    {
        ConstructStatus status = null!;
        _wire.As(_anna, () => status = _annas.Watch(Barge, () => _redrawn++));
        _wire.Settle();
        return status;
    }

    /// <summary>Runs both machines' rotas for <paramref name="ticks" /> ticks, delivering what is sent.</summary>
    void Run(int ticks)
    {
        for (var i = 0; i < ticks; i++)
        {
            _tick++;
            _wire.As(_server, () => _serverRota.Run(_tick));
            _wire.As(_anna, () => _annaRota.Run(_tick));
            _wire.Settle();
        }
    }

    /// <summary>A client asking about a construct is told at once how it stands, and the panel showing it draws.</summary>
    [Fact]
    public void AClientIsToldAtOnce()
    {
        var status = Watching();

        Assert.True(status.Heard);
        Assert.Equal("Mining Barge", status.Construct);
        Assert.Equal(1, status.Systems);
        Assert.Equal(1, _redrawn);
    }

    /// <summary>A change on the server reaches a watching client at the next look, worded from what was sent.</summary>
    [Fact]
    public void AChangeReachesTheClient()
    {
        var status = Watching();
        _airlock.Enabled = false;
        _sim.Tick();

        Run(Rota.Hundred);

        Assert.Single(status.Items);
        Assert.Equal(StatusLevel.Off, status.Items[0].Level);
        Assert.Equal("Airlock", status.Items[0].Logic);
        Assert.Equal(2, _redrawn);
    }

    /// <summary>Automation that does not change sends nothing, however long the client watches.</summary>
    [Fact]
    public void NothingChangedSendsNothing()
    {
        Watching();
        var delivered = _wire.Delivered;

        for (var i = 0; i < 2; i++)
        {
            _wire.As(_anna, () => _annas.Watch(Barge, () => _redrawn++));
            Run(Rota.Hundred);
        }

        Assert.Equal(delivered, _wire.Delivered);
    }

    /// <summary>A construct no panel has shown for five seconds is let go, and the server stops telling the client of it.</summary>
    [Fact]
    public void AnUnseenConstructIsLetGo()
    {
        var status = Watching();
        Run(StatusWatch.Unseen + Rota.Hundred);

        _airlock.Enabled = false;
        _sim.Tick();
        Run(Rota.Hundred);

        Assert.Empty(status.Items);
        Assert.Equal(0, _serverRota.Count);
        Assert.Equal(0, _annaRota.Count);
    }

    /// <summary>A client that has gone is told nothing more.</summary>
    [Fact]
    public void AGoneClientIsForgotten()
    {
        Watching();
        _connected.Clear();
        _airlock.Enabled = false;
        _sim.Tick();
        var delivered = _wire.Delivered;

        Run(Rota.Hundred);

        Assert.Equal(delivered, _wire.Delivered);
        Assert.Equal(0, _serverRota.Count);
    }

    /// <summary>What a report carries comes out as it went in, a block's trouble and an unstarted sequence's reason included.</summary>
    [Fact]
    public void AReportReadsBackAsItWasWritten()
    {
        var report = new StatusReport
        {
            Construct = Barge,
            Name = "Mining Barge",
            Systems = 5,
            Normal = 2,
            Items =
            [
                new StatusItem
                {
                    Level = StatusLevel.Problem, Logic = "Airlock", Number = 3, Blueprint = true, Installation = "Port", Position = 2,
                    Sequence = "Cycle", Row = 4, Halted = true,
                    Fault = new SequenceFault(SequenceProblem.MissingTarget, 3, BlockTrouble.Kept(Cause.VentNoOxygen, "Oxygen"), "Outer Door")
                },
                new StatusItem
                {
                    Level = StatusLevel.Warning, Logic = "", Number = 4, Installation = "", Sequence = "", Alone = true,
                    Fault = SequenceFault.Unstarted(Unwired.Cleared, "Button Panel")
                }
            ]
        };

        var data = new byte[StatusReport.SizeOf(ref report)];
        var offset = 0;
        StatusReport.Write(ref report, data, ref offset);
        offset = 0;
        StatusReport.Read(data, ref offset, out var read);

        Assert.Equal(data.Length, offset);
        Assert.Equal(report.Name, read.Name);
        Assert.Equal(report.Systems, read.Systems);
        Assert.Equal(report.Normal, read.Normal);
        Assert.Equal(2, read.Items.Length);
        Assert.True(read.Items[0].Same(report.Items[0]));
        Assert.True(read.Items[1].Same(report.Items[1]));
    }
}
