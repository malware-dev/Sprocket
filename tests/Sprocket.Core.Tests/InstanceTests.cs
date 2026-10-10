using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Xunit;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Localization;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Machine;

namespace Sprocket.Tests;

/// <summary>
/// Two of a system are two working machines, not one machine listed twice.
/// </summary>
/// <remarks>
/// A system's sequences all point at the things its author built with. Another one of it says
/// "where they used that door, I use this one", and the runtime runs the same sequences once
/// for each, resolving as it goes.
///
/// Before this, one of a system was a name and a number the runtime never saw: two airlocks
/// meant the same sequences running once over the same doors, and the second did nothing at
/// all.
/// </remarks>
public class InstanceTests
{
    sealed class Ship
    {
        public Rigged Rig = new();
        public Automation Program = new();
        public AutomationRunner Runner = null!;
        public AutomationSystem Airlock = null!;

        /// <summary>Names a block, so a test builds sequences the way the editor does.</summary>
        public string Ref(long id) => Program.Declare(World, World.FindEntity(id)!).Name;

        public GridWorld World => Rig.World;

        int _frame;

        public void Tick(int ticks = 1)
        {
            for (var i = 0; i < ticks; i++)
            {
                Rig.Tick();
                Runner.Tick(++_frame);
            }
        }

        /// <summary>
        /// The host starting one of a system, which is what pressing its own button does.
        /// </summary>
        /// <remarks>
        /// A button raises nothing a rule can sample: its slot carries which system, which one of it
        /// and which sequence, and the unit resolves that and calls. So a test that means "the port
        /// button was pressed" says which one of the system ran, and the slot carrying the right key
        /// is the harness's business - see RigTests.
        /// </remarks>
        public void Start(SystemInstance one)
        {
            Runner.Call(Program.Sequences[0], one);
        }
    }

    /// <summary>
    /// One airlock, written against the front doors, then fitted a second time to the port.
    /// </summary>
    static Ship Build()
    {
        var ship = new Ship();

        ship.Rig.Panel(1, "Front Button");
        ship.Rig.Door(2, "Front Inner");
        ship.Rig.Door(3, "Front Outer");

        ship.Rig.Panel(11, "Port Button");
        ship.Rig.Door(12, "Port Inner");
        ship.Rig.Door(13, "Port Outer");

        ship.Airlock = ship.Program.AddSystem();
        ship.Airlock.Name = "Airlock";
        ship.Airlock.IsBlueprint = true;
        ship.Airlock.AddInstance();


        var when = new SignalTrigger(new ToolbarSlot(1, 1, 1).EventId);
        when.Sources.Add(ship.Ref(1));

        var cycle = ship.Program.AddSequence(ship.Airlock, when);
        cycle.Name = "Cycle";

        var close = new Act("close");
        close.Targets.Add(ship.Ref(2));
        cycle.Steps.Add(close);
        cycle.Steps.Add(new Await());

        var open = new Act("open");
        open.Targets.Add(ship.Ref(3));
        cycle.Steps.Add(open);
        cycle.Steps.Add(new Await());

        ship.Runner = new AutomationRunner(ship.Program, ship.World);
        return ship;
    }

    /// <summary>
    /// Points one of the system at the port doors instead of the front ones.
    /// </summary>
    /// <remarks>
    /// Both sides are declarations now: the system was written against one set of claims and this
    /// says which claims stand in for them here.
    /// </remarks>
    static void Fit(Ship ship, SystemInstance instance)
    {
        instance.Fill(ship.Ref(1), new[] { ship.Ref(11) });
        instance.Fill(ship.Ref(2), new[] { ship.Ref(12) });
        instance.Fill(ship.Ref(3), new[] { ship.Ref(13) });
    }

    static Door Door(Ship ship, string name) =>
        ship.Rig.Rig.Fitted.OfType<Door>().First(d => d.Name == name);

    /// <summary>The first of the system, which is the one it was written against.</summary>
    static SystemInstance Front(Ship ship) => ship.Airlock.Instances[0];

    /// <summary>
    /// Calling a sequence names which one of its system to run it on.
    /// </summary>
    /// <remarks>
    /// What a button on the port airlock means, as against a button on the front one. Before this
    /// there was only <c>Call(Sequence)</c>, which runs the first of them - so a toolbar slot could
    /// say which sequence and never which airlock, and a second one of a system could be started
    /// only by its own blocks changing.
    /// </remarks>
    [Fact]
    public void CallingNamesWhichOneOfTheSystemToRun()
    {
        var ship = Build();
        var port = ship.Airlock.AddInstance();
        Fit(ship, port);

        Door(ship, "Front Inner").ForceOpen();
        Door(ship, "Port Inner").ForceOpen();
        ship.Tick(2);
        ship.Rig.Rig.Forget();

        ship.Runner.Call(ship.Program.Sequences[0], port);
        ship.Tick(3);

        // The port one ran, and the front one did not - no button was pressed at all.
        Assert.Contains("Port Inner:close", ship.Rig.Rig.Log);
        Assert.DoesNotContain("Front Inner:close", ship.Rig.Rig.Log);
    }

