using Sprocket.Host.SpaceEngineers.Features.LogicSystems;
using Sprocket.Host.SpaceEngineers.Features.Selector;
using Sprocket.Host.SpaceEngineers.Features.Sequences;
using Sprocket.Host.SpaceEngineers.Features.States;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Text;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using Lumel;
using Lumel.Controls;
using Lumel.Input;
using Lumel.Text;
using VRageMath;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A logic system's states set, tested and watched from the editor's pickers, and its types and states kept on its State tab.</summary>
public class StateEditingTests
{
    const string Head = "sprocket 1 alpha 18\n\n"
        + "system \"3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43\" as Airlock\n{\n"
        + "    type side = inside | outside\n\n"
        + "    let where = inside\n"
        + "    let there = outside\n"
        + "    let cycling = off\n\n";

    /// <summary>The airlock with an empty sequence.</summary>
    const string Program = Head + "    sequence 1 {\n    }\n}\n";

    /// <summary>The airlock with a flag and no type, and an empty sequence.</summary>
    const string Untyped = "sprocket 1 alpha 18\n\n"
        + "system \"3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43\" as Airlock\n{\n"
        + "    let cycling = off\n\n"
        + "    sequence 1 {\n    }\n}\n";

    /// <summary>The airlock with a sequence that sets where.</summary>
    const string Setting = Head + "    sequence 1 {\n        set where to outside\n    }\n}\n";

    /// <summary>Set state asks which state and what to set it to, and adds the step, which reads as the script says it.</summary>
    [Fact]
    public void SetStateAddsTheStep()
    {
        var screen = OnSequence(Program, out var sequence);
        var editor = screen.Editor;

        editor.PressAdd(SentenceText.SetStateWords);
        screen.Run(1);
        PickState(screen, "where");
        Give(screen, "outside");
        Assert.Equal("Set where to outside", editor.Screen.Title());
        editor.PressPrimary();
        screen.Run(1);

        var set = Assert.IsType<SetState>(Assert.Single(sequence.Steps));
        Assert.Equal("where", set.State);
        Assert.Equal("outside", set.Value.Text);
        Assert.Equal("", set.From);
        Assert.Equal("set where to outside", SequenceText.Step(editor.Runner.Bindings, sequence, set, null));
    }

    /// <summary>A state can be set to what another state of its type holds, which the step keeps as that state's name.</summary>
    [Fact]
    public void AStateIsSetToAnotherStatesValue()
    {
        var screen = OnSequence(Program, out var sequence);
        var editor = screen.Editor;

        editor.PressAdd(SentenceText.SetStateWords);
        screen.Run(1);
        PickState(screen, "where");
        Assert.Equal(new[] { "inside", "outside" }, Words(screen));
        Page(screen).Others["there"].Pick(HeldModifier.Alone, 1);
        screen.Run(1);
        Assert.Equal("Set where to there", editor.Screen.Title());
        editor.PressPrimary();
        screen.Run(1);

        var set = Assert.IsType<SetState>(Assert.Single(sequence.Steps));
        Assert.Equal("there", set.From);
    }

    /// <summary>A flag's state is offered on and off, and nothing of another type.</summary>
    [Fact]
    public void AFlagIsOfferedOnAndOff()
    {
        var screen = OnSequence(Program, out _);

        screen.Editor.PressAdd(SentenceText.SetStateWords);
        screen.Run(1);
        PickState(screen, "cycling");

        Assert.Equal(new[] { "on", "off" }, Words(screen));
    }

    /// <summary>If state asks which state, how it compares and with what, and adds an if testing it.</summary>
    [Fact]
    public void IfStateAddsAnIf()
    {
        var screen = OnSequence(Program, out var sequence);
        var editor = screen.Editor;

        editor.PressAdd(SentenceText.IfWord);
        screen.Run(1);
        ToStates(screen);
        PickState(screen, "where");
        Relate(screen, "is not");
        Give(screen, "inside");
        Assert.Equal("If where is not inside", editor.Screen.Title());
        editor.PressPrimary();
        screen.Run(1);

        var test = Assert.IsType<StateTest>(Assert.IsType<If>(sequence.Steps[0]).Test);
        Assert.IsType<EndIf>(sequence.Steps[1]);
        Assert.Equal("where", test.State);
        Assert.Equal(Relation.IsNot, test.Relation);
        Assert.Equal("inside", test.Value.Text);
        Assert.Equal("if where is not inside", SequenceText.Step(editor.Runner.Bindings, sequence, sequence.Steps[0], null));
    }

    /// <summary>The row closing an if adds an otherwise if testing a state, after the branches it already has.</summary>
    [Fact]
    public void AnOtherwiseIfTestsAState()
    {
        var screen = OnSequence(Head + "    sequence 1 {\n        if where == inside then {\n        }\n    }\n}\n", out var sequence);
        var editor = screen.Editor;

        var end = editor.StepRows().Single(row => row.Kind == StepKind.End);
        Assert.Single(end.Adds).Choices!.Single(choice => choice.Text == SentenceText.OtherwiseIfWords).Adds!.Execute();
        screen.Run(1);
        ToStates(screen);
        PickState(screen, "there");
        Give(screen, "inside");
        Assert.Equal("Otherwise if there is inside", editor.Screen.Title());
        editor.PressPrimary();
        screen.Run(1);

        var test = Assert.IsType<StateTest>(Assert.IsType<Else>(sequence.Steps[1]).Test);
        Assert.Equal("there", test.State);
        Assert.Equal("inside", test.Value.Text);
    }

