using Sprocket.Host.SpaceEngineers.Features.Expressions;
using Sprocket.Host.SpaceEngineers.Features.LogicSystems;
using Sprocket.Host.SpaceEngineers.Features.Sequences;
using Sprocket.Host.SpaceEngineers.Framework.Language.Reading;
using Sprocket.Host.SpaceEngineers.Framework.Language.Writing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Model.Expressions;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// Every value that offers Expression… under its mark takes an expression built on the expression page, and the program saved
/// holds it as the expression, read back as one.
/// </summary>
public class ExpressionEntryTests
{
    /// <summary>A wait's length built as an expression is saved as the expression.</summary>
    [Fact]
    public void AWaitsLengthIsBuiltAsAnExpression()
    {
        var sim = ExpressionEditorTests.Arm("wait 1s", out _, out var sequence);
        sim.Editor.StepRows()[0].Slots.OfType<DurationSlot>().Single().Opens!.Execute();
        sim.Tick();
        var panel = Assert.IsType<SequencePage>(sim.Editor.Current).Lengths!;
        panel.Mark.Command!.Execute();
        sim.Tick();
        sim.Editor.PressAnswer(sim.Editor.Texts.ValueExpression);
        sim.Tick();

        Build(sim, Times("Pause"));

        var wait = Assert.IsType<Wait>(Assert.Single(sequence.Steps));
        Assert.Equal("Pause * 2", wait.Expression?.Written);
        var read = Assert.IsType<Wait>(Assert.Single(Reloaded(sim).Systems[0].Sequences[0].Steps));
        Assert.Equal("Pause * 2", read.Expression?.Written);
        Assert.Contains("Pause × 2", sim.Editor.Screen.UndoText);
    }

    /// <summary>A clock's interval built as an expression is saved as the expression.</summary>
    [Fact]
    public void AClocksIntervalIsBuiltAsAnExpression()
    {
        var sim = ExpressionEditorTests.Loaded("every 1s run 1", out var system);
        sim.Editor.Triggers()[0].Starts.OfType<DurationSlot>().Single().Opens!.Execute();
        sim.Tick();
        var panel = Assert.IsType<LogicSystemPage>(sim.Editor.Current).Rate!;
        panel.Mark.Command!.Execute();
        sim.Tick();
        sim.Editor.PressAnswer(sim.Editor.Texts.ValueExpression);
        sim.Tick();

        Build(sim, Times("Pause"));

        var every = Assert.IsType<EveryTrigger>(Assert.Single(system.Triggers));
        Assert.Equal("Pause * 2", every.IntervalExpression?.Written);
        Assert.Equal("Pause * 2", Assert.IsType<EveryTrigger>(Assert.Single(Reloaded(sim).Systems[0].Triggers)).IntervalExpression?.Written);
    }

    /// <summary>What an if compares with, built as an expression, is saved as the expression.</summary>
    [Fact]
    public void WhatAnIfComparesWithIsBuiltAsAnExpression()
    {
        var sim = ExpressionEditorTests.Arm("if all sprocket.position on [ arm ] > 1 then {\n            wait 1s\n        }", out _, out var sequence);
        sim.Editor.StepRows()[0].Slots.OfType<ArgumentsSlot>().Single(part => part.Holds == ArgumentRole.Checked).Opens!.Execute();
        sim.Tick();
        var giving = Assert.IsType<SequencePage>(sim.Editor.Current).Giving!;
        Expression(sim, giving);

        Build(sim, Times("Reach"));
        giving.Done();
        sim.Tick();

        var test = sequence.Steps.OfType<If>().Single().Compared!;
        Assert.Equal("Reach * 2", test.Value.Tree?.Written);
        Assert.Equal("Reach * 2", Reloaded(sim).Systems[0].Sequences[0].Steps.OfType<If>().Single().Compared!.Value.Tree?.Written);
        var words = Framework.Text.SequenceText.Step(sim.Runner.Bindings, sequence, sequence.Steps[0], sequence.System.First, true, expression => ExpressionShown.Of(sim.Editor, sequence.System, expression));
        Assert.Contains("Reach × 2", words);
    }