    /// <summary>
    /// The names a step acts on stand for one set of blocks: a block two slots are answered with is acted on once.
    /// </summary>
    [Fact]
    public void ABlockTwoSlotsReachIsActedOnOnce()
    {
        var ship = Build();
        var close = (Act)ship.Program.Sequences[0].Steps[0];
        close.Targets.Add(ship.Ref(3));

        // The port airlock answers both doors with its inner one.
        var port = ship.Airlock.AddInstance();
        port.Fill(ship.Ref(2), new[] { ship.Ref(12) });
        port.Fill(ship.Ref(3), new[] { ship.Ref(12) });
        ship.Program.Edited();

        Door(ship, "Port Inner").ForceOpen();
        ship.Tick(2);
        ship.Rig.Rig.Forget();

        ship.Start(port);
        ship.Tick(3);

        Assert.Single(ship.Rig.Rig.Log, line => line == "Port Inner:close");
    }

    [Fact]
    public void EachOneOfASystemActsOnItsOwnThings()
    {

        var ship = Build();
        Fit(ship, ship.Airlock.AddInstance());

        Door(ship, "Front Inner").ForceOpen();
        Door(ship, "Port Inner").ForceOpen();
        ship.Tick(2);
        ship.Rig.Rig.Forget();

        ship.Start(ship.Airlock.Instances[1]);
        ship.Tick(3);

        // The port button ran the port airlock, and only the port airlock.
        Assert.Contains("Port Inner:close", ship.Rig.Rig.Log);
        Assert.DoesNotContain("Front Inner:close", ship.Rig.Rig.Log);
    }

    [Fact]
    public void TheFirstOneOfASystemNeedsNoFillingIn()
    {
        var ship = Build();
        Fit(ship, ship.Airlock.AddInstance());

        Door(ship, "Front Inner").ForceOpen();
        ship.Tick(2);
        ship.Rig.Rig.Forget();

        ship.Start(Front(ship));
        ship.Tick(3);

        Assert.Contains("Front Inner:close", ship.Rig.Rig.Log);
        Assert.DoesNotContain("Port Inner:close", ship.Rig.Rig.Log);
    }

    /// <summary>
    /// A gate asks about the doors of the one it is running for.
    /// </summary>
    /// <remarks>
    /// The condition names the author's inner door, exactly as the step does. Left unresolved,
    /// the port airlock would open its outer door the moment the *front* inner door happened
    /// to be shut - true at the wrong moment, and invisibly so.
    /// </remarks>
    [Fact]
    public void AGateAsksAboutTheThingsOfTheOneItIsRunningFor()
    {
        var ship = Build();
        Fit(ship, ship.Airlock.AddInstance());

        // The front inner door is shut; the port one is open and switched off, so it cannot move.
        Door(ship, "Port Inner").ForceOpen();
        Door(ship, "Port Inner").Enabled = false;
        ship.Tick(2);
        ship.Rig.Rig.Forget();

        ship.Start(ship.Airlock.Instances[1]);
        ship.Tick(20);

        // It asked its own door, which cannot close, and stopped there rather than reading the front
        // airlock's shut door and opening onto space.
        var fault = SequenceSummary.Fault(ship.Runner.Get(ship.Program.Sequences[0], ship.Airlock.Instances[1]));
        Assert.Equal(SequenceProblem.Unable, fault.Problem);
        Assert.Equal("Port Inner", fault.Which);
        Assert.DoesNotContain("Port Outer:open", ship.Rig.Rig.Log);
    }

    [Fact]
    public void EachOneOfASystemKeepsItsOwnPlaceInTheSequence()
    {
        var ship = Build();
        Fit(ship, ship.Airlock.AddInstance());

        var cycle = ship.Program.Sequences[0];
        var front = ship.Airlock.Instances[0];
        var port = ship.Airlock.Instances[1];

        Door(ship, "Front Inner").ForceOpen();
        Door(ship, "Port Inner").ForceOpen();
        ship.Tick(2);

        ship.Start(Front(ship));
        ship.Tick(2);

        Assert.True(ship.Runner.Get(cycle, front)!.IsRunning);
        Assert.False(ship.Runner.Get(cycle, port)!.IsRunning);
    }

