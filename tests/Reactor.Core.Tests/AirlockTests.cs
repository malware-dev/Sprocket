using Reactor.Runtime;
using Reactor.World;
using Reactor.Sequences;
using Xunit;

namespace Reactor.Tests;

/// <summary>
/// The airlock, built the way a player describes it out loud.
/// </summary>
/// <remarks>
/// "I hit the button. The inner door shuts. The air gets pumped out. The outer door
/// opens."
///
/// Three sentences, three steps, and nothing else - no gates, no waits, no
/// conditions. The ordering is carried by the list being a list, exactly as it is in
/// a recipe. If the model cannot express a player's own sentence without additions,
/// the model is wrong; that is what these tests are really checking.
/// </remarks>
public class AirlockTests
{
    sealed class Airlock
    {
        public TestWorld World = new();
        public Automation Automation = new();
        public AutomationRunner Runner = null!;

        public TestDoor Inner = null!;
        public TestDoor Outer = null!;
        public TestVent Vent = null!;
        public TestButton Inside = null!;

        public Sequence LetMeOut = null!;

        int _frame;

        public void Tick(int ticks = 1)
        {
            for (var i = 0; i < ticks; i++)
            {
                World.Tick();
                Runner.Tick(++_frame);
            }
        }

        public SequenceRunner State => Runner.Get(LetMeOut)!;
    }

    static Condition Is(string stateId, params long[] entities)
    {
        var condition = new Condition(stateId);
        foreach (var entity in entities)
            condition.Entities.Add(entity);
        return condition;
    }

    static Step Do(string actionId, params long[] targets)
    {
        var step = new Step(actionId);
        foreach (var target in targets)
            step.Targets.Add(target);
        return step;
    }

    static Airlock Build()
    {
        var a = new Airlock();
        a.Inner = a.World.Add(new TestDoor(1, "Inner Door"));
        a.Outer = a.World.Add(new TestDoor(2, "Outer Door"));
        a.Vent = a.World.Add(new TestVent(3, "Airlock Vent"));
        a.Inside = a.World.Add(new TestButton(4, "Inside Button"));

        var trigger = new WhenTrigger("pressed");
        trigger.Sources.Add(a.Inside.Id);

        a.LetMeOut = a.Automation.AddSequence(trigger);
        a.LetMeOut.Name = "Let me out";

        a.LetMeOut.Steps.Add(Do("close", a.Inner.Id));
        a.LetMeOut.Steps.Add(Do("depressurize", a.Vent.Id));
        a.LetMeOut.Steps.Add(Do("open", a.Outer.Id));

        a.Runner = new AutomationRunner(a.Automation, a.World);
        return a;
    }

    [Fact]
    public void EachStepFinishesBeforeTheNextBegins()
    {
        var a = Build();
        a.Inner.ForceOpen();
        a.Tick(3);
        a.World.Log.Clear();

        a.Inside.Press();
        a.Tick();
        Assert.Equal(["Inner Door:close"], a.World.Log);

        // The door takes time, and nothing else may happen while it travels. Nobody
        // wrote "once it is shut" - the list being a list is what says that.
        a.Tick(5);
        Assert.Equal(["Inner Door:close"], a.World.Log);
        Assert.True(a.State.IsWaiting);
        Assert.Equal(0, a.State.CurrentStep);

        a.Tick(10);
        Assert.Contains("Airlock Vent:depressurize", a.World.Log);
        Assert.DoesNotContain("Outer Door:open", a.World.Log);

        a.Tick(15);
        Assert.Equal(["Inner Door:close", "Airlock Vent:depressurize", "Outer Door:open"], a.World.Log);

        a.Tick(15);
        Assert.False(a.State.IsRunning);
    }