    /// <summary>An if on blocks is made an if on a state from its own selector, its source switched there, and keeps its steps.</summary>
    [Fact]
    public void AnIfOnBlocksBecomesAnIfOnAState()
    {
        var screen = OnSequence(Head.Replace("system ", "let door1 = block 'Door/LargeBlockDoor' [ 2 ]\n\nsystem ")
            + "    sequence 1 {\n        if all sprocket.status on [ door1 ] == closed then {\n            set where to outside\n        }\n    }\n}\n", out var sequence);
        var editor = screen.Editor;

        var row = editor.StepRows()[0];
        row.Slots.OfType<EntitiesSlot>().Single().Opens!.Execute();
        screen.Run(1);
        Assert.IsType<PickPage>(editor.Current);

        ToStates(screen);
        PickState(screen, "where");
        Give(screen, "inside");
        editor.PressPrimary();
        screen.Run(1);

        var test = Assert.IsType<StateTest>(Assert.IsType<If>(sequence.Steps[0]).Test);
        Assert.Equal("where", test.State);
        Assert.IsType<SetState>(sequence.Steps[1]);
        Assert.IsType<EndIf>(sequence.Steps[2]);
        Assert.IsType<SequencePage>(editor.Current);
    }

    /// <summary>The selector an if on a state is changed with opens on States, and switching back to Blocks offers the blocks again.</summary>
    [Fact]
    public void AnIfOnAStateOpensOnStates()
    {
        var screen = OnSequence(Head + "    sequence 1 {\n        if where == inside then {\n        }\n    }\n}\n", out _);
        var editor = screen.Editor;

        editor.StepRows()[0].Slots[0].Opens!.Execute();
        screen.Run(1);
        Assert.IsType<StatePage>(editor.Current);

        Press(screen, SourceBox(screen, editor.Texts.PickSourceStates), editor.Texts.PickSourceBlocks);
        Assert.IsType<PickPage>(editor.Current);
        Assert.NotEmpty(editor.Pick().Stages[0].Entries());
    }

    /// <summary>A step setting a state reads as the state, "to", and its value, and pressing the state opens the picker on what it sets.</summary>
    [Fact]
    public void ASetsRowOpensItsPicker()
    {
        var screen = OnSequence(Setting, out var sequence);
        var editor = screen.Editor;

        var row = Assert.Single(editor.StepRows());
        Assert.Equal(StepKind.Set, row.Kind);
        Assert.Equal("where", Assert.IsType<SystemStateSlot>(row.Slots[0]).Name);
        Assert.Equal("to", SentenceText.Joining(row.Slots, 1));

        row.Slots[0].Opens!.Execute();
        screen.Run(1);
        Assert.Equal("Set where to outside", editor.Screen.Title());

        Give(screen, "inside");
        editor.PressPrimary();
        screen.Run(1);
        Assert.Equal("inside", Assert.IsType<SetState>(Assert.Single(sequence.Steps)).Value.Text);
    }

    /// <summary>The value a step sets a state to is chosen from a menu under it, the value it has marked, as one edit.</summary>
    [Fact]
    public void ASetsValueIsChosenFromAMenuUnderIt()
    {
        var screen = OnSequence(Setting, out var sequence);
        var editor = screen.Editor;

        Assert.Single(editor.StepRows()).Slots[1].Opens!.Execute();
        screen.Run(1);
        Assert.True(editor.Screen.HasQuestion);
        Assert.Equal(new[] { "inside", "outside", editor.Texts.ValueNewSetting, editor.Texts.PickStateValueOf }, editor.Screen.Answers.Select(answer => answer.Text).ToArray());
        Assert.Equal("outside", editor.Screen.Default!.Text);

        editor.PressAnswer("inside");
        screen.Run(1);
        Assert.Equal("inside", Assert.IsType<SetState>(Assert.Single(sequence.Steps)).Value.Text);

        editor.Screen.Undo!.Execute();
        screen.Run(1);
        Assert.Equal("outside", Assert.IsType<SetState>(Assert.Single(sequence.Steps)).Value.Text);
    }

    /// <summary>Pressing a set's value on the screen hangs its menu under it and keeps it there.</summary>
    [Fact]
    public void PressingASetsValueHangsItsMenu()
    {
        var screen = OnSequence(Setting, out _);
        var editor = screen.Editor;
        screen.Run(5);

        var at = screen.Text("outside").At;
        var point = Screen.Ui(at.X + 4f, at.Y + 8f);
        screen.Scene.MoveTo(point);
        screen.Scene.Press(point, 1);
        screen.Run(1);

        Assert.True(editor.Screen.HasQuestion, "the menu put itself away");
        Assert.Equal("outside", editor.Screen.Default!.Text);
    }

