using Sprocket.Host.SpaceEngineers.LcdApps.StatusPanel;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using Xunit;

namespace Sprocket.Tests;

/// <summary>
/// The construct's automation as the status panel says it: the installations that want looking at, most wanting first,
/// and how many logic systems need nothing.
/// </summary>
public class ConstructStatusTests
{
    sealed class Ship
    {
        public Rigged Rig = new();
        public Automation Program = new();
        public AutomationRunner Runner = null!;

        public GridWorld World => Rig.World;

        public string Ref(long id) => Program.Declare(World, World.FindEntity(id)!).Name;

        int _frame;

        public void Tick(int ticks = 1)
        {
            for (var i = 0; i < ticks; i++)
            {
                Rig.Tick();
                Runner.Tick(++_frame);
            }
        }
    }

    static Ship Build()
    {
        var ship = new Ship();
        ship.Rig.Door(1, "Inner Door");
        ship.Rig.Door(2, "Outer Door");
        ship.Rig.Panel(3, "Airlock Button");

        ship.Runner = new AutomationRunner(ship.Program, ship.World);
        return ship;
    }

    /// <summary>Adds a switched-on system with one sequence that a door opening starts.</summary>
    static AutomationSystem System(Ship ship, string name, long watches, long acts)
    {
        var system = ship.Program.AddSystem();
        system.Name = name;
        system.Enabled = true;

        var opened = SignalTrigger.Written("ext", "finishes-opening");
        opened.Sources.Add(ship.Ref(watches));

        var sequence = ship.Program.AddSequence(system, opened);
        sequence.Name = name + " cycle";

        var act = new Act("close");
        act.Targets.Add(ship.Ref(acts));
        sequence.Steps.Add(act);
        sequence.Steps.Add(new Await());

        return system;
    }

    /// <summary>Adds a sequence to <paramref name="system" /> that nothing starts, which is a warning.</summary>
    static void Spare(Ship ship, AutomationSystem system)
    {
        var spare = ship.Program.AddSequence(system);
        spare.Name = "Seal up";
        var shut = new Act("close");
        shut.Targets.Add(ship.Ref(2));
        spare.Steps.Add(shut);
        spare.Steps.Add(new Await());
    }

    static ConstructStatus Look(Ship ship, ConstructStatus? status = null, int now = 0)
    {
        status ??= new ConstructStatus();
        status.Begin(now);
        status.Include(ship.Program, ship.Runner);
        status.End("Miner");
        return status;
    }

    /// <summary>A construct whose automation is working wants nothing looked at, and counts every logic system as normal.</summary>
    [Fact]
    public void WorkingAutomationWantsNothingLookedAt()
    {
        var ship = Build();
        System(ship, "Airlock", 1, 2);
        System(ship, "Hangar", 2, 1);
        ship.Tick(3);

        var status = Look(ship);

        Assert.True(status.Heard);
        Assert.Equal("Miner", status.Construct);
        Assert.Empty(status.Items);
        Assert.Equal(2, status.Systems);
        Assert.Equal(2, status.Normal);
    }

    /// <summary>Nothing is said of a construct whose automation has not been heard from.</summary>
    [Fact]
    public void NothingIsSaidBeforeItIsHeard()
    {
        var status = new ConstructStatus();
        status.Unheard("Miner");

        Assert.False(status.Heard);
        Assert.Empty(status.Items);
        Assert.Equal("Miner", status.Construct);
    }

    /// <summary>A lost block is a problem and a sequence nothing starts is a warning, and the problem comes first.</summary>
    [Fact]
    public void AProblemComesBeforeAWarning()
    {
        var ship = Build();
        AutomationSystem airlock = System(ship, "Airlock", 2, 2);
        AutomationSystem hangar = System(ship, "Hangar", 1, 1);
        Spare(ship, airlock);
        ship.Tick(3);

        ship.Rig.Grind(1);
        ship.Tick(3);
        ship.Runner.Look();

        var status = Look(ship);

        Assert.Equal(2, status.Items.Count);
        Assert.Equal(StatusLevel.Problem, status.Items[0].Level);
        Assert.Equal(1, status.Problems);
        Assert.Equal(0, status.Normal);
        Assert.Contains(status.Items, item => item.System == hangar);
        Assert.Contains(status.Items, item => item.System == airlock);
    }