    [Fact]
    public void AJammedDoorStopsTheSequence_RatherThanVentingTheShip()
    {
        // The whole argument for waiting on the thing itself rather than on a
        // stopwatch. A guessed duration produces an airlock that works until the day
        // a door sticks.
        var a = Build();
        a.Inner.ForceOpen();
        a.Tick(3);
        a.World.Log.Clear();

        a.Inside.Press();
        a.Tick();

        for (var i = 0; i < 60; i++)
        {
            a.Inner.ForceOpen();
            a.Tick();
        }

        Assert.True(a.State.IsWaiting);
        Assert.Equal(0, a.State.CurrentStep);
        Assert.Equal(["Inner Door:close"], a.World.Log);
    }

    [Fact]
    public void InstantActionsAreNotWaitedOn()
    {
        // Only things that genuinely take time hold the sequence up. An action that
        // reaches for no particular state is finished the moment it is issued.
        var a = Build();
        var beacon = a.World.Add(new TestLamp(5, "Beacon"));

        var sequence = a.Automation.AddSequence(new EveryTrigger(1));
        sequence.Steps.Add(Do("on", beacon.Id));
        sequence.Steps.Add(Do("close", a.Inner.Id));

        a.Tick();
        Assert.Equal(["Beacon:on", "Inner Door:close"], a.World.Log);
    }

    [Fact]
    public void DoingTwoThingsAtOnceHasToBeAskedFor()
    {
        var a = Build();
        a.Inner.ForceOpen();
        a.Outer.ForceOpen();
        a.Tick(3);
        a.World.Log.Clear();

        var sequence = a.Automation.AddSequence(new EveryTrigger(1000));
        var first = Do("close", a.Inner.Id);
        first.WaitsForCompletion = false;
        sequence.Steps.Add(first);
        sequence.Steps.Add(Do("close", a.Outer.Id));

        a.Tick();                       // lets the runner pick the new sequence up
        a.World.Log.Clear();

        a.Runner.Call(sequence);

        // Both doors are moving together, which would not happen by default.
        Assert.Equal(["Inner Door:close", "Outer Door:close"], a.World.Log);
    }

    [Fact]
    public void PressingMidCycleIsIgnored_AndItSaysWhatItIsWaitingOn()
    {
        var a = Build();
        a.Inner.ForceOpen();
        a.Tick(3);

        a.Inside.Press();
        a.Tick(2);
        a.Inside.Press();
        a.Tick(2);

        Assert.Equal(1, a.State.FiredCount);
        Assert.Equal(1, a.State.SkippedCount);
        Assert.True(a.State.IsWaiting);
        Assert.Equal(0, a.State.CurrentStep);
    }

    [Fact]
    public void AGuardSkipsTheDangerousStep_AndRecordsWhy()
    {
        // "only if the inner door is shut - open the outer door". Unlike a wait, a
        // guard passes the step over and the sequence carries on.
        var a = Build();
        a.LetMeOut.Steps[0].WaitsForCompletion = false;
        a.LetMeOut.Steps[2].Gate = new OnlyIfGate(Is("is-shut", a.Inner.Id));

        a.Inner.ForceOpen();
        a.Tick(3);
        a.World.Log.Clear();

        a.Inside.Press();
        for (var i = 0; i < 30; i++)
        {
            a.Inner.ForceOpen();
            a.Tick();
        }

        Assert.DoesNotContain("Outer Door:open", a.World.Log);
        Assert.NotNull(a.State.LastSkippedStep);
        Assert.Equal(2, a.State.LastSkippedStep!.StepIndex);
    }

    /// <summary>
    /// The airlock as a player would actually write it: secure the side you are not
    /// using, then open the side you are.
    /// </summary>
    /// <remarks>
    /// There is no safety feature here. No interlock, no guard, no second set of
    /// sequences watching the doors. Safety is simply the first half of the
    /// procedure, which is how a careful person operates a real airlock.
    ///
    /// Note that step 1 needs no condition on it. Closing an already-closed door
    /// finishes instantly, so "make sure the outer door is shut" and "close the outer
    /// door" are the same sentence - and nobody ever has to write "if it is open,
    /// close it".
    /// </remarks>
    static Sequence Cycle(Airlock a, string name, TestDoor leave, TestDoor enter, string pump)
    {
        var sequence = a.Automation.AddSequence(new CalledTrigger());
        sequence.Name = name;
        sequence.Steps.Add(Do("close", leave.Id));
        sequence.Steps.Add(Do("lock", leave.Id));
        sequence.Steps.Add(Do(pump, a.Vent.Id));
        sequence.Steps.Add(Do("unlock", enter.Id));
        sequence.Steps.Add(Do("open", enter.Id));
        return sequence;
    }