    /// <summary>The value of another state, in the menu under a set's value, opens the selector the step was made in.</summary>
    [Fact]
    public void AnotherStatesValueOpensTheSelector()
    {
        var screen = OnSequence(Setting, out _);
        var editor = screen.Editor;

        Assert.Single(editor.StepRows()).Slots[1].Opens!.Execute();
        screen.Run(1);
        editor.PressAnswer(editor.Texts.PickStateValueOf);
        screen.Run(1);
        Assert.Equal("Set where to outside", editor.Screen.Title());
    }

    /// <summary>A state set to another state's value has that state's name for its value, and a menu under it marks the value of another state.</summary>
    [Fact]
    public void AValueTakenFromAnotherStateIsMarkedInTheMenu()
    {
        var screen = OnSequence(Head + "    sequence 1 {\n        set where to there\n    }\n}\n", out var sequence);
        var editor = screen.Editor;

        var row = Assert.Single(editor.StepRows());
        Assert.Equal("there", Assert.IsType<SystemStateSlot>(row.Slots[1]).Name);
        row.Slots[1].Opens!.Execute();
        screen.Run(1);
        Assert.Equal(editor.Texts.PickStateValueOf, editor.Screen.Default!.Text);

        editor.PressAnswer("inside");
        screen.Run(1);
        var set = Assert.IsType<SetState>(Assert.Single(sequence.Steps));
        Assert.Equal("inside", set.Value.Text);
        Assert.Equal("", set.From);
    }

    /// <summary>A flag no other state holds the like of is offered on, off and a new setting, and no other state's value.</summary>
    [Fact]
    public void AFlagsMenuOffersNoOtherStatesValue()
    {
        var screen = OnSequence(Head + "    sequence 1 {\n        set cycling to on\n    }\n}\n", out _);
        var editor = screen.Editor;

        Assert.Single(editor.StepRows()).Slots[1].Opens!.Execute();
        screen.Run(1);
        Assert.Equal(new[] { "on", "off", editor.Texts.ValueNewSetting }, editor.Screen.Answers.Select(answer => answer.Text).ToArray());
    }

    /// <summary>The value an if on a state checks is chosen from a menu under it, as one edit.</summary>
    [Fact]
    public void AnIfsStateValueIsChosenFromAMenuUnderIt()
    {
        var screen = OnSequence(Head + "    sequence 1 {\n        if where == inside then {\n        }\n    }\n}\n", out var sequence);
        var editor = screen.Editor;

        editor.StepRows()[0].Slots[2].Opens!.Execute();
        screen.Run(1);
        Assert.Equal("inside", editor.Screen.Default!.Text);
        editor.PressAnswer("outside");
        screen.Run(1);
        var test = Assert.IsType<StateTest>(((If)sequence.Steps[0]).Test);
        Assert.Equal("outside", test.Value.Text);
        Assert.Equal(Relation.Is, test.Relation);
    }

    /// <summary>The value a when watches a state for is chosen from a menu under it.</summary>
    [Fact]
    public void AWatchedStateValueIsChosenFromAMenuUnderIt()
    {
        var screen = OnSystem(Head + "    when where == outside then run 1\n\n    sequence 1 {\n    }\n}\n", out var system);
        var editor = screen.Editor;

        editor.Triggers()[0].Starts[2].Opens!.Execute();
        screen.Run(1);
        Assert.Equal(new[] { "inside", "outside", editor.Texts.ValueNewSetting, editor.Texts.PickStateValueOf }, editor.Screen.Answers.Select(answer => answer.Text).ToArray());
        editor.PressAnswer("inside");
        screen.Run(1);
        Assert.Equal("inside", Assert.IsType<StateTrigger>(system.Triggers[0]).Test.Value.Text);

        editor.Screen.Undo!.Execute();
        screen.Run(1);
        Assert.Equal("outside", Assert.IsType<StateTrigger>(system.Triggers[0]).Test.Value.Text);
    }

    /// <summary>When state can watch a state made in its own picker, which is added with the trigger as one undo step.</summary>
    [Fact]
    public void WhenStateMakesANewState()
    {
        var screen = OnSystem(Program, out var system);
        var editor = screen.Editor;

        editor.PressAdd(SentenceText.WhenWord);
        screen.Run(1);
        ToStates(screen);
        PickState(screen, editor.Texts.PickNewState);
        Page(screen).Head.Fields[0].Text = "alarm";
        Give(screen, "on");
        Assert.Equal("When alarm is on", editor.Screen.Title());
        Assert.Equal("on", Page(screen).Form.Fields[Page(screen).Form.Count - 1].Text);
        editor.Pick().Stages[3].Entries().First(entry => entry.Text == editor.Texts.PickNewSequence).Pick(HeldModifier.Alone, 1);
        screen.Run(1);
        editor.PressPrimary();
        screen.Run(1);

        Assert.True(system.FindState("alarm")!.IsFlag);
        Assert.Equal("alarm", Assert.IsType<StateTrigger>(system.Triggers.Last()).Test.State);

        editor.Screen.Undo!.Execute();
        screen.Run(1);
        Assert.Null(system.FindState("alarm"));
        Assert.DoesNotContain(system.Triggers, trigger => trigger is StateTrigger);
    }

