using System.Text;
using Reactor.Host.SpaceEngineers.Core.Sequences;
using Xunit;

namespace Reactor.Tests;

/// <summary>
/// Every edit has to reverse itself exactly.
/// </summary>
/// <remarks>
/// This is the price of do-and-undo pairs over replaying a log. An undo that does not exactly
/// reverse its apply produces a state that never existed - half-reverted and self-contradictory
/// - and it only ever runs when somebody is already recovering from a mistake, which is the
/// worst possible moment to hand them a second one.
///
/// So it is not answered by being careful. Every edit gets the same round trip: write down what
/// the sequence reads as, apply, undo, and it must read as exactly that again. Adding an edit
/// without adding a line here is the way this rots.
/// </remarks>
public class UndoTests
{
    /// <summary>
    /// A sequence to edit, and nothing else.
    /// </summary>
    /// <remarks>
    /// No construct: an edit and its undo are about the program, and every one of these tests reads
    /// the sequence back as text. What the blocks it names really are never comes into it.
    /// </remarks>
    static Sequence Build()
    {
        var trigger = new WhenTrigger("pressed");
        trigger.Sources.Add("call");

        var program = new Automation();
        var sequence = program.AddSequence(program.AddSystem(), trigger);
        sequence.Name = "Let me out";
        sequence.Steps.Add(StepFor("close", "inner"));
        sequence.Steps.Add(StepFor("depressurize", "vent"));
        sequence.Steps.Add(StepFor("open", "outer"));

        return sequence;
    }

    static Act StepFor(string actionId, params string[] targets)
    {
        var step = new Act(actionId);
        foreach (var target in targets)
            step.Targets.Add(target);
        return step;
    }

    /// <summary>
    /// Everything about a sequence, flattened, so a difference is readable rather than a
    /// bare false.
    /// </summary>
    static string Shape(Sequence sequence)
    {
        var text = new StringBuilder();
        text.Append(sequence.Name).Append(sequence.Enabled ? " [on]" : " [off]");

        switch (sequence.Trigger)
        {
            case WhenTrigger onEvent:
                text.Append(" when ").Append(onEvent.EventId)
                    .Append(onEvent.RequiresAll ? " all of " : " any of ")
                    .Append(string.Join("+", onEvent.Sources));
                break;
            case RequiredTrigger needed:
                text.Append(" required ").Append(needed.EventId)
                    .Append(" of ").Append(string.Join("+", needed.Sources));
                break;
            case EveryTrigger every:
                text.Append(" every ").Append(every.IntervalTicks);
                break;
        }

        foreach (var step in sequence.Steps)
        {
            if (step is Act act)
            {
                text.Append(" | ").Append(act.ActionId).Append(':')
                    .Append(string.Join("+", act.Targets));
                continue;
            }

            text.Append(step is Wait wait ? " | wait " + wait.Ticks : " | await");
        }

        return text.ToString();
    }

    static void RoundTrip(SequenceEdit edit)
    {
        RoundTrip(Build(), edit);
    }

    /// <summary>The same, where the edit had to be built against the sequence it is about.</summary>
    static void RoundTrip(Sequence sequence, SequenceEdit edit)
    {
        var before = Shape(sequence);

        edit.Apply(sequence);
        Assert.NotEqual(before, Shape(sequence));

        edit.Undo(sequence);
        Assert.Equal(before, Shape(sequence));
    }

    [Fact]
    public void AddingAStepReversesExactly() =>
        RoundTrip(new AddStep("adding a step", 3, StepFor("close", "outer")));

    [Fact]
    public void AddingAStepInTheMiddleReversesExactly() =>
        RoundTrip(new AddStep("adding a step", 1, StepFor("close", "outer")));

    [Fact]
    public void RemovingAStepReversesExactly()
    {
        Sequence sequence = Build();
        RoundTrip(new RemoveStep(1, sequence.Steps[1]));
    }

    [Fact]
    public void RemovingTheFirstStepReversesExactly()
    {
        Sequence sequence = Build();
        RoundTrip(new RemoveStep(0, sequence.Steps[0]));
    }