    [Fact]
    public void BothDoorsAreNeverOpenAtOnce_WithNoSafetyFeatureAtAll()
    {
        var a = Build();
        var letMeOut = Cycle(a, "Let me out", a.Inner, a.Outer, "depressurize");

        // Someone has just walked in from inside, so the inner door is open.
        a.Inner.ForceOpen();
        a.Tick(3);
        a.Runner.Call(letMeOut);

        for (var i = 0; i < 80; i++)
        {
            a.Tick();
            Assert.False(a.Inner.Status == "open" && a.Outer.Status == "open",
                $"both doors open at tick {i}");
        }

        Assert.Equal("open", a.Outer.Status);
        Assert.Equal("shut", a.Inner.Status);
    }

    [Fact]
    public void MakingSureIsTheSameAsDoing()
    {
        // The outer door is already shut, so step 1 costs nothing and the sequence
        // moves straight on. This is what removes the need for conditionals.
        var a = Build();
        var letMeOut = Cycle(a, "Let me out", a.Inner, a.Outer, "depressurize");

        a.Tick(3);
        a.World.Log.Clear();
        a.Runner.Call(letMeOut);

        Assert.Equal(["Inner Door:close", "Inner Door:lock", "Airlock Vent:depressurize"], a.World.Log);
    }

    [Fact]
    public void LockingBeforeTheDoorHasShutPinsItOpen()
    {
        // Why waiting is the default. Locking is disabling, and a disabled door stops
        // where it is - so skipping the wait leaves the door the sequence was trying
        // to secure standing open.
        var a = Build();
        var letMeOut = Cycle(a, "Let me out", a.Inner, a.Outer, "depressurize");
        letMeOut.Steps[0].WaitsForCompletion = false;

        a.Inner.ForceOpen();
        a.Tick(3);
        a.Runner.Call(letMeOut);
        a.Tick(80);

        Assert.Equal("open", a.Inner.Status);
        Assert.Equal("open", a.Outer.Status);
    }

    /// <summary>
    /// The other way people build airlocks: no cycle button at all, just a mutual
    /// exclusion between the doors, so operating one by hand secures the other.
    /// </summary>
    /// <remarks>
    /// Neither style is privileged by the system, which is the point - Reactor is an
    /// automation system, not a shipped airlock.
    ///
    /// This only ever locks and unlocks; it never closes. That matters. A version
    /// that closed the opposite door would feed back on itself: closing the outer
    /// door raises "outer door finishes closing", which would then close the inner
    /// door the player had just walked through. Locking raises no such event.
    /// </remarks>
    static void Mutex(Airlock a, TestDoor door, TestDoor other)
    {
        var opened = new WhenTrigger("opened");
        opened.Sources.Add(door.Id);
        var secure = a.Automation.AddSequence(opened);
        secure.Name = $"Secure the other door while {door.Name} is open";
        secure.Steps.Add(Do("lock", other.Id));

        var shut = new WhenTrigger("shut");
        shut.Sources.Add(door.Id);
        var release = a.Automation.AddSequence(shut);
        release.Name = $"Release the other door when {door.Name} shuts";
        release.Steps.Add(Do("unlock", other.Id));
    }

    [Fact]
    public void OperatingOneDoorByHandSecuresTheOther()
    {
        var a = Build();
        Mutex(a, a.Inner, a.Outer);
        Mutex(a, a.Outer, a.Inner);

        a.Tick(3);

        // The player opens the inner door themselves - Reactor never touches it.
        a.Inner.ForceOpen();
        a.Tick(3);

        // Anything trying to open the outer door now gets nowhere.
        var careless = a.Automation.AddSequence(new EveryTrigger(1));
        careless.Steps.Add(Do("open", a.Outer.Id));
        a.Tick(30);

        Assert.NotEqual("open", a.Outer.Status);
    }

