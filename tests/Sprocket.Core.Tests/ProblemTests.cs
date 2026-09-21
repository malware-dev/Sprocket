using Sprocket.Host.SpaceEngineers.Editing;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using Xunit;

namespace Sprocket.Tests;

/// <summary>
/// Something is wrong with a sequence, and every screen between the front door and the line at
/// fault says so.
/// </summary>
/// <remarks>
/// A problem nobody can find is a problem nobody can fix. The failure these hold to is the quiet
/// one: a mark on the ship's screen, nothing on the system's, and a red banner in the editor with
/// nine steps under it and no indication which of them is being complained about.
/// <para>
/// The case they are built on is different in kind from the rest: nothing the sequence names is
/// missing. Somebody cleared the button from the game's own terminal, which is a legitimate way to
/// say "stop doing that" - so it is reported rather than undone.
/// </para>
/// </remarks>
public class ProblemTests
{
    [Fact]
    public void ASequenceWhoseTriggerCanNeverFireSaysSo()
    {
        var ship = Build();

        Assert.False(Fault(ship).Exists);

        // What a button that no longer starts this looks like from core's side.
        ship.Rig.CannotStart(ship.Sequence);

        var fault = Fault(ship);
        Assert.Equal(SequenceProblem.NeverTriggered, fault.Problem);
        Assert.Equal(SequenceFault.Trigger, fault.Step);
    }

    /// <summary>Half way through picking blocks is not the same as unreachable.</summary>
    /// <remarks>
    /// Nothing chosen and nothing done yet is somebody mid-thought, and saying "nothing can start
    /// this" would be scolding them for not having finished. What tells it from a built sequence
    /// that has lost its trigger is whether it does anything at all.
    /// </remarks>
    [Fact]
    public void AnUnfinishedTriggerIsNotReported()
    {
        var ship = Build();
        ((MomentTrigger)ship.Sequence.Trigger).Sources.Clear();
        ship.Sequence.Steps.Clear();

        ship.Rig.CannotStart(ship.Sequence);

        Assert.False(Fault(ship).Exists);
    }

    /// <summary>
    /// A sequence nothing starts and nothing calls is remarked on, not marked broken.
    /// </summary>
    /// <remarks>
    /// The case that could not be judged until a step could call one: a sequence with no trigger was
    /// always allowed, because it might be there to be <c>run</c> by another. Now that the calling is
    /// written down, "no trigger and no caller" is worth saying - and it is a warning, because nothing
    /// is broken and nothing is wired wrongly. There is simply nobody to start it yet.
    /// </remarks>
    [Fact]
    public void ASequenceNothingStartsOrCallsIsWarnedAbout()
    {
        var ship = Build();

        var warn = ship.Automation.AddSequence(ship.Automation.Systems[0]);
        warn.Name = "Warn";

        // Nothing starts it and it does nothing yet: somebody mid-build, which is not worth a word.
        Assert.False(Fault(ship, warn).Exists);

        var shut = new Act("close");
        shut.Targets.Add(ship.Automation.Declare(ship.World, ship.World.FindEntity(2)!).Name);
        warn.Steps.Add(shut);
        warn.Steps.Add(new Await());

        var fault = Fault(ship, warn);
        Assert.Equal(SequenceProblem.NothingStarts, fault.Problem);
        Assert.True(fault.IsWarning);

        // A step that runs it is what it was missing, and it needs no trigger of its own.
        ship.Sequence.Steps.Add(new Run(warn.Number));

        Assert.False(Fault(ship, warn).Exists);
    }