    /// <summary>
    /// Stopping one of a system stops that one, and leaves the others working.
    /// </summary>
    /// <remarks>
    /// The switch somebody actually reaches for: welding on the port airlock should stop the
    /// port airlock, not both of them and not every rule in the system one at a time.
    /// </remarks>
    [Fact]
    public void StoppingOneOfASystemLeavesTheOthersRunning()
    {
        var ship = Build();
        var port = ship.Airlock.AddInstance();
        Fit(ship, port);

        Door(ship, "Front Inner").ForceOpen();
        Door(ship, "Port Inner").ForceOpen();
        ship.Tick(2);

        port.Enabled = false;
        ship.Rig.Rig.Forget();

        ship.Start(ship.Airlock.Instances[1]);
        ship.Start(Front(ship));
        ship.Tick(3);

        Assert.Contains("Front Inner:close", ship.Rig.Rig.Log);
        Assert.DoesNotContain("Port Inner:close", ship.Rig.Rig.Log);
    }

    /// <summary>
    /// Two switches, and neither overwrites the other.
    /// </summary>
    [Fact]
    public void TheTwoSwitchesAreIndependent()
    {
        var ship = Build();
        var port = ship.Airlock.AddInstance();
        Fit(ship, port);

        var cycle = ship.Program.Sequences[0];
        var front = ship.Airlock.Instances[0];

        port.Enabled = false;

        // Parking one leaves the other and the sequence as they were.
        Assert.False(port.Enabled);
        Assert.True(front.Enabled);
        Assert.True(cycle.CanRun(front));

        Door(ship, "Front Inner").ForceOpen();
        ship.Tick(2);
        ship.Rig.Rig.Forget();

        ship.Start(Front(ship));
        ship.Tick(3);
        Assert.Contains("Front Inner:close", ship.Rig.Rig.Log);
    }

    /// <summary>
    /// Which parts a system acts on is worked out from what it does, and asked once each.
    /// </summary>
    [Fact]
    public void WhatTheSequencesActOnIsWorkedOutFromThem()
    {
        var ship = Build();

        var parts = new List<string>();
        ship.Airlock.Acted(parts);

        // The button, the inner door and the outer door, each once however many steps name it.
        Assert.Equal(new[] { ship.Ref(1), ship.Ref(2), ship.Ref(3) }, parts);
    }

    [Fact]
    public void WhatAnAuthorWroteAboutAPartOutlivesTheStepThatMadeIt()
    {
        var ship = Build();
        ship.Rig.Light(4, "Front Light");

        var warn = new Act("on");
        warn.Targets.Add(ship.Ref(4));
        ship.Program.Sequences[0].Steps.Add(warn);

        var part = ship.Airlock.PartFor(ship.Ref(4));
        part.Annotation = "The warning light. Whatever you want flashing while the doors move.";
        part.Expects.Add("InteriorLight");

        var parts = new List<string>();
        ship.Airlock.Acted(parts);
        Assert.Contains(ship.Ref(4), parts);

        // The only step that used it goes, so the system no longer acts on it - but the part it
        // declared, and what its author wrote about it, stay.
        ship.Program.Sequences[0].Steps.Remove(warn);

        ship.Airlock.Acted(parts);
        Assert.DoesNotContain(ship.Ref(4), parts);

        Assert.Equal("The warning light. Whatever you want flashing while the doors move.",
            ship.Airlock.FindPart(ship.Ref(4))!.Annotation);
        Assert.Equal(new[] { "InteriorLight" }, ship.Airlock.FindPart(ship.Ref(4))!.Expects);
    }

    /// <summary>
    /// Another one of a system gets a number no other one has had.
    /// </summary>
    /// <remarks>
    /// Counting them handed out a number that was already in use as soon as one in the middle was
    /// saved with a higher number than the count, and a number decides which one a switch is about.
    /// </remarks>
    [Fact]
    public void AnotherOneOfASystemNeverTakesANumberInUse()
    {
        var program = new Automation();
        var system = program.AddSystem();
        system.Restore(4, "Airlock 4");

        var added = system.AddInstance();

        Assert.Equal(5, added.Number);
    }

    /// <summary>
    /// An installation's name and its number are unrelated: the name counts on from the installations there are, the number
    /// from the highest.
    /// </summary>
    [Fact]
    public void AnInstallationsNameIsNotItsNumber()
    {
        var program = new Automation();
        var system = program.AddSystem();
        system.IsBlueprint = true;
        system.Restore(4, "Port airlock");

        var name = SystemSummary.NewInstanceName(new Texts(), system);
        var added = system.AddInstance();

        Assert.Equal("Installation 2", name);
        Assert.Equal(5, added.Number);
    }
}