    [Fact]
    public void MovingAStepReversesExactly() => RoundTrip(new MoveStep(0, 2));

    [Fact]
    public void MovingAStepBackwardsReversesExactly() => RoundTrip(new MoveStep(2, 0));

    [Fact]
    public void ChangingAStepReversesExactly()
    {
        Sequence sequence = Build();
        RoundTrip(new ChangeStep(0, (Act)sequence.Steps[0], new[] { "inner", "outer" }, "lock"));
    }

    [Fact]
    public void ChangingAWaitReversesExactly()
    {
        Sequence sequence = Build();
        sequence.Steps[1] = new Wait(60);
        var edit = new SetWait(1, 60, 120);

        var before = Shape(sequence);
        edit.Apply(sequence);
        Assert.NotEqual(before, Shape(sequence));
        edit.Undo(sequence);
        Assert.Equal(before, Shape(sequence));
    }

    [Fact]
    public void RenamingReversesExactly() => RoundTrip(new Rename("Let me out", "Cycle"));

    /// <summary>
    /// Running or parked is deliberately not an edit.
    /// </summary>
    /// <remarks>
    /// Undo walks back what a sequence is, never whether it is running, so no press of undo
    /// can arm a system behind somebody's back.
    /// </remarks>
    [Fact]
    public void TheSwitchIsNotSomethingUndoCanTouch()
    {
        Assert.DoesNotContain(
            typeof(SequenceEdit).Assembly.GetTypes(),
            t => t.Name == "SetEnabled");
    }

    [Fact]
    public void ChangingTheTriggerReversesExactly()
    {
        Sequence sequence = Build();
        var was = (MomentTrigger)sequence.Trigger;

        var now = new WhenTrigger("opened");
        now.RequiresAll = true;
        now.Sources.Add("inner");
        now.Sources.Add("outer");

        RoundTrip(sequence, new ChangeTrigger(was, now));
    }

    /// <summary>
    /// Changing what starts a sequence can change how it is started, and that reverses too.
    /// </summary>
    /// <remarks>
    /// One trip to the picker, and pointing at a button instead of a door means the construct
    /// announces the moment rather than Reactor watching for it. That is a different trigger, not
    /// a different field on the same one, so undo has to put the whole thing back.
    /// </remarks>
    [Fact]
    public void ChangingToAMomentTheConstructAnnouncesReversesExactly()
    {
        Sequence sequence = Build();
        var was = (MomentTrigger)sequence.Trigger;

        var now = new RequiredTrigger("pressed-1");
        now.Sources.Add("call");

        RoundTrip(sequence, new ChangeTrigger(was, now));
        Assert.IsType<WhenTrigger>(sequence.Trigger);
    }

    [Fact]
    public void ChangingTheCadenceReversesExactly()
    {
        var program = new Automation();
        var sequence = program.AddSequence(program.AddSystem(), new EveryTrigger(60));
        var edit = new SetCadence(60, 600);

        var before = Shape(sequence);
        edit.Apply(sequence);
        Assert.NotEqual(before, Shape(sequence));
        edit.Undo(sequence);
        Assert.Equal(before, Shape(sequence));
    }

    /// <summary>
    /// An edit that settles on what was already there is not an edit.
    /// </summary>
    /// <remarks>
    /// Going to a picker and coming back without changing your mind must leave nothing behind,
    /// or the offer becomes "undo changing a step" for a step nobody changed.
    /// </remarks>
    [Fact]
    public void SettlingOnWhatWasAlreadyThereIsNotAnEdit()
    {
        Sequence sequence = Build();
        var when = (WhenTrigger)sequence.Trigger;

        var same = new WhenTrigger("pressed");
        same.Sources.Add("call");

        Assert.False(new ChangeStep(0, (Act)sequence.Steps[0], new[] { "inner" }, "close").Changes);
        Assert.False(new ChangeTrigger(when, same).Changes);
        Assert.False(new Rename("Let me out", "Let me out").Changes);
        Assert.False(new MoveStep(1, 1).Changes);
        Assert.False(new SetCadence(60, 60).Changes);
    }
}