    /// <summary>
    /// A warning says its words and nothing else: no count, no mark, and it is not switched off.
    /// </summary>
    /// <remarks>
    /// The difference from a fault, at the screen. A row washed red sends somebody hunting for a
    /// broken block, the count on a ship's screen is a promise that something is wrong, and parking
    /// it would make the step somebody is about to point at it fail for a second reason.
    /// </remarks>
    [Fact]
    public void AWarningIsNotCountedMarkedOrSwitchedOff()
    {
        var ship = Build();

        var warn = ship.Automation.AddSequence(ship.Automation.Systems[0]);
        warn.Name = "Warn";
        var shut = new Act("close");
        shut.Targets.Add(ship.Automation.Declare(ship.World, ship.World.FindEntity(2)!).Name);
        warn.Steps.Add(shut);
        warn.Steps.Add(new Await());

        // Long enough for the screens to have marked what they mark, and for Sprocket to have parked
        // anything it parks.
        ship.Runner.Tick(600);
        ship.Editor.Tick(600);

        Assert.True(warn.Enabled);

        ship.Editor.PressRow(1);
        ship.Editor.Tick(1200);

        var row = ship.Editor.Rows()[1];
        Assert.Equal("Nothing starts this yet.", row.Problem);
        Assert.True(row.ProblemIsWarning);
        Assert.Equal(0, row.Problems);

        // And the sequence's own screen says it too, quietly.
        ship.Editor.PressRow(1);
        ship.Editor.Tick(1800);

        var sentence = ship.Editor.Sentence();
        Assert.Equal("Nothing starts this yet.", sentence.Problem);
        Assert.True(sentence.ProblemIsWarning);
    }

    /// <summary>
    /// Something missing is said before something unreachable, and it is said about its own step.
    /// </summary>
    /// <remarks>
    /// Both are true of this sequence at once: the door it closes is gone and its button raises
    /// nothing. A screen that said "nothing can start this" would be sending somebody to mend the
    /// trigger, which is not where the news is.
    /// </remarks>
    [Fact]
    public void AMissingTargetIsReportedOnItsOwnStep()
    {
        var ship = Build();

        ship.Rig.CannotStart(ship.Sequence);
        ship.Rig.Grind(2);

        var fault = Fault(ship);
        Assert.Equal(SequenceProblem.MissingTarget, fault.Problem);
        Assert.Equal(0, fault.Step);
    }

    /// <summary>
    /// The one of a system that is broken is marked, and the system itself is not.
    /// </summary>
    /// <remarks>
    /// The same sequence can be sound on the front airlock and broken on the port one, so the count
    /// belongs to the fitting rather than the design. Marking both would say there are two problems.
    /// </remarks>
    [Fact]
    public void TheFailingInstanceIsMarkedAndTheSystemIsNot()
    {
        var ship = Build();
        var rows = ship.Editor.Rows();

        Assert.Equal(0, rows[0].Problems);
        Assert.Equal(0, rows[1].Problems);

        // Worked out on a cadence while the screen is open, so a tick is what picks up a change
        // made outside Sprocket - answering can cost a toolbar read, so it is not asked per frame.
        ship.Rig.CannotStart(ship.Sequence);
        ship.Runner.Tick(600);
        ship.Editor.Tick(600);

        rows = ship.Editor.Rows();
        Assert.Equal(0, rows[0].Problems);
        Assert.Equal(1, rows[1].Problems);
    }

    /// <summary>
    /// The screen in the middle of the trail marks the sequence, and says what is wrong with it.
    /// </summary>
    /// <remarks>
    /// This is the screen the mark on the front door sends somebody to. A system's list that stayed
    /// quiet would end the trail one screen short of the sequence, leaving them to open each in turn.
    /// </remarks>
    [Fact]
    public void TheBrokenSequenceIsMarkedWhereItIsListed()
    {
        var ship = Build();
        ship.Rig.Grind(2);

        ship.Editor.PressRow(1);
        ship.Editor.Tick(1200);

        var rows = ship.Editor.Rows();
        Assert.Equal(1, rows[0].Problems);
        Assert.Equal("This affects something that isn't here any more.", rows[0].Problem);
    }

    /// <summary>
    /// The end of the trail: a trigger that cannot fire is marked on its own row.
    /// </summary>
    /// <remarks>
    /// What is wrong is said once, and it is said where it can be mended - pressing that row asks
    /// again which block starts this. The sequence's row says nothing, because the sequence is not
    /// what is broken, and a screen saying it twice would send somebody to the wrong place half
    /// the time.
    /// </remarks>
    [Fact]
    public void TheTriggerAtFaultIsMarkedOnItsOwnRow()
    {
        var ship = Build();
        ship.Rig.CannotStart(ship.Sequence);

        ship.Editor.PressRow(1);
        ship.Editor.Tick(1200);

        Assert.Equal("Nothing can start this.", ship.Editor.Triggers()[0].Problem);

        Assert.Equal("", ship.Editor.Rows()[0].Problem);
        Assert.Equal(0, ship.Editor.Rows()[0].Problems);
    }