    /// <summary>What a watcher compares with, built as an expression, is saved as the expression.</summary>
    [Fact]
    public void WhatAWatcherComparesWithIsBuiltAsAnExpression()
    {
        var sim = ExpressionEditorTests.Loaded("when all sprocket.position on [ arm ] > 1 then run 1", out var system);
        sim.Editor.Triggers()[0].Starts.OfType<ArgumentsSlot>().Single(part => part.Holds == ArgumentRole.Checked).Opens!.Execute();
        sim.Tick();
        var giving = Assert.IsType<LogicSystemPage>(sim.Editor.Current).Giving!;
        Expression(sim, giving);

        Build(sim, Times("Reach"));
        giving.Done();
        sim.Tick();

        var when = Assert.IsType<WhenTrigger>(Assert.Single(system.Triggers));
        Assert.Equal("Reach * 2", when.Value.Tree?.Written);
        Assert.Equal("Reach * 2", Assert.IsType<WhenTrigger>(Assert.Single(Reloaded(sim).Systems[0].Triggers)).Value.Tree?.Written);
    }

    /// <summary>What a step sets a state to, built as an expression, is offered last under its part and saved as the expression.</summary>
    [Fact]
    public void WhatAStateIsSetToIsBuiltAsAnExpression()
    {
        var sim = ExpressionEditorTests.Loaded("every 1s run 1", out var system);
        var sequence = system.Sequences[0];
        sequence.Steps.Add(new SetState { State = "lit", Value = Literal.Of(true) });
        sim.Editor.Program.Edited();
        sim.Editor.Invalidate();
        sim.Tick();
        sim.Editor.Push(new SequencePage(sim.Editor, sequence));
        for (var i = 0; i < 5; i++)
            sim.Tick();

        sim.Editor.StepRows()[1].Slots.OfType<ArgumentsSlot>().Single(part => part.Holds == ArgumentRole.NewValue).Opens!.Execute();
        sim.Tick();
        Assert.Equal(sim.Editor.Texts.ValueExpression, sim.Editor.Screen.Answers.Last().Text);
        sim.Editor.PressAnswer(sim.Editor.Texts.ValueExpression);
        sim.Tick();
        var stopped = new BlockReading(new Framework.Game.Members.MemberKey("sprocket", "stopped"), Combination.None, new List<Literal>(), new List<string> { "arm" });
        Build(sim, draft => draft.Fill((DraftHole)draft.Root, new DraftValue(stopped)));

        var set = Assert.IsType<SetState>(sequence.Steps[1]);
        Assert.Equal("sprocket.stopped on [ arm ]", set.Expression?.Written);
        Assert.Equal(set.Expression?.Written, Assert.IsType<SetState>(Reloaded(sim).Systems[0].Sequences[0].Steps[1]).Expression?.Written);
        Assert.DoesNotContain("sprocket.", sim.Editor.Screen.UndoText);
        Assert.Contains("arm", sim.Editor.Screen.UndoText);
        sim.Editor.Undo(system, sequence);
        Assert.Null(set.Expression);
        Assert.True(set.Value.Flag);
    }