    /// <summary>When state makes a trigger started as the state comes to hold a value, which runs a new sequence.</summary>
    [Fact]
    public void WhenStateMakesATrigger()
    {
        var screen = Screen.Holding(Program);
        var editor = screen.Editor;
        var system = editor.Program.Systems[0];
        system.Enabled = false;
        editor.Program.Edited();
        editor.Push(new LogicSystemPage(editor, system));
        screen.Run(1);

        editor.PressAdd(SentenceText.WhenWord);
        screen.Run(1);
        ToStates(screen);
        PickState(screen, "where");
        Give(screen, "outside");
        editor.Pick().Stages[2].Entries().First(entry => entry.Text == editor.Texts.PickNewSequence).Pick(HeldModifier.Alone, 1);
        screen.Run(1);
        editor.PressPrimary();
        screen.Run(1);

        var made = Assert.IsType<StateTrigger>(system.Triggers.Last());
        Assert.Equal("where", made.Test.State);
        Assert.Equal("outside", made.Test.Value.Text);
        Assert.Equal(2, system.Sequences.Count);
    }

    /// <summary>The State tab lists each type with its choices and each state with what it holds, starts at and holds now.</summary>
    [Fact]
    public void TheStateTabListsTypesAndStates()
    {
        var screen = OnSystem(Program, out _);
        var view = screen.Editor.PressTab(screen.Editor.Texts.SystemState);
        screen.Run(1);

        var type = Assert.Single(Target.Within(view).OfType<TypeRow>());
        Assert.Equal("side", type.Name.Text);
        Assert.Equal(new[] { "inside", "outside" }, Target.Within(type).OfType<NamePlate>().Skip(1).Select(chip => chip.Text).ToArray());

        var states = Target.Within(view).OfType<StateRow>().ToList();
        Assert.Equal(new[] { "where", "there", "cycling" }, states.Select(row => row.Name.Text).ToArray());
        Assert.Equal("(side)", states[0].Kind);
        Assert.Equal("(" + screen.Editor.Texts.StateFlag + ")", states[2].Kind);
        Assert.Equal("inside", states[0].Start.Text);
        Assert.Equal("inside", states[0].Now);
    }

    /// <summary>
    /// While the logic system is switched off, what a state is now is changed by hand from a menu under it, the value it has
    /// marked, as one edit; nothing watching the state reacts.
    /// </summary>
    [Fact]
    public void AStateIsChangedByHandWhileOff()
    {
        var screen = OnSystem(Head + "    when where == outside then run 1\n\n    sequence 1 {\n    }\n}\n", out var system);
        var editor = screen.Editor;
        var view = editor.PressTab(editor.Texts.SystemState);
        screen.Run(1);

        var where = Target.Within(view).OfType<StateRow>().First();
        Assert.NotNull(where.Current.Command);
        where.Current.Command!.Execute();
        screen.Run(1);
        Assert.Equal(new[] { "inside", "outside" }, editor.Screen.Answers.Select(answer => answer.Text).ToArray());
        Assert.Equal("inside", editor.Screen.Default!.Text);

        editor.PressAnswer("outside");
        screen.Run(1);
        var shown = editor.StandIn(system);
        Assert.Equal("outside", editor.Runner.States.Read(system, shown, system.FindState("where")!).Text);
        Assert.Equal("outside", where.Now);
        Assert.False(editor.Runner.Get(system.Sequences[0], shown)?.IsRunning ?? false, "a when on the state reacted to a change by hand");

        editor.Screen.Undo!.Execute();
        screen.Run(1);
        Assert.Equal("inside", editor.Runner.States.Read(system, shown, system.FindState("where")!).Text);
        Assert.Equal("inside", where.Now);
    }

    /// <summary>While the logic system and the installation are both switched on, what a state is now cannot be pressed; switching the installation off lets it be.</summary>
    [Fact]
    public void AStateIsChangedByHandOnlyWhileSomethingIsOff()
    {
        var screen = OnSystem(Program, out var system);
        var editor = screen.Editor;
        var view = editor.PressTab(editor.Texts.SystemState);
        system.Enabled = true;
        editor.Program.Edited();
        screen.Run(2);

        var where = Target.Within(view).OfType<StateRow>().First();
        Assert.Null(where.Current.Command);

        editor.StandIn(system).Enabled = false;
        screen.Run(2);
        Assert.NotNull(where.Current.Command);
        where.Current.Command!.Execute();
        screen.Run(1);
        editor.PressAnswer("outside");
        screen.Run(1);
        Assert.Equal("outside", editor.Runner.States.Read(system, editor.StandIn(system), system.FindState("where")!).Text);
    }

    /// <summary>A type and a state are added under names counted on from how many there are; a new type has two choices.</summary>
    [Fact]
    public void TypesAndStatesAreAdded()
    {
        var screen = OnSystem(Program, out var system);
        var editor = screen.Editor;
        editor.PressTab(editor.Texts.SystemState);
        screen.Run(1);

        editor.PressAdd(editor.Texts.StateTypeAdd);
        screen.Run(1);
        editor.PressAdd(editor.Texts.StateStateAdd);
        screen.Run(1);

        Assert.Equal("state type 2", system.Types[1].Name);
        Assert.Equal(new[] { "choice 1", "choice 2" }, system.Types[1].Choices.ToArray());
        Assert.Equal("state 4", system.States[3].Name);
        Assert.True(system.States[3].IsFlag);
    }