    /// <summary>
    /// And the sequence's own screen still says what is wrong, without a line to mark.
    /// </summary>
    /// <remarks>
    /// Somebody standing on the sequence wondering why it never runs is owed the reason. What
    /// starts it is not on this screen, so there is nothing here to point at - the words are the
    /// whole of what it has to say, and no step is accused of it.
    /// </remarks>
    [Fact]
    public void TheSequenceSaysItWithoutMarkingAStep()
    {
        var ship = Build();
        ship.Rig.CannotStart(ship.Sequence);

        ship.Editor.PressRow(1);
        ship.Editor.PressRow(0);

        var sentence = ship.Editor.Sentence();
        Assert.Equal("Nothing can start this.", sentence.Problem);
        Assert.All(sentence.Steps, step => Assert.False(step.HasProblem));
    }

    /// <summary>
    /// A built sequence with nothing left to start it says so, however it got that way.
    /// </summary>
    /// <remarks>
    /// A trigger empties when the host takes away a block whose arrangement is gone - a button
    /// somebody cleared. That leaves the same shape as a sequence half way through being built, and
    /// those two must not be treated alike: one is somebody mid-thought, the other is a finished
    /// thing that cannot run. Having steps is what tells them apart.
    /// </remarks>
    [Fact]
    public void ABuiltSequenceWithNothingToStartItSaysSo()
    {
        var ship = Build();
        ((MomentTrigger)ship.Sequence.Trigger).Sources.Clear();

        var fault = Fault(ship);
        Assert.Equal(SequenceProblem.NeverTriggered, fault.Problem);
        Assert.Equal(SequenceFault.Trigger, fault.Step);
    }

    /// <summary>
    /// A sequence that cannot work is switched off, and mending it does not switch it back on.
    /// </summary>
    /// <remarks>
    /// Switching it off costs nothing, because it was not going to run. What it buys is that editing
    /// it no longer starts with an errand - a sequence may only be changed while it is parked.
    /// Switching one on stays somebody's decision every time, so mending leaves it parked.
    /// </remarks>
    [Fact]
    public void ABrokenSequenceIsSwitchedOff()
    {
        var ship = Build();
        ship.Sequence.Enabled = true;

        ship.Rig.CannotStart(ship.Sequence);
        ship.Runner.Tick(600);
        ship.Editor.Tick(600);

        Assert.False(ship.Sequence.Enabled);

        ship.Rig.CanStart(ship.Sequence);
        ship.Runner.Tick(1200);
        ship.Editor.Tick(1200);

        Assert.False(ship.Sequence.Enabled);
    }

    /// <summary>A step's own trouble marks the step, and only the step.</summary>
    [Fact]
    public void AStepAtFaultIsMarkedRatherThanTheWholeSentence()
    {
        var ship = Build();

        ship.Editor.PressRow(1);
        ship.Editor.PressRow(0);

        ship.Rig.Grind(2);
        ship.Runner.Tick(600);
        ship.Editor.Tick(600);

        var sentence = ship.Editor.Sentence();
        Assert.True(sentence.Steps[0].HasProblem);
        Assert.NotEmpty(sentence.Problem);
    }

    /// <summary>A mended trigger stops being marked, which is the half that is easy to forget.</summary>
    [Fact]
    public void MendingItClearsTheMark()
    {
        var ship = Build();
        ship.Rig.CannotStart(ship.Sequence);

        ship.Editor.PressRow(1);
        ship.Editor.Tick(1200);
        Assert.Equal("Nothing can start this.", ship.Editor.Triggers()[0].Problem);

        ship.Rig.CanStart(ship.Sequence);
        ship.Runner.Tick(1800);
        ship.Editor.Tick(1800);

        Assert.True(string.IsNullOrEmpty(ship.Editor.Triggers()[0].Problem));
    }