    /// <summary>A text calculated for a panel shows on the panel as what it comes to once the step runs.</summary>
    [Fact]
    public void ATextCalculatedForAPanelShowsWhatItComesTo()
    {
        var sim = Simulation.Fresh();
        var board = sim.Machine.TextPanel(60, "Board");
        sim.Tick();
        var program = "sprocket 3 alpha 1\n\n"
            + "let 'Board' = block 'TextPanel/LargeTextPanel' [ 60, [ (60 0 0) ], \"Board\" ]\n\n"
            + "system blueprint \"0b0c8f3e-5d5e-4a43-9a51-6f1c2d7e9a10\" as Malware.Sign\n{\n"
            + "    required slot board expects sprocket.screen\n"
            + "    optional slot Reach expects real = 5\n\n"
            + "    sequence 1 {\n        call sprocket.show_text(\"Shut\") on [ board ]\n    }\n}\n\n"
            + "instance 1 of \"0b0c8f3e-5d5e-4a43-9a51-6f1c2d7e9a10\" as 'Installation 1' {\n    board = [ sprocket.get_screen(1) on [ 'Board' ] ]\n}\n";
        Assert.Equal("", ProgramReader.Load(program, sim.Editor.Program, sim.World.Values));
        var system = sim.Editor.Program.Systems[0];
        var sequence = system.Sequences[0];
        system.Enabled = false;
        sim.Editor.Program.Edited();
        sim.Editor.Invalidate();
        sim.Tick();
        sim.Editor.Push(new SequencePage(sim.Editor, sequence));
        for (var i = 0; i < 5; i++)
            sim.Tick();

        sim.Editor.StepRows()[0].Slots.OfType<ActionSlot>().First().Opens!.Execute();
        sim.Tick();
        var form = sim.Editor.Pick().Arguments!.Form;
        form.MarkOf(0)!.Command!.Execute();
        sim.Tick();
        sim.Editor.PressAnswer(sim.Editor.Texts.ValueExpression);
        sim.Tick();
        Build(sim, draft =>
        {
            draft.Fill((DraftHole)draft.Root, new DraftJoin(JoinKind.Add));
            draft.Fill((DraftHole)((DraftJoin)draft.Root).Parts[0], new DraftValue(new Operand(Literal.Of("Reach "))));
            draft.Fill((DraftHole)((DraftJoin)draft.Root).Parts[1], new DraftValue(new Operand(Literal.Setting("Reach"))));
        });
        sim.Editor.Current.Primary();
        sim.Tick();

        system.Enabled = true;
        sim.Editor.Program.Edited();
        for (var i = 0; i < 3; i++)
            sim.Tick();
        sim.Runner.Call(sequence, system.First);
        for (var i = 0; i < 5; i++)
            sim.Tick();

        Assert.Equal("Reach 5", board.Text);
    }

