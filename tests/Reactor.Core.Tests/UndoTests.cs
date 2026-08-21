using System.Text;
using Reactor.Sequences;
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
    static (TestWorld World, Sequence Sequence) Build()
    {
        var world = new TestWorld();
        world.Add(new TestDoor(1, "Inner Door"));
        world.Add(new TestDoor(2, "Outer Door"));
        world.Add(new TestVent(3, "Airlock Vent"));
        world.Add(new TestButton(4, "Airlock Button"));

        var trigger = new WhenTrigger("pressed");
        trigger.Sources.Add(4);

        var sequence = new Sequence(1, trigger);
        sequence.Name = "Let me out";
        sequence.Steps.Add(StepFor("close", 1));
        sequence.Steps.Add(StepFor("depressurize", 3));
        sequence.Steps.Add(StepFor("open", 2));

        return (world, sequence);
    }

    static Step StepFor(string actionId, params long[] targets)
    {
        var step = new Step(actionId);
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
            case EveryTrigger every:
                text.Append(" every ").Append(every.IntervalTicks);
                break;
        }

        foreach (var step in sequence.Steps)
        {
            text.Append(" | ").Append(step.ActionId).Append(':')
                .Append(string.Join("+", step.Targets));

            if (step.Gate is AfterGate after)
                text.Append(" after ").Append(after.Ticks);
        }

        return text.ToString();
    }

    static void RoundTrip(SequenceEdit edit)
    {
        var (_, sequence) = Build();
        var before = Shape(sequence);

        edit.Apply(sequence);
        Assert.NotEqual(before, Shape(sequence));

        edit.Undo(sequence);
        Assert.Equal(before, Shape(sequence));
    }

    [Fact]
    public void AddingAStepReversesExactly() =>
        RoundTrip(new AddStep("adding a step", 3, StepFor("close", 2)));

    [Fact]
    public void AddingAStepInTheMiddleReversesExactly() =>
        RoundTrip(new AddStep("adding a step", 1, StepFor("close", 2)));

    [Fact]
    public void RemovingAStepReversesExactly()
    {
        var (_, sequence) = Build();
        RoundTrip(new RemoveStep(1, sequence.Steps[1]));
    }

    [Fact]
    public void RemovingTheFirstStepReversesExactly()
    {
        var (_, sequence) = Build();
        RoundTrip(new RemoveStep(0, sequence.Steps[0]));
    }

    [Fact]
    public void MovingAStepReversesExactly() => RoundTrip(new MoveStep(0, 2));

    [Fact]
    public void MovingAStepBackwardsReversesExactly() => RoundTrip(new MoveStep(2, 0));

    [Fact]
    public void ChangingAStepReversesExactly()
    {
        var (_, sequence) = Build();
        RoundTrip(new ChangeStep(0, sequence.Steps[0], new long[] { 1, 2 }, "lock"));
    }

    [Fact]
    public void ChangingAWaitReversesExactly()
    {
        var (_, sequence) = Build();
        sequence.Steps[1].Gate = new AfterGate(60);
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
        var (_, sequence) = Build();
        var when = (WhenTrigger)sequence.Trigger;
        RoundTrip(new ChangeTrigger(when, new long[] { 1, 2 }, "opened", true));
    }

    [Fact]
    public void ChangingTheCadenceReversesExactly()
    {
        var world = new TestWorld();
        var sequence = new Sequence(1, new EveryTrigger(60));
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
        var (_, sequence) = Build();
        var when = (WhenTrigger)sequence.Trigger;

        Assert.False(new ChangeStep(0, sequence.Steps[0], new long[] { 1 }, "close").Changes);
        Assert.False(new ChangeTrigger(when, new long[] { 4 }, "pressed", false).Changes);
        Assert.False(new Rename("Let me out", "Let me out").Changes);
        Assert.False(new MoveStep(1, 1).Changes);
        Assert.False(new SetCadence(60, 60).Changes);
    }
}
