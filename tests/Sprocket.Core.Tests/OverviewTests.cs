using Sprocket.Host.SpaceEngineers.LcdApps.StatusPanel;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Tests;

/// <summary>
/// The construct's automation as a panel says it, one line per system.
/// </summary>
/// <remarks>
/// A glance rather than a look: the line has to be right about the whole system while saying one
/// thing about it, and the totals have to be right about the whole construct. The words are the
/// editor's own, so what is tested here is which state a row picks and what it counts.
/// </remarks>
public class OverviewTests
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

    /// <summary>Adds a system with one sequence that a door opening starts.</summary>
    static AutomationSystem System(Ship ship, string name, long watches, long acts)
    {
        var system = ship.Program.AddSystem();
        system.Name = name;

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

    [Fact]
    public void OneLinePerSystemSayingWhatItIsDoing()
    {
        var ship = Build();
        System(ship, "Airlock", 1, 2);
        AutomationSystem hangar = System(ship, "Hangar", 2, 1);
        hangar.IsBlueprint = true;
        hangar.AddInstance();
        hangar.AddInstance();

        ship.Tick(3);

        var overview = new Overview();
        overview.Fill(ship.World, ship.Program, ship.Runner);

        Assert.Equal(2, overview.Rows.Count);
        Assert.Equal("Airlock", overview.Rows[0].Name);
        Assert.Equal("Hangar", overview.Rows[1].Name);

        // Two of the hangar fitted, which is what the row says instead of listing them.
        Assert.Equal(1, overview.Rows[0].Instances);
        Assert.Equal(2, overview.Rows[1].Instances);

        // Nothing has happened yet, and nothing is wrong.
        Assert.Equal(SequenceStatus.NeverRun, overview.Rows[0].Status);
        Assert.Equal(0, overview.Running);
        Assert.Equal(0, overview.Problems);
        Assert.Equal(0, overview.Warnings);
    }

    /// <summary>
    /// A system with something happening in it reads as running.
    /// </summary>
    /// <remarks>
    /// One row for a system that may hold several sequences, so the row shows the most telling of
    /// them: a ship where one rule is mid-cycle and three are waiting is a ship that is doing
    /// something, and a row saying "waiting" would be true of the wrong part of it.
    /// </remarks>
    [Fact]
    public void ASystemWithSomethingHappeningReadsAsRunning()
    {
        var ship = Build();
        System(ship, "Airlock", 1, 2);

        // The door it closes has to be open, or closing it finishes in the tick it started and the
        // sequence is over before anybody could glance at it.
        Door(ship, 2).ForceOpen();
        ship.Tick(Rota.Ten);

        Door(ship, 1).ForceOpen();
        ship.Tick(Rota.Ten);

        var overview = new Overview();
        overview.Fill(ship.World, ship.Program, ship.Runner);

        Assert.True(overview.Rows[0].Status == SequenceStatus.Running
            || overview.Rows[0].Status == SequenceStatus.RunningWaiting);
        Assert.Equal(1, overview.Running);
    }

    /// <summary>A system with every one of it switched off says so, and is not counted as anything else.</summary>
    [Fact]
    public void AParkedSystemSaysSo()
    {
        var ship = Build();
        AutomationSystem airlock = System(ship, "Airlock", 1, 2);
        airlock.IsBlueprint = true;
        airlock.AddInstance();
        airlock.AddInstance();
        ship.Tick(3);

        foreach (var instance in airlock.Instances)

            instance.Enabled = false;
        ship.Tick();

        var overview = new Overview();
        overview.Fill(ship.World, ship.Program, ship.Runner);

        Assert.True(overview.Rows[0].Parked);
        Assert.Equal(0, overview.Running);
        Assert.Equal(0, overview.Problems);
    }

    /// <summary>
    /// A broken rule counts as a problem and a remark counts as a warning, on the row and in the total.
    /// </summary>
    /// <remarks>
    /// The two are counted apart because the panel says them apart: a problem is red and means
    /// something is wired to a block that is gone, a warning is amber and means nobody has given a
    /// rule anything to start it yet.
    /// </remarks>
    [Fact]
    public void ProblemsAndWarningsAreCountedApart()
    {
        var ship = Build();
        AutomationSystem airlock = System(ship, "Airlock", 1, 2);

        // Nothing starts this one and nothing calls it, which is the warning.
        var spare = ship.Program.AddSequence(airlock);
        spare.Name = "Seal up";
        var shut = new Act("close");
        shut.Targets.Add(ship.Ref(2));
        spare.Steps.Add(shut);
        spare.Steps.Add(new Await());

        ship.Tick(3);

        var overview = new Overview();
        overview.Fill(ship.World, ship.Program, ship.Runner);

        Assert.Equal(0, overview.Problems);
        Assert.Equal(1, overview.Warnings);
        Assert.Equal(1, overview.Rows[0].Warnings);

        // And a rule whose blocks are gone is the other kind, once something has run into it or an open
        // editor has looked.
        ship.Rig.Grind(2);
        ship.Tick(3);
        ship.Runner.Look();

        overview.Fill(ship.World, ship.Program, ship.Runner);

        Assert.True(overview.Problems > 0);
        Assert.True(overview.Rows[0].Problems > 0);
    }

    /// <summary>
    /// Two Sprockets on one construct make one picture.
    /// </summary>
    /// <remarks>
    /// Somebody looking at a panel wants the ship, not whichever block the panel sits nearest, so the
    /// rows and the totals are the construct's and every Sprocket adds to them.
    /// </remarks>
    [Fact]
    public void EverySprocketOnTheConstructAddsToOnePicture()
    {
        var ship = Build();
        System(ship, "Airlock", 1, 2);

        var second = new Automation();
        var runner = new AutomationRunner(second, ship.World);
        var rig = second.AddSystem();
        rig.Name = "Drill Rig";
        second.AddSequence(rig, new EveryTrigger(600));

        var overview = new Overview();
        overview.Clear();
        overview.Include(ship.World, ship.Program, ship.Runner);
        overview.Include(ship.World, second, runner);

        Assert.Equal(2, overview.Rows.Count);
        Assert.Equal("Airlock", overview.Rows[0].Name);
        Assert.Equal("Drill Rig", overview.Rows[1].Name);
    }

    static Door Door(Ship ship, long id) =>
        (Door)ship.Rig.Rig.Fitted.First(f => f.Id == id);
}