    /// <summary>A text built of words around the average of some pistons' position shows on the panel as what it comes to once the step runs, and is saved as the expression.</summary>
    [Fact]
    public void ATextAroundAReadingShowsWhatItComesTo()
    {
        var sim = Simulation.Fresh();
        var board = sim.Machine.TextPanel(60, "Board");
        sim.Machine.Piston(61, "Arm Piston");
        sim.Tick();
        var program = "sprocket 3 alpha 1\n\n"
            + "let 'Board' = block 'TextPanel/LargeTextPanel' [ 60, [ (60 0 0) ], \"Board\" ]\n"
            + "let 'Arm Piston' = block 'ExtendedPistonBase/LargePistonBase' [ 61, [ (61 0 0) ], \"Arm Piston\" ]\n\n"
            + "system blueprint \"0b0c8f3e-5d5e-4a43-9a51-6f1c2d7e9a10\" as Malware.Sign\n{\n"
            + "    required slot board expects sprocket.screen\n"
            + "    required slot tanks expects ExtendedPistonBase\n\n"
            + "    sequence 1 {\n        call sprocket.show_text(\"Shut\") on [ board ]\n    }\n}\n\n"
            + "instance 1 of \"0b0c8f3e-5d5e-4a43-9a51-6f1c2d7e9a10\" as 'Installation 1' {\n    board = [ sprocket.get_screen(1) on [ 'Board' ] ]\n    tanks = [ 'Arm Piston' ]\n}\n";
        Assert.Equal("", ProgramReader.Load(program, sim.Editor.Program, sim.World.Values));
        var system = sim.Editor.Program.Systems[0];
        var sequence = system.Sequences[0];
        system.Enabled = false;
        sim.Editor.Program.Edited();
        sim.Editor.Invalidate();
        sim.Tick();
        sim.Editor.Push(new SequencePage(sim.Editor, sequence));
        for (var i = 0; i < 5; i++)
            sim.Tick();

        sim.Editor.StepRows()[0].Slots.OfType<ActionSlot>().First().Opens!.Execute();
        sim.Tick();
        var form = sim.Editor.Pick().Arguments!.Form;
        form.MarkOf(0)!.Command!.Execute();
        sim.Tick();
        sim.Editor.PressAnswer(sim.Editor.Texts.ValueExpression);
        sim.Tick();
        var level = new BlockReading(new Framework.Game.Members.MemberKey("sprocket", "position"), Combination.Average, new List<Literal>(),
            new List<string> { "tanks" });
        Build(sim, draft =>
        {
            draft.Fill((DraftHole)draft.Root, new DraftJoin(JoinKind.Add));
            var outer = (DraftJoin)draft.Root;
            draft.Fill((DraftHole)outer.Parts[0], new DraftJoin(JoinKind.Add));
            var inner = (DraftJoin)((DraftJoin)draft.Root).Parts[0];
            draft.Fill((DraftHole)inner.Parts[0], new DraftValue(new Operand(Literal.Of("Oxygen: "))));
            draft.Fill((DraftHole)((DraftJoin)((DraftJoin)draft.Root).Parts[0]).Parts[1], new DraftValue(level));
            draft.Fill((DraftHole)((DraftJoin)draft.Root).Parts[1], new DraftValue(new Operand(Literal.Of("%"))));
        });
        sim.Editor.Current.Primary();
        sim.Tick();

        var act = Assert.IsType<Act>(Assert.Single(sequence.Steps));
        Assert.True(act.Arguments[0].IsExpression, act.Arguments[0].Said);
        system.Enabled = true;
        sim.Editor.Program.Edited();
        for (var i = 0; i < 3; i++)
            sim.Tick();
        sim.Runner.Call(sequence, system.First);
        for (var i = 0; i < 5; i++)
            sim.Tick();

        var halt = system.First.Halt ?? system.Halt;
        Assert.True(halt == null, halt?.Problem.ToString());
        Assert.Matches(@"^Oxygen: [0-9.]+%$", board.Text);
    }