    /// <summary>The state being carried is highlighted while it is held, as a step is, and the highlight goes as it is dropped.</summary>
    [Fact]
    public void TheStateBeingCarriedIsHighlighted()
    {
        var screen = OnSystem(Program, out _);
        var view = screen.Editor.PressTab(screen.Editor.Texts.SystemState);
        screen.Run(1);
        var row = Target.Within(view).OfType<StateRow>().ToList()[1];
        var grab = row.DragHandle.ToScreen(row.DragHandle.Bounds.Center);

        screen.Scene.MoveTo(grab);
        screen.Scene.Down(grab);
        screen.Scene.Move(new Vector2(grab.X, grab.Y + 10f));
        screen.Run(1);
        Assert.Equal(Palette.SlotFill, row.Background);

        screen.Scene.Up();
        screen.Run(1);
        Assert.NotEqual(Palette.SlotFill, row.Background);
    }

    /// <summary>An empty list leaves no gap between its heading and its add button.</summary>
    [Fact]
    public void AnEmptyListTakesNoRoom()
    {
        var screen = OnSystem(Untyped, out _);
        var tab = screen.Editor.PressTab(screen.Editor.Texts.SystemState);
        screen.Run(1);

        Assert.False(Target.Within(tab).OfType<StatePanel>().Single().Types.IsVisible, "an empty list still takes a gap");
    }

    /// <summary>A state something uses has no bin to press, and says why; one nothing uses can be removed.</summary>
    [Fact]
    public void AStateInUseCannotBeRemoved()
    {
        var screen = OnSystem(Setting, out var system);
        var editor = screen.Editor;
        var view = editor.PressTab(editor.Texts.SystemState);
        screen.Run(1);

        var states = Target.Within(view).OfType<StateRow>().ToList();
        Assert.Null(states[0].Removes);
        Assert.NotNull(states[2].Removes);

        states[2].Removes!.Execute();
        screen.Run(1);
        Assert.Null(system.FindState("cycling"));
    }

    /// <summary>The value a state starts as is chosen among the values of the type it holds, and nothing else.</summary>
    [Fact]
    public void AStartIsChosenAmongItsOwnTypesValues()
    {
        var screen = OnSystem(Program, out var system);
        var editor = screen.Editor;
        var view = editor.PressTab(editor.Texts.SystemState);
        screen.Run(1);

        Target.Within(view).OfType<StateRow>().Last().Start.Command!.Execute();
        screen.Run(1);
        Assert.Equal(new[] { "on", "off" }, editor.Screen.Answers.Select(answer => answer.Text).ToArray());

        editor.PressAnswer("on");
        screen.Run(1);
        Assert.Equal("on", system.FindState("cycling")!.Start.Said);
    }

    /// <summary>The type a state holds is chosen on its own, and the state then starts as the type's first value.</summary>
    [Fact]
    public void ChoosingATypeStartsTheStateAsItsFirstValue()
    {
        var screen = OnSystem(Program, out var system);
        var editor = screen.Editor;
        var view = editor.PressTab(editor.Texts.SystemState);
        screen.Run(1);

        Target.Within(view).OfType<StateRow>().Last().TypeButton.Command!.Execute();
        screen.Run(1);
        Assert.Equal(new[] { editor.Texts.StateFlag, "side", StatusRow.Plus + editor.Texts.PickNewType }, editor.Screen.Answers.Select(answer => answer.Text).ToArray());

        editor.PressAnswer("side");
        screen.Run(1);
        var cycling = system.FindState("cycling")!;
        Assert.Equal("side", cycling.Type);
        Assert.Equal("inside", cycling.Start.Text);
    }

    /// <summary>A state's box makes a type for it, and the state and the type go back together as one undo step.</summary>
    [Fact]
    public void AStatesBoxMakesANewTypeForIt()
    {
        var screen = OnSystem(Untyped, out var system);
        var editor = screen.Editor;
        var view = editor.PressTab(editor.Texts.SystemState);
        screen.Run(1);

        Target.Within(view).OfType<StateRow>().Single().TypeButton.Command!.Execute();
        screen.Run(1);
        editor.PressAnswer(StatusRow.Plus + editor.Texts.PickNewType);
        screen.Run(1);

        var cycling = system.FindState("cycling")!;
        var type = Assert.Single(system.Types);
        Assert.Equal(type.Name, cycling.Type);
        Assert.Equal(type.Choices[0], cycling.Start.Text);

        editor.Screen.Undo!.Execute();
        screen.Run(1);

        Assert.Empty(system.Types);
        Assert.True(cycling.IsFlag);
    }

    /// <summary>
    /// A new state is made in the picker that sets one: named, given the value it starts at, which decides what it holds, and
    /// added with the step as one undo step.
    /// </summary>
    [Fact]
    public void SetStateMakesANewState()
    {
        var screen = OnSequence(Program, out var sequence);
        var editor = screen.Editor;
        var system = editor.Program.Systems[0];

        editor.PressAdd(SentenceText.SetStateWords);
        screen.Run(1);
        PickState(screen, editor.Texts.PickNewState);
        var form = Page(screen).Head;
        Assert.Equal("state 4", form.Fields[0].Text);

        form.Fields[0].Text = "door";
        Press(screen, form.Fields[1], "side");
        Press(screen, Page(screen).Starts.Fields[0], "outside");
        Give(screen, "inside");
        Assert.Equal("Set door to inside", editor.Screen.Title());
        editor.PressPrimary();
        screen.Run(1);

        var door = system.FindState("door")!;
        Assert.Equal("side", door.Type);
        Assert.Equal("outside", door.Start.Text);
        var set = Assert.IsType<SetState>(Assert.Single(sequence.Steps));
        Assert.Equal("door", set.State);
        Assert.Equal("inside", set.Value.Text);

        editor.Screen.Undo!.Execute();
        screen.Run(1);
        Assert.Empty(sequence.Steps);
        Assert.Null(system.FindState("door"));
    }

