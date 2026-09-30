using System.Text;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using Xunit;

namespace Sprocket.Tests;

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
        text.Append(sequence.Name);

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

    /// <summary>
    /// And for a change to the system, read back through the sequence it is about.
    /// </summary>
    /// <remarks>
    /// What starts a sequence belongs to its system, and is still part of what that sequence is -
    /// so the same flattened shape catches it, and an undo that put back something that merely read
    /// the same would show up as a difference here.
    /// </remarks>
    static void RoundTrip(Sequence sequence, SystemEdit edit)
    {
        var system = sequence.System;
        var before = Shape(sequence);

        edit.Apply(system);
        Assert.NotEqual(before, Shape(sequence));

        edit.Undo(system);
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
        RoundTrip(new ChangeStep(0, (Act)sequence.Steps[0], new[] { "inner", "outer" }, "lock", Form.Native, new[] { Literal.Of(4L) }, true));
    }

    /// <summary>A step changed to something that takes no time loses the wait after it, and undoing puts that wait back.</summary>
    [Fact]
    public void ChangingAStepToOneThatTakesNoTimeDropsItsAwait()
    {
        Sequence sequence = Build();
        var awaited = new Await();
        sequence.Steps.Insert(1, awaited);
        var before = Shape(sequence);

        var edit = new ChangeStep(0, (Act)sequence.Steps[0], new[] { "inner" }, "on", Form.Native, new Literal[0], false);
        edit.Apply(sequence);
        Assert.IsNotType<Await>(sequence.Steps[1]);

        edit.Undo(sequence);
        Assert.Same(awaited, sequence.Steps[1]);
        Assert.Equal(before, Shape(sequence));
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
    /// announces the moment rather than Sprocket watching for it. That is a different trigger, not
    /// a different field on the same one, so undo has to put the whole thing back.
    /// </remarks>
    [Fact]
    public void ChangingToAMomentTheConstructAnnouncesReversesExactly()
    {
        Sequence sequence = Build();
        var was = (MomentTrigger)sequence.Trigger;

        var now = new RequiredTrigger("pressed_1");
        now.Sources.Add("call");

        RoundTrip(sequence, new ChangeTrigger(was, now));
        Assert.IsType<WhenTrigger>(sequence.Trigger);
    }

    [Fact]
    public void ChangingTheCadenceReversesExactly()
    {
        var program = new Automation();
        var every = new EveryTrigger(60);
        var sequence = program.AddSequence(program.AddSystem(), every);

        RoundTrip(sequence, new SetCadence(every, 60, 600));
    }

    /// <summary>
    /// Deleting a trigger reverses, which is what makes deleting one safe to do without asking.
    /// </summary>
    [Fact]
    public void DeletingATriggerReversesExactly()
    {
        Sequence sequence = Build();

        RoundTrip(sequence, new ForgetTrigger(sequence.System, sequence.Trigger));
    }

    /// <summary>
    /// Deleting a trigger the construct answers takes its trigger part and what each instance answered it with, and undoing
    /// puts all three back as they were.
    /// </summary>
    [Fact]
    public void DeletingATriggerTakesItsPartAndBringsItBack()
    {
        var program = new Automation();
        var system = program.AddSystem();
        var sequence = program.AddSequence(system);
        sequence.Steps.Add(StepFor("open", "outer"));

        var part = system.PartFor("call");
        part.IsTrigger = true;
        var trigger = RequiredTrigger.Named("call");
        trigger.Does = new Run(sequence.Number);
        system.AddTrigger(trigger);
        system.First!.Fill("call", new[] { "panel1" }, new[] { new ToolbarSlot(1, 1, 4) });

        var edit = new ForgetTrigger(system, trigger);
        edit.Apply(system);

        Assert.Empty(system.Triggers);
        Assert.Null(system.FindPart("call"));
        Assert.Null(system.First.FilledWith("call"));

        edit.Undo(system);

        Assert.Same(trigger, system.Triggers[0]);
        Assert.Same(part, system.FindPart("call"));
        Assert.Equal(new[] { "panel1" }, system.First.FilledWith("call"));
        Assert.Equal(4, system.First.Positions("call")![0].Slot);
    }

    /// <summary>A system with one slot that has a label and says what it is for, as a blueprint's author leaves it.</summary>
    static Part Slot(out AutomationSystem system)
    {
        var program = new Automation();
        system = program.AddSystem();
        system.Annotation = "Cycles an airlock.";

        var part = system.PartFor("inner");
        part.Annotation = "The door to the inside of the ship.";

        var close = new Act("close");
        close.Targets.Add("inner");
        program.AddSequence(system).Steps.Add(close);
        system.First!.Fill("inner", new[] { "entity1" });
        return part;
    }

    /// <summary>Applies <paramref name="edit" /> to <paramref name="system" />, checks it changed what <paramref name="read" /> reads, and that undoing puts it back.</summary>
    static void RoundTrip(AutomationSystem system, SystemEdit edit, Func<string> read)
    {
        var before = read();

        edit.Apply(system);
        Assert.NotEqual(before, read());
        Assert.True(edit.Undoable(system));

        edit.Undo(system);
        Assert.Equal(before, read());
        Assert.True(edit.Redoable(system));
    }

    [Fact]
    public void DescribingABlueprintReversesExactly()
    {
        _ = Slot(out var system);
        RoundTrip(system, new Annotate(system.Annotation, "Cycles an airlock with **one button**."), () => system.Annotation);
    }

    /// <summary>Renaming a slot renames it everywhere its system names it, keeps what it says, and reverses exactly.</summary>
    [Fact]
    public void RenamingASlotRenamesItEverywhereAndReversesExactly()
    {
        var part = Slot(out var system);
        var close = (Act)system.Sequences[0].Steps[0];
        var edit = new RenamePart(part, "Hatch");

        RoundTrip(system, edit, () => part.Id + " " + close.Targets[0]);

        edit.Apply(system);
        Assert.Same(part, system.FindPart("Hatch"));
        Assert.Equal("Hatch", close.Targets[0]);
        Assert.Equal(new[] { "entity1" }, system.First!.FilledWith("Hatch"));
        Assert.Null(system.First!.FilledWith("inner"));
        Assert.Equal("The door to the inside of the ship.", part.Annotation);
    }

    [Fact]
    public void DescribingASlotReversesExactly()
    {
        var part = Slot(out var system);
        var edit = new AnnotatePart(part, "The door to the **inside** of the ship.", "describing a slot");

        RoundTrip(system, edit, () => part.Annotation);

        edit.Apply(system);
        Assert.Equal("inner", part.Id);
    }

    [Fact]
    public void MakingASlotOptionalReversesExactly()
    {
        var part = Slot(out var system);
        RoundTrip(system, new SetOptional(part, true), () => part.Optional ? "optional" : "required");
    }

    /// <summary>A step back over a slot the system no longer has is dropped rather than taken.</summary>
    [Fact]
    public void ASlotThatIsGoneCannotBeWalkedBack()
    {
        var part = Slot(out var system);
        var edit = new SetOptional(part, true);
        edit.Apply(system);

        var other = new Automation().AddSystem();
        Assert.False(edit.Undoable(other));
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

        Assert.False(new ChangeStep(0, (Act)sequence.Steps[0], new[] { "inner" }, "close", Form.Native, new Literal[0], true).Changes);
        Assert.False(new Rename("Let me out", "Let me out").Changes);
        Assert.False(new MoveStep(1, 1).Changes);

        var part = Slot(out var system);
        Assert.False(new Annotate(system.Annotation, system.Annotation).Changes);
        Assert.False(new RenamePart(part, "inner").Changes);
        Assert.False(new AnnotatePart(part, part.Annotation, "describing a slot").Changes);
        Assert.False(new SetOptional(part, false).Changes);
    }
}