    /// <summary>An installation of a blueprint is named on its own, and one switched off comes after it.</summary>
    [Fact]
    public void AnInstallationIsNamedOnItsOwn()
    {
        var ship = Build();
        AutomationSystem airlock = System(ship, "Airlock", 1, 2);
        airlock.IsBlueprint = true;
        airlock.AddInstance();
        airlock.AddInstance();
        Spare(ship, airlock);
        ship.Tick(3);

        airlock.Instances[1].Enabled = false;
        ship.Tick();

        var status = Look(ship);

        Assert.Equal(2, status.Items.Count);
        Assert.Same(airlock.Instances[0], status.Items[0].Instance);
        Assert.Equal(StatusLevel.Warning, status.Items[0].Level);
        Assert.Same(airlock.Instances[1], status.Items[1].Instance);
        Assert.Equal(StatusLevel.Off, status.Items[1].Level);
    }

    /// <summary>A logic system switched off because it could not work is a problem, and says why.</summary>
    [Fact]
    public void AHaltedSystemIsAProblem()
    {
        var ship = Build();
        AutomationSystem airlock = System(ship, "Airlock", 1, 2);
        ship.Tick(3);

        airlock.Halt = new Halt(SequenceProblem.MissingTarget, 1, 0, BlockTrouble.None, "Outer Door");
        airlock.Enabled = false;

        var status = Look(ship);

        Assert.Single(status.Items);
        Assert.Equal(StatusLevel.Problem, status.Items[0].Level);
        Assert.Same(airlock.Halt, status.Items[0].Halt);
        Assert.Equal(1, status.Problems);
    }

    /// <summary>A logic system its player switched off is a warning, after everything else.</summary>
    [Fact]
    public void ASystemSwitchedOffByHandWantsLookingAt()
    {
        var ship = Build();
        AutomationSystem airlock = System(ship, "Airlock", 1, 2);
        Spare(ship, airlock);
        ship.Tick(3);

        airlock.Enabled = false;

        var status = Look(ship);

        Assert.Single(status.Items);
        Assert.Equal(StatusLevel.Off, status.Items[0].Level);
        Assert.True(status.Troubled);
        Assert.Equal(1, status.Warnings);
        Assert.Equal(0, status.Normal);
    }

    /// <summary>The newest of one level comes first, and an item keeps the tick it was first seen.</summary>
    [Fact]
    public void TheNewestOfALevelComesFirst()
    {
        var ship = Build();
        AutomationSystem airlock = System(ship, "Airlock", 1, 2);
        Spare(ship, airlock);
        ship.Tick(3);

        var status = Look(ship, now: 10);

        AutomationSystem hangar = System(ship, "Hangar", 2, 1);
        Spare(ship, hangar);
        ship.Tick(3);
        Look(ship, status, 20);

        Assert.Equal(2, status.Items.Count);
        Assert.Same(hangar, status.Items[0].System);
        Assert.Equal(20, status.Items[0].Since);
        Assert.Same(airlock, status.Items[1].System);
        Assert.Equal(10, status.Items[1].Since);
    }

    /// <summary>Looking again at automation that has not changed leaves the revision, so no screen draws itself again.</summary>
    [Fact]
    public void NothingChangedLeavesTheRevision()
    {
        var ship = Build();
        AutomationSystem airlock = System(ship, "Airlock", 1, 2);
        Spare(ship, airlock);
        ship.Tick(3);

        var status = Look(ship, now: 1);
        var revision = status.Revision;
        Look(ship, status, 2);

        Assert.Equal(revision, status.Revision);
        Assert.Equal(1, status.Items[0].Since);
    }

    /// <summary>Two programs on one construct make one status.</summary>
    [Fact]
    public void EveryProgramOnTheConstructAddsToOneStatus()
    {
        var ship = Build();
        System(ship, "Airlock", 1, 2);

        var second = new Automation();
        var runner = new AutomationRunner(second, ship.World);
        var rig = second.AddSystem();
        rig.Name = "Drill Rig";
        rig.Enabled = true;
        second.AddSequence(rig, new EveryTrigger(600));
        ship.Tick(3);

        var status = new ConstructStatus();
        status.Begin(0);
        status.Include(ship.Program, ship.Runner);
        status.Include(second, runner);
        status.End("Miner");

        Assert.Equal(2, status.Systems);
    }
}