    /// <summary>
    /// Where a flag is set or tested and no other flag state is offered, its value is a switch, which holds the side it shows:
    /// a new state can be finished without touching it.
    /// </summary>
    [Fact]
    public void AFlagsSwitchHoldsWhatItShows()
    {
        var screen = OnSequence("sprocket 1 alpha 18\n\nsystem \"3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43\" as Airlock\n{\n"
            + "    sequence 1 {\n    }\n}\n", out var sequence);
        var editor = screen.Editor;

        editor.PressAdd(SentenceText.IfWord);
        screen.Run(1);
        ToStates(screen);
        PickState(screen, editor.Texts.PickNewState);

        Assert.IsType<SwitchField>(Page(screen).Form.Fields[Page(screen).Form.Count - 1]);
        Assert.True(Page(screen).CanPrimary);
        editor.PressPrimary();
        screen.Run(1);

        var test = Assert.IsType<StateTest>(Assert.IsType<If>(sequence.Steps[0]).Test);
        Assert.Equal("state 1", test.State);
        Assert.False(test.Value.Flag);
    }

    /// <summary>
    /// A new state can hold a new type, made from the end of its starts list and shown as the State tab shows a type; the type,
    /// the state and the step are added as one undo step.
    /// </summary>
    [Fact]
    public void ANewStateGetsANewType()
    {
        var screen = OnSequence(Program, out var sequence);
        var editor = screen.Editor;
        var system = editor.Program.Systems[0];

        editor.PressAdd(SentenceText.SetStateWords);
        screen.Run(1);
        PickState(screen, editor.Texts.PickNewState);
        Press(screen, Page(screen).Head.Fields[1], StatusRow.Plus + editor.Texts.PickNewType);

        var page = Page(screen);
        Assert.Equal(StatusRow.Plus + editor.Texts.PickNewType, page.Head.Fields[1].Text);
        Assert.Equal("state type 2", page.Head.Fields[2].Text);
        Assert.Equal("choice 1", page.Starts.Fields[0].Text);
        page.AddChoice.Command!.Execute();
        screen.Run(1);
        Assert.Equal(new[] { "choice 1", "choice 2", "choice 3" }, StateValueQuestion.Fields(editor.Texts, system,
            new State("x", "state type 2", Literal.Name("choice 1")), false, new DeclaredType("state type 2") { Choices = { "choice 1", "choice 2", "choice 3" } })[0].Words.ToArray());

        Give(screen, "choice 3");
        Assert.Equal("Set state 4 to choice 3", editor.Screen.Title());
        editor.PressPrimary();
        screen.Run(1);

        var type = system.FindType("state type 2")!;
        Assert.Equal(new[] { "choice 1", "choice 2", "choice 3" }, type.Choices.ToArray());
        var state = system.FindState("state 4")!;
        Assert.Equal("state type 2", state.Type);
        Assert.Equal("choice 1", state.Start.Text);
        Assert.Equal("choice 3", Assert.IsType<SetState>(Assert.Single(sequence.Steps)).Value.Text);

        editor.Screen.Undo!.Execute();
        screen.Run(1);
        Assert.Empty(sequence.Steps);
        Assert.Null(system.FindState("state 4"));
        Assert.Null(system.FindType("state type 2"));
    }

    /// <summary>A new state may not take a name the logic system already has, and the pick waits until it has one of its own.</summary>
    [Fact]
    public void ANewStateNeedsAFreeName()
    {
        var screen = OnSequence(Program, out _);
        var editor = screen.Editor;

        editor.PressAdd(SentenceText.IfWord);
        screen.Run(1);
        ToStates(screen);
        PickState(screen, editor.Texts.PickNewState);
        var form = Page(screen).Head;
        form.Fields[0].Text = "where";
        Give(screen, "on");

        Assert.False(Page(screen).CanPrimary);
        Assert.Equal(editor.Texts.PickStateNameTaken, editor.Pick().Stages[1].Note);
    }

    /// <summary>The values a new state can start at are the ones the State tab offers for a state nothing uses.</summary>
    [Fact]
    public void ANewStateStartsAtWhatTheTabOffers()
    {
        var screen = OnSequence(Program, out _);
        var editor = screen.Editor;
        editor.PressAdd(SentenceText.SetStateWords);
        screen.Run(1);
        PickState(screen, editor.Texts.PickNewState);

        foreach (var start in new[] { "on", "off" })
            Press(screen, Page(screen).Starts.Fields[0], start);

        Press(screen, Page(screen).Head.Fields[1], "side");
        foreach (var start in new[] { "inside", "outside" })
            Press(screen, Page(screen).Starts.Fields[0], start);
    }