    [Fact]
    public void ClosingTheDoorReleasesTheOtherOne()
    {
        var a = Build();
        Mutex(a, a.Inner, a.Outer);
        Mutex(a, a.Outer, a.Inner);

        a.Tick(3);
        a.Inner.ForceOpen();
        a.Tick(3);

        a.World.Invoke(a.Inner, "close");
        a.Tick(15);

        var opener = a.Automation.AddSequence(new EveryTrigger(1));
        opener.Steps.Add(Do("open", a.Outer.Id));
        a.Tick(30);

        Assert.Equal("open", a.Outer.Status);
    }

    [Fact]
    public void TheReverseDirectionIsTheSameSequenceMirrored()
    {
        var a = Build();
        var letMeIn = Cycle(a, "Let me in", a.Outer, a.Inner, "pressurize");

        a.Outer.ForceOpen();
        a.Tick(3);
        a.Runner.Call(letMeIn);
        a.Tick(80);

        Assert.Equal("open", a.Inner.Status);
        Assert.Equal("shut", a.Outer.Status);
    }
}

/// <summary>
/// A reading is a reading, whatever sort it is.
/// </summary>
/// <remarks>
/// The model used to have boolean signals as its primitive, so a tank had to decide it was
/// "low" and hand core a flag, throwing the number away before anything could ask a different
/// question of it. A flag is just a property whose readings are true and false; the level and
/// the label are properties too, and a state is a relation against whichever one it names.
/// </remarks>
public class PropertyTests
{
    static (TestWorld World, TestTank Tank) Build()
    {
        var world = new TestWorld();
        var tank = world.Add(new TestTank(1, "Hydrogen Tank"));
        return (world, tank);
    }

    [Fact]
    public void AStateOverANumberComparesAgainstIt()
    {
        var (world, tank) = Build();
        var low = tank.Kind.FindState("is-low")!;
        var fine = tank.Kind.FindState("is-fine")!;

        tank.SetFill(0.10);
        Assert.True(low.Holds(tank.GetValue("fill")));
        Assert.False(fine.Holds(tank.GetValue("fill")));

        // The boundary belongs to "has plenty": is-low is Below, is-fine is AtLeast, so the
        // pair covers every reading exactly once and no level is both or neither.
        tank.SetFill(0.25);
        Assert.False(low.Holds(tank.GetValue("fill")));
        Assert.True(fine.Holds(tank.GetValue("fill")));
    }

    [Fact]
    public void AStateOverTextComparesAgainstIt()
    {
        var (world, tank) = Build();
        var spare = tank.Kind.FindState("is-spare")!;

        tank.SetLabel("main");
        Assert.False(spare.Holds(tank.GetValue("label")));

        tank.SetLabel("spare");
        Assert.True(spare.Holds(tank.GetValue("label")));
    }

    [Fact]
    public void AFlagIsJustAPropertyWhoseReadingsAreTrueAndFalse()
    {
        var world = new TestWorld();
        var door = world.Add(new TestDoor(1, "Inner Door"));
        var open = door.Kind.FindState("is-open")!;

        Assert.Equal(ValueKind.Flag, door.Kind.FindProperty("open")!.Kind);
        Assert.Equal(Relation.Is, open.Relation);
        Assert.True(open.Operand.Flag);
    }

    /// <summary>Only a number can be above something; anything can equal something.</summary>
    [Fact]
    public void TheRelationsOfferedDependOnWhatIsBeingRead()
    {
        Assert.True(Relations.Suits(Relation.Is, ValueKind.Text));
        Assert.False(Relations.Suits(Relation.Above, ValueKind.Text));
        Assert.False(Relations.Suits(Relation.Above, ValueKind.Flag));
        Assert.True(Relations.Suits(Relation.Above, ValueKind.Number));
    }
}
