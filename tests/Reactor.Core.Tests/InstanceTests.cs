using Reactor.Runtime;
using Reactor.Sequences;
using Xunit;

namespace Reactor.Tests;

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
        public TestWorld World = new();
        public Automation Program = new();
        public AutomationRunner Runner = null!;
        public AutomationSystem Airlock = null!;

        /// <summary>Names a block, so a test builds sequences the way the editor does.</summary>
        public string Ref(long id) => Program.Declare(World, World.FindEntity(id)!).Name;

        int _frame;

        public void Tick(int ticks = 1)
        {
            for (var i = 0; i < ticks; i++)
            {
                World.Tick();
                Runner.Tick(++_frame);
            }
        }
    }

    /// <summary>
    /// One airlock, written against the front doors, then fitted a second time to the port.
    /// </summary>
    static Ship Build()
    {
        var ship = new Ship();

        ship.World.Add(new TestButton(1, "Front Button"));
        ship.World.Add(new TestDoor(2, "Front Inner"));
        ship.World.Add(new TestDoor(3, "Front Outer"));

        ship.World.Add(new TestButton(11, "Port Button"));
        ship.World.Add(new TestDoor(12, "Port Inner"));
        ship.World.Add(new TestDoor(13, "Port Outer"));

        ship.Airlock = ship.Program.AddSystem();
        ship.Airlock.Name = "Airlock";

        var when = new WhenTrigger("pressed");
        when.Sources.Add(ship.Ref(1));

        var cycle = ship.Program.AddSequence(ship.Airlock, when);
        cycle.Name = "Cycle";

        var close = new Step("close");
        close.Targets.Add(ship.Ref(2));
        cycle.Steps.Add(close);

        var open = new Step("open");
        open.Targets.Add(ship.Ref(3));
        var onceShut = new Condition("is-shut");
        onceShut.Entities.Add(ship.Ref(2));
        open.Gate = new OnceGate(onceShut);
        cycle.Steps.Add(open);

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

    static TestButton Button(Ship ship, string name) =>
        ship.World.Entities.OfType<TestButton>().First(b => b.Name == name);

    static TestDoor Door(Ship ship, string name) =>
        ship.World.Entities.OfType<TestDoor>().First(d => d.Name == name);

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
        ship.World.Log.Clear();

        ship.Runner.Call(ship.Program.Sequences[0], port);
        ship.Tick(3);

        // The port one ran, and the front one did not - no button was pressed at all.
        Assert.Contains("Port Inner:close", ship.World.Log);
        Assert.DoesNotContain("Front Inner:close", ship.World.Log);
    }

    [Fact]
    public void EachOneOfASystemActsOnItsOwnThings()
    {
        var ship = Build();
        Fit(ship, ship.Airlock.AddInstance());

        Door(ship, "Front Inner").ForceOpen();
        Door(ship, "Port Inner").ForceOpen();
        ship.Tick(2);
        ship.World.Log.Clear();

        Button(ship, "Port Button").Press();
        ship.Tick(3);

        // The port button ran the port airlock, and only the port airlock.
        Assert.Contains("Port Inner:close", ship.World.Log);
        Assert.DoesNotContain("Front Inner:close", ship.World.Log);
    }

    [Fact]
    public void TheFirstOneOfASystemNeedsNoFillingIn()
    {
        var ship = Build();
        Fit(ship, ship.Airlock.AddInstance());

        Door(ship, "Front Inner").ForceOpen();
        ship.Tick(2);
        ship.World.Log.Clear();

        Button(ship, "Front Button").Press();
        ship.Tick(3);

        Assert.Contains("Front Inner:close", ship.World.Log);
        Assert.DoesNotContain("Port Inner:close", ship.World.Log);
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

        // The front inner door is shut; the port one is wedged open by a lock.
        Door(ship, "Port Inner").ForceOpen();
        Door(ship, "Port Inner").Invoke("lock");
        ship.Tick(2);
        ship.World.Log.Clear();

        Button(ship, "Port Button").Press();
        ship.Tick(20);

        // It tried to close its own door and is waiting on it, rather than reading the front
        // airlock's shut door and opening onto space.
        Assert.Contains("Port Inner:close", ship.World.Log);
        Assert.DoesNotContain("Port Outer:open", ship.World.Log);
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

        Button(ship, "Front Button").Press();
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
        ship.World.Log.Clear();

        Button(ship, "Port Button").Press();
        Button(ship, "Front Button").Press();
        ship.Tick(3);

        Assert.Contains("Front Inner:close", ship.World.Log);
        Assert.DoesNotContain("Port Inner:close", ship.World.Log);
    }

    /// <summary>
    /// Three switches, and none of them overwrites another.
    /// </summary>
    [Fact]
    public void TheThreeSwitchesAreIndependent()
    {
        var ship = Build();
        var port = ship.Airlock.AddInstance();
        Fit(ship, port);

        var cycle = ship.Program.Sequences[0];
        var front = ship.Airlock.Instances[0];

        port.Enabled = false;
        ship.Airlock.Enabled = false;
        ship.Airlock.Enabled = true;

        // Starting the system again leaves the deliberately-parked one parked.
        Assert.False(port.Enabled);
        Assert.True(front.Enabled);
        Assert.True(cycle.Enabled);

        Door(ship, "Front Inner").ForceOpen();
        ship.Tick(2);
        ship.World.Log.Clear();

        Button(ship, "Front Button").Press();
        ship.Tick(3);
        Assert.Contains("Front Inner:close", ship.World.Log);
    }

    /// <summary>
    /// The parts a system needs are worked out from what it does, and asked once each.
    /// </summary>
    [Fact]
    public void ThePartsAreDerivedFromWhatTheSequencesUse()
    {
        var ship = Build();

        var parts = new List<string>();
        ship.Airlock.Parts(parts);

        // The button, the inner door, the outer door - and the inner door only once, though it
        // appears in a step and in a gate.
        Assert.Equal(new[] { ship.Ref(1), ship.Ref(2), ship.Ref(3) }, parts);
    }

    [Fact]
    public void WhatAnAuthorWroteAboutAPartOutlivesTheStepThatMadeIt()
    {
        var ship = Build();
        ship.World.Add(new TestLamp(4, "Front Light"));

        var warn = new Step("on");
        warn.Targets.Add(ship.Ref(4));
        ship.Program.Sequences[0].Steps.Add(warn);

        var role = ship.Airlock.RoleFor(ship.Ref(4));
        role.Name = "the warning light";
        role.Instructions = "Whatever you want flashing while the doors move.";

        var parts = new List<string>();
        ship.Airlock.Parts(parts);
        Assert.Contains(ship.Ref(4), parts);

        // The only step that used it goes. The part goes with it - the list is derived, so it
        // cannot disagree with what the system does - but what its author wrote does not.
        ship.Program.Sequences[0].Steps.Remove(warn);

        ship.Airlock.Parts(parts);
        Assert.DoesNotContain(ship.Ref(4), parts);

        Assert.Equal("the warning light", ship.Airlock.FindRole(ship.Ref(4))!.Name);
        Assert.Equal("Whatever you want flashing while the doors move.",
            ship.Airlock.FindRole(ship.Ref(4))!.Instructions);
    }
}