    /// <summary>A state something uses is offered only its own type's values, so a new start cannot change what it holds.</summary>
    [Fact]
    public void AStateInUseKeepsItsType()
    {
        var screen = OnSystem(Setting, out _);
        var editor = screen.Editor;
        var view = editor.PressTab(editor.Texts.SystemState);
        screen.Run(1);

        Target.Within(view).OfType<StateRow>().First().Start.Command!.Execute();
        screen.Run(1);

        Assert.Equal(new[] { "inside", "outside" }, editor.Screen.Answers.Select(answer => answer.Text).ToArray());
    }

    /// <summary>The new type's name typed in the state selector, and a choice's typed over at a press on it, are taken.</summary>
    [Fact]
    public void ANewTypeIsRenamedInTheSelector()
    {
        var screen = OnSequence(Program, out _);
        var editor = screen.Editor;
        var system = editor.Program.Systems[0];

        editor.PressAdd(SentenceText.SetStateWords);
        screen.Run(1);
        PickState(screen, editor.Texts.PickNewState);
        Press(screen, Page(screen).Head.Fields[1], StatusRow.Plus + editor.Texts.PickNewType);

        var page = Page(screen);
        TypeInto(screen, page.Head.Fields[2], "door");

        var chip = page.Choices["choice 1"];
        chip.Command!.Execute();
        screen.Run(1);
        Assert.True(chip.IsRenaming, "a choice of two is renamed at a press");
        chip.Box.Text = "open";
        editor.Input!.Focus(null);
        screen.Run(2);

        Assert.Equal("door", page.Head.Fields[2].Text);
        Assert.Equal("open", page.Starts.Fields[0].Text);
        Give(screen, "open");
        editor.PressPrimary();
        screen.Run(1);

        Assert.Equal(new[] { "open", "choice 2" }, system.FindType("door")!.Choices.ToArray());
    }

    /// <summary>
    /// A state named like one of the choices is offered as a state, apart from the choices, and choosing it sets the state
    /// from that state rather than to the choice.
    /// </summary>
    [Fact]
    public void AStateNamedLikeAChoiceIsOfferedApartAndSetFrom()
    {
        const string program = "sprocket 1 alpha 18\n\n"
            + "system \"3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43\" as Airlock\n{\n"
            + "    type side = inside | outside\n\n"
            + "    let where = inside\n"
            + "    let inside = outside\n\n"
            + "    sequence 1 {\n    }\n}\n";
        var screen = OnSequence(program, out var sequence);
        screen.Scene.Speak("no");
        screen.Run(1);
        var editor = screen.Editor;

        editor.PressAdd(SentenceText.SetStateWords);
        screen.Run(1);
        PickState(screen, "where");

        Assert.Equal(new[] { "inside", "outside" }, Words(screen));

        Page(screen).Others["inside"].Pick(HeldModifier.Alone, 1);
        screen.Run(1);
        editor.PressPrimary();
        screen.Run(1);

        var set = Assert.IsType<SetState>(Assert.Single(sequence.Steps));
        Assert.Equal("inside", set.From);
    }

    /// <summary>A type's name typed over is taken as the keyboard leaves it, and reaches the states that hold it.</summary>
    [Fact]
    public void ATypeIsRenamed()
    {
        var screen = OnSystem(Program, out var system);
        var editor = screen.Editor;
        var view = editor.PressTab(editor.Texts.SystemState);
        screen.Run(1);

        var name = Target.Within(view).OfType<TypeRow>().Single().Name;
        name.Command!.Execute();
        screen.Run(1);
        name.Box.Text = "place";
        editor.Input!.Focus(null);
        screen.Run(1);

        Assert.Equal("place", system.Types[0].Name);
        Assert.Equal("place", system.FindState("where")!.Type);
    }

    /// <summary>A state is refused a name another state has in another case, takes its own name in another case, and may share a type's.</summary>
    [Theory]
    [InlineData("THERE", "where")]
    [InlineData("Where", "Where")]
    [InlineData("SIDE", "SIDE")]
    public void AStatesNameIsCheckedInAnyCase(string typed, string after)
    {
        var screen = OnSystem(Program, out var system);
        var editor = screen.Editor;
        var view = editor.PressTab(editor.Texts.SystemState);
        screen.Run(1);

        var name = Target.Within(view).OfType<StateRow>().First().Name;
        name.Command!.Execute();
        screen.Run(1);
        name.Box.Text = typed;
        name.Box.Submit!.Execute();
        screen.Run(1);

        Assert.Equal(after, system.States[0].Name);
    }

    /// <summary>A type's two choices can only be renamed, so a press renames one; once it has three, a press offers renaming and removing.</summary>
    [Fact]
    public void ATypesChoicesAreRenamedAtAPressUntilThereAreThree()
    {
        var screen = OnSystem(Program, out var system);
        var editor = screen.Editor;
        var view = editor.PressTab(editor.Texts.SystemState);
        screen.Run(1);

        var row = Target.Within(view).OfType<TypeRow>().Single();
        var inside = row.ChoiceChips["inside"];
        inside.Command!.Execute();
        screen.Run(1);
        Assert.True(inside.IsRenaming, "a choice of two is renamed at a press");
        editor.Input!.Key(VRage.Input.MyKeys.Escape, false, false);
        screen.Run(1);

        row.AddChoice.Command!.Execute();
        screen.Run(1);
        row = Target.Within(view).OfType<TypeRow>().Single();
        row.ChoiceChips[system.Types[0].Choices[2]].Command!.Execute();
        screen.Run(1);

        Assert.Equal(new[] { editor.Texts.StateRename, editor.Texts.StateRemoveChoice }, editor.Screen.Answers.Select(answer => answer.Text).ToArray());
    }