    /// <summary>
    /// A step whose blocks do not offer what it does is broken, and stays as it was written.
    /// </summary>
    /// <remarks>
    /// A block replaced by one of another kind, or the mod that taught it the act removed. Opening the
    /// sequence must not turn the step into something the blocks do offer: that is the screen writing
    /// a program nobody wrote, and the next save keeping it.
    /// </remarks>
    [Fact]
    public void AStepItsBlocksDoNotOfferIsBrokenAndLeftAsWritten()
    {
        var ship = Build();
        var act = (Act)ship.Sequence.Steps[0];
        act.ActionId = "transmogrify";

        var fault = Fault(ship);
        Assert.Equal(SequenceProblem.Unoffered, fault.Problem);
        Assert.Equal(0, fault.Step);
        Assert.True(fault.Parks);

        // Into the sequence, which is where the step is built for the screen.
        ship.Editor.PressRow(1);
        ship.Editor.PressRow(0);
        ship.Editor.Tick(600);

        Assert.IsType<SequencePage>(ship.Editor.Current);
        Assert.Equal("transmogrify", act.ActionId);
    }

    /// <summary>A step that does not wait for what it started is a fault this version reports.</summary>
    /// <remarks>
    /// The editor draws every step as one that waits, because it writes a wait after every action.
    /// A program from anywhere else can leave one out, and drawn as it stands the screen would say the
    /// door shuts before anything else happens when nothing waits for it at all.
    /// </remarks>
    [Fact]
    public void AStepThatDoesNotWaitIsAFault()
    {
        var ship = Build();
        Assert.IsType<Await>(ship.Sequence.Steps[1]);
        ship.Sequence.Steps.RemoveAt(1);

        var fault = Fault(ship);
        Assert.Equal(SequenceProblem.Unawaited, fault.Problem);
        Assert.Equal(0, fault.Step);
        Assert.True(fault.Parks);
    }

    static SequenceFault Fault(Ship ship) => Fault(ship, ship.Sequence);

    static SequenceFault Fault(Ship ship, Sequence sequence) =>
        SequenceSummary.Fault(ship.World, ship.Runner.Bindings, sequence,
            ship.Runner.Get(sequence));

    static Ship Build()
    {
        var ship = new Ship();
        ship.Rig.Panel(1, "Airlock Button");
        ship.Rig.Door(2, "Inner Door");

        ship.Runner = new AutomationRunner(ship.Automation, ship.World);
        ship.Editor = new SprocketEditor(ship.World, ship.Automation, ship.Runner);

        ship.Editor.NewSystem("Airlock");
        ship.Editor.PressStart("When…");
        ship.Editor.PressBlock("Airlock Button");
        ship.Editor.PressChoice("is pressed");
        ship.Editor.PressPrimary();
        ship.Editor.PressChoice("Run a new sequence");

        ship.Editor.PressAdd("do something");
        ship.Editor.PressBlock("Inner Door");
        ship.Editor.PressChoice("close");
        ship.Editor.PressPrimary();

        ship.Editor.GoToRoot();
        ship.Sequence = ship.Automation.Sequences[0];

        // Switched on, because these are about a sequence that would work: one left parked says so
        // itself, and that would be the answer to every question below rather than the one asked.
        ship.Sequence.Enabled = true;
        return ship;
    }

    sealed class Ship
    {
        public Rigged Rig = new();

        public GridWorld World => Rig.World;
        public Automation Automation = new();
        public AutomationRunner Runner = null!;
        public SprocketEditor Editor = null!;
        public Sequence Sequence = null!;
    }

    /// <summary>
    /// A trigger the construct has to answer, and has not, is reported on the sequence.
    /// </summary>
    /// <remarks>
    /// Which slot answers which declaration is nowhere recorded - the host counts them - so what core
    /// asks is whether anything can start this sequence at all, whatever sort of trigger it declares.
    /// </remarks>
    [Fact]
    public void ATriggerNothingAnsweredIsReported()
    {
        var ship = Build();
        var system = ship.Automation.Systems[0];
        var sequence = ship.Automation.AddSequence(system);

        var needed = new RequiredTrigger();
        needed.Runs = sequence.Number;
        system.AddTrigger(needed);

        // Nothing on this construct claims it, which is what the host says by this.
        ship.Rig.CannotStart(sequence);

        var fault = SequenceSummary.Fault(ship.World, ship.Runner.Bindings, sequence, null);
        Assert.Equal(SequenceProblem.NeverTriggered, fault.Problem);

        // And a sequence nothing is supposed to start is not scolded: it may be there to be run.
        var called = ship.Automation.AddSequence(system);
        ship.Rig.CannotStart(called);
        Assert.False(SequenceSummary.Fault(ship.World, ship.Runner.Bindings, called, null).Exists);
    }
}