    /// <summary>
    /// A text built on the expression page for a new step or one given its text already, around a value of blocks picked there, which makes a slot of them, is
    /// saved as the expression, never as its words.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ATextAroundBlocksPickedOnThePageIsSavedAsTheExpression(bool given)
    {
        var rig = Rig.Demo();
        rig.TextPanel(40, "Status Panel");
        var screen = Screen.On(rig);
        var editor = screen.Editor;
        editor.NewButtonSystem("Airlock");
        screen.Run(2);
        editor.PressAdd("task");
        editor.PressBlock("Status Panel");
        screen.Run(2);
        editor.PressChoice("show text");
        screen.Run(2);
        if (given)
        {
            editor.Pick().Arguments!.Form.Fields.First(field => field is Framework.Ui.Fields.LinesField).Text = "Docked";
            editor.PressPrimary();
            screen.Run(2);
            editor.StepRows().Last().Slots.OfType<ActionSlot>().First().Opens!.Execute();
            screen.Run(2);
        }

        var form = editor.Pick().Arguments!.Form;
        form.MarkOf(0)!.Command!.Execute();
        screen.Run(2);
        editor.PressAnswer(editor.Texts.ValueExpression);
        screen.Run(2);

        var panel = Assert.IsType<ExpressionPage>(editor.Current).Panel;
        var draft = panel.Draft;
        if (!(draft.Root is DraftHole))
            draft.Remove(draft.Root);

        draft.Fill((DraftHole)draft.Root, new DraftJoin(JoinKind.Add));
        draft.Fill((DraftHole)((DraftJoin)draft.Root).Parts[0], new DraftValue(new Operand(Literal.Of("Oxygen: "))));
        screen.Run(2);
        var grip = ExpressionBuilderTests.Middle(((Lumel.Layout.HStack)panel.Tray.PieceFor(ValueSource.Reading)).Children[0]);
        var place = ExpressionBuilderTests.Middle(panel.ShownFor(((DraftJoin)draft.Root).Parts[1])!);
        screen.Do("drag", ExpressionBuilderTests.Say(grip.X), ExpressionBuilderTests.Say(grip.Y), ExpressionBuilderTests.Say(place.X), ExpressionBuilderTests.Say(place.Y));
        screen.Run(3);
        if (screen.Shows(editor.Texts.PickSourceBlocks))
        {
            editor.PressAnswer(editor.Texts.PickSourceBlocks);
            screen.Run(2);
        }

        editor.TickBlock("Airlock Vent");
        screen.Run(2);
        editor.PressChoice("oxygen level");
        screen.Run(2);
        editor.PressPrimary();
        screen.Run(2);
        var builder = Assert.IsType<ExpressionPage>(editor.Current);
        Assert.True(builder.Panel.Reads, builder.Panel.Why);
        builder.Primary();
        screen.Run(2);
        editor.PressPrimary();
        screen.Run(2);

        var act = Assert.IsType<Act>(editor.Program.Systems[0].Sequences[0].Steps.Last());
        Assert.True(act.Arguments[0].IsExpression, act.Arguments[0].Said);
        var saved = ProgramWriter.Save(editor.Program, editor.World.Values);
        var read = new Automation();
        Assert.Equal("", ProgramReader.Load(saved, read, new Values()));
        var reread = Assert.IsType<Act>(read.Systems[0].Sequences[0].Steps.Last());
        Assert.True(reread.Arguments[0].IsExpression, reread.Arguments[0].Said + " " + saved);
    }

    /// <summary>Presses Expression… under the mark of the first value <paramref name="giving" /> asks for.</summary>
    static void Expression(Simulation sim, ValuesQuestion giving)
    {
        giving.Form.MarkOf(0)!.Command!.Execute();
        sim.Tick();
        sim.Editor.PressAnswer(sim.Editor.Texts.ValueExpression);
        sim.Tick();
    }

    /// <summary>Builds, on the expression page open, the setting <paramref name="setting" /> times 2 in an emptied place.</summary>
    static Action<ExpressionDraft> Times(string setting) => draft =>
    {
        draft.Fill((DraftHole)draft.Root, new DraftJoin(JoinKind.Multiply));
        draft.Fill((DraftHole)((DraftJoin)draft.Root).Parts[0], new DraftValue(new Operand(Literal.Setting(setting))));
        draft.Fill((DraftHole)((DraftJoin)draft.Root).Parts[1], new DraftValue(new Operand(Literal.Of(2L))));
    };

    /// <summary>Empties the expression page open, has <paramref name="build" /> fill it, and presses Done.</summary>
    static void Build(Simulation sim, Action<ExpressionDraft> build)
    {
        var builder = Assert.IsType<ExpressionPage>(sim.Editor.Current);
        var draft = builder.Panel.Draft;
        if (!(draft.Root is DraftHole))
            draft.Remove(draft.Root);

        build(draft);
        Assert.True(builder.Panel.Reads, builder.Panel.Why);
        builder.Primary();
        sim.Tick();
    }

    /// <summary>The program <paramref name="sim" /> saves, read back into a program of its own.</summary>
    static Automation Reloaded(Simulation sim)
    {
        var read = new Automation();
        Assert.Equal("", ProgramReader.Load(ProgramWriter.Save(sim.Editor.Program, sim.World.Values), read, new Values()));
        return read;
    }
}