    /// <summary>A state's name another state has is refused as a heading's is: Enter keeps the box, and the state keeps its name.</summary>
    [Fact]
    public void AStateNameTakenIsRefused()
    {
        var screen = OnSystem(Program, out var system);
        var editor = screen.Editor;
        var view = editor.PressTab(editor.Texts.SystemState);
        screen.Run(1);

        var name = Target.Within(view).OfType<StateRow>().Single(row => row.Name.Text == "there").Name;
        name.Command!.Execute();
        screen.Run(1);
        name.Box.Text = "where";
        editor.Input!.Key(VRage.Input.MyKeys.Enter, false, false);
        screen.Run(1);

        Assert.True(name.IsRenaming, "the box was put away");
        Assert.NotNull(system.FindState("there"));
    }

    static Screen OnSystem(string program, out AutomationSystem system)
    {
        var screen = Screen.Holding(program);
        var editor = screen.Editor;
        system = editor.Program.Systems[0];
        system.Enabled = false;
        editor.Program.Edited();
        editor.Push(new LogicSystemPage(editor, system));
        screen.Run(1);
        return screen;
    }

    static Screen OnSequence(string program, out Sequence sequence)
    {
        var screen = Screen.Holding(program);
        var editor = screen.Editor;
        var system = editor.Program.Systems[0];
        sequence = system.Sequences[0];
        system.Enabled = false;
        editor.Program.Edited();
        editor.Push(new SequencePage(editor, sequence));
        screen.Run(1);
        return screen;
    }

    /// <summary>The Source box heading the selector's first column, showing <paramref name="shows" />.</summary>
    static ListField SourceBox(Screen screen, string shows) =>
        Target.Within(screen.Editor.Pick().Stages[0]).OfType<ListField>().First(field => field.Text == shows);

    /// <summary>Switches the selector showing, on blocks, to the logic system's states, through its Source box.</summary>
    static void ToStates(Screen screen)
    {
        var texts = screen.Editor.Texts;
        Press(screen, SourceBox(screen, texts.PickSourceBlocks), texts.PickSourceStates);
        Assert.IsType<StatePage>(screen.Editor.Current);
    }

    static void PickState(Screen screen, string name)
    {
        screen.Editor.Pick().Stages[0].Entries().First(entry => entry.Text == name).Pick(HeldModifier.Alone, 1);
        screen.Run(1);
    }

    static StatePage Page(Screen screen) => Assert.IsType<StatePage>(screen.Editor.Current);

    /// <summary>The words the value of the state chosen is given in.</summary>
    static string[] Words(Screen screen)
    {
        var page = Page(screen);
        var fields = StateValueQuestion.Fields(screen.Editor.Texts, screen.Editor.Program.Systems[0], screen.Editor.Program.Systems[0].FindState(Chosen(screen))!, page.Form.Count > 1);
        return fields[fields.Count - 1].Words.ToArray();
    }

    static string Chosen(Screen screen) => screen.Editor.Pick().Stages[0].Entries().First(entry => entry.IsChosen).Text;

    /// <summary>Presses <paramref name="word" /> among the value's words.</summary>
    static void Give(Screen screen, string word)
    {
        var form = Page(screen).Form;
        Press(screen, form.Fields[form.Count - 1], word);
    }

    /// <summary>Presses <paramref name="relation" /> among the ways the state can be compared.</summary>
    static void Relate(Screen screen, string relation) => Press(screen, Page(screen).Form.Fields[0], relation);

    /// <summary>Types <paramref name="text" /> into <paramref name="field" />, a box, in place of what it holds, as somebody would.</summary>
    static void TypeInto(Screen screen, IField field, string text)
    {
        var box = Target.Within((Element)field).OfType<TextBox>().Single();
        box.Text = "";
        foreach (var c in text)
            ((IInputTarget)box).Text(c);

        screen.Run(1);
    }

    /// <summary>Chooses <paramref name="word" /> in <paramref name="field" />: presses its button, flips its switch, or in a list, types its first letter until it shows.</summary>
    static void Press(Screen screen, IField field, string word)
    {
        if (field is ListField)
        {
            for (var i = 0; i < 16 && field.Text != word; i++)
            {
                ((IInputTarget)field).Text(word[0]);
                screen.Run(1);
            }

            Assert.Equal(word, field.Text);
            return;
        }

        // A choice between on and off and nothing else is a switch, flipped to the word wanted.
        if (field is SwitchField)
        {
            if (field.Text != word)
                Target.Within((Element)field).OfType<Switch>().Single().Command!.Execute();

            screen.Run(1);
            Assert.Equal(word, field.Text);
            return;
        }

        Target.Within((Element)field).OfType<Button>().First(button => button.Text == word).Command!.Execute();
        screen.Run(1);
    }
}
