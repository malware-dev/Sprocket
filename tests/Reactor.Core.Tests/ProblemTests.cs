using Reactor.Host.SpaceEngineers.Core.Editing;
using Reactor.Host.SpaceEngineers.Core.Runtime;
using Reactor.Host.SpaceEngineers.Core.Sequences;
using Reactor.Host.SpaceEngineers.World;
using Xunit;

namespace Reactor.Tests;

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
        // made outside Reactor - answering can cost a toolbar read, so it is not asked per frame.
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
        ship.Rig.CannotStart(ship.Sequence);

        ship.Editor.PressRow(1);

        var rows = ship.Editor.Rows();
        Assert.Equal(1, rows[0].Problems);
        Assert.Equal("Nothing can start this.", rows[0].Problem);
    }

    /// <summary>
    /// The end of the trail: the line at fault is marked, and no other line is.
    /// </summary>
    /// <remarks>
    /// What is wrong is said once, in words, on the sentence. Which line it is about is said by the
    /// mark, because words alone in front of a multi-step sequence are an accusation without an
    /// address - and somebody arrived here from two screens away precisely so they would not have to
    /// read all of it to work out where to press.
    /// </remarks>
    [Fact]
    public void TheFailingLineIsMarkedInTheEditor()
    {
        var ship = Build();
        ship.Rig.CannotStart(ship.Sequence);

        ship.Editor.PressRow(1);
        ship.Editor.PressRow(0);

        var sentence = ship.Editor.Sentence();
        Assert.Equal("Nothing can start this.", sentence.Problem);
        Assert.True(sentence.Trigger.HasProblem);
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

    /// <summary>A step's own trouble marks the step, and leaves the trigger alone.</summary>
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
        Assert.False(sentence.Trigger.HasProblem);
        Assert.True(sentence.Steps[0].HasProblem);
    }

    /// <summary>A mended sequence stops being marked, which is the half that is easy to forget.</summary>
    [Fact]
    public void MendingItClearsTheMark()
    {
        var ship = Build();
        ship.Rig.CannotStart(ship.Sequence);

        ship.Editor.PressRow(1);
        ship.Editor.PressRow(0);
        Assert.True(ship.Editor.Sentence().Trigger.HasProblem);

        ship.Rig.CanStart(ship.Sequence);
        ship.Runner.Tick(600);
        ship.Editor.Tick(600);

        var sentence = ship.Editor.Sentence();
        Assert.True(string.IsNullOrEmpty(sentence.Problem));
        Assert.False(sentence.Trigger.HasProblem);
    }

    static SequenceFault Fault(Ship ship) =>
        SequenceSummary.Fault(ship.World, ship.Runner.Bindings, ship.Sequence,
            ship.Runner.Get(ship.Sequence));

    static Ship Build()
    {
        var ship = new Ship();
        ship.Rig.Panel(1, "Airlock Button");
        ship.Rig.Door(2, "Inner Door");

        ship.Runner = new AutomationRunner(ship.Automation, ship.World);
        ship.Editor = new ReactorEditor(ship.World, ship.Automation, ship.Runner);

        ship.Editor.NewSystem("Airlock");
        ship.Editor.PressStart("When…");
        ship.Editor.PressBlock("Airlock Button");
        ship.Editor.PressChoice("is pressed");
        ship.Editor.PressPrimary();

        ship.Editor.PressAdd("do something");
        ship.Editor.PressBlock("Inner Door");
        ship.Editor.PressChoice("close");
        ship.Editor.PressPrimary();

        ship.Editor.GoToRoot();
        ship.Sequence = ship.Automation.Sequences[0];
        return ship;
    }

    sealed class Ship
    {
        public Rigged Rig = new();

        public GridWorld World => Rig.World;
        public Automation Automation = new();
        public AutomationRunner Runner = null!;
        public ReactorEditor Editor = null!;
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
