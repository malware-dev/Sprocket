using Sprocket.Host.SpaceEngineers.Features.Expressions;
using Sprocket.Host.SpaceEngineers.Features.Selector;
using Sprocket.Host.SpaceEngineers.Features.Sequences;
using Sprocket.Host.SpaceEngineers.Features.LogicSystems;
using Sprocket.Host.SpaceEngineers.Framework.Language.Reading;
using Sprocket.Host.SpaceEngineers.Framework.Language.Writing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// A step holding an expression opens in the editor as it is written: one chip with the expression's text, kept as it is by what
/// else is edited on the step, and replaced only by a fixed value or a setting chosen in its place.
/// </summary>
public class ExpressionEditorTests
{
    const string Expression = "10 * Reach";

    // How the expression reads on screen: its calculation as mathematics writes it.
    const string Shown = "10 × Reach";

    /// <summary>The arm's blueprint, its one sequence holding <paramref name="step" />, open on its page.</summary>
    internal static Simulation Arm(string step, out AutomationSystem system, out Sequence sequence)
    {
        var sim = Simulation.Fresh();
        sim.Machine.Piston(50, "Arm Piston");
        sim.Tick();
        var program = "sprocket 3 alpha 3\n\n"
            + "let 'Arm Piston' = block 'ExtendedPistonBase/LargePistonBase' [ 50, [ (50 0 0) ], \"Arm Piston\" ]\n\n"
            + "system blueprint \"0b0c8f3e-5d5e-4a43-9a51-6f1c2d7e9a10\" as Malware.Arm\n{\n"
            + "    required slot arm expects ExtendedPistonBase\n"
            + "    optional slot Reach expects real = 5\n"
            + "    optional slot Pause expects duration = 2s\n\n"
            + "    sequence 1 {\n        " + step + "\n    }\n}\n\n"
            + "instance 1 of \"0b0c8f3e-5d5e-4a43-9a51-6f1c2d7e9a10\" as 'Installation 1' {\n    arm = [ 'Arm Piston' ]\n}\n";
        Assert.Equal("", ProgramReader.Load(program, sim.Editor.Program, sim.World.Values));
        system = sim.Editor.Program.Systems[0];
        sequence = system.Sequences[0];
        system.Enabled = false;
        sim.Editor.Program.Edited();
        sim.Editor.Invalidate();
        sim.Tick();
        sim.Editor.Push(new SequencePage(sim.Editor, sequence));
        for (var i = 0; i < 5; i++)
            sim.Tick();

        return sim;
    }

    /// <summary>
    /// A text panel's text calculated on the expression page is held in the step's panel as it reads, and Done saves it as
    /// the expression, never as its words.
    /// </summary>
    [Fact]
    public void ATextCalculatedForAPanelIsSavedAsTheExpression()
    {
        var sim = Simulation.Fresh();
        sim.Machine.TextPanel(60, "Board");
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

        OpenStep(sim);
        Assert.IsType<PickPage>(sim.Editor.Current);
        var form = sim.Editor.Pick().Arguments!.Form;
        form.MarkOf(0)!.Command!.Execute();
        sim.Tick();
        sim.Editor.PressAnswer(sim.Editor.Texts.ValueExpression);
        sim.Tick();

        var builder = Assert.IsType<ExpressionPage>(sim.Editor.Current);
        var draft = builder.Panel.Draft;
        draft.Remove(draft.Root);
        draft.Fill((DraftHole)draft.Root, new DraftJoin(JoinKind.Add));
        draft.Fill((DraftHole)((DraftJoin)draft.Root).Parts[0], new DraftValue(new Framework.Model.Expressions.Operand(Literal.Of("Reach "))));
        draft.Fill((DraftHole)((DraftJoin)draft.Root).Parts[1], new DraftValue(new Framework.Model.Expressions.Operand(Literal.Setting("Reach"))));
        Assert.True(builder.Panel.Reads, builder.Panel.Why);
        builder.Primary();
        sim.Tick();

        Assert.True(form.ExpressionAt(0)!.IsVisible);
        Assert.False(((Lumel.Element)form.Fields[0]).IsVisible);
        Assert.Equal("\"Reach \" + Reach", form.ExpressionAt(0)!.Text);
        sim.Editor.Current.Primary();
        sim.Tick();

        var act = Assert.IsType<Act>(Assert.Single(sequence.Steps));
        Assert.True(act.Arguments[0].IsExpression, act.Arguments[0].Said);
        Assert.Equal("\"Reach \" + Reach", act.Arguments[0].Tree.Written);
    }

    /// <summary>
    /// A text whose words are those of an expression, built as that expression on the expression page, is a change, and Done
    /// makes the step hold the expression.
    /// </summary>
    [Fact]
    public void AnExpressionWrittenAsTheTextItReplacesIsAChange()
    {
        var sim = Simulation.Fresh();
        sim.Machine.TextPanel(60, "Board");
        sim.Tick();
        var program = "sprocket 3 alpha 1\n\n"
            + "let 'Board' = block 'TextPanel/LargeTextPanel' [ 60, [ (60 0 0) ], \"Board\" ]\n\n"
            + "system blueprint \"0b0c8f3e-5d5e-4a43-9a51-6f1c2d7e9a10\" as Malware.Sign\n{\n"
            + "    required slot board expects sprocket.screen\n"
            + "    optional slot Reach expects real = 5\n\n"
            + "    sequence 1 {\n        call sprocket.show_text(\"\"\"Reach \"\" + Reach\") on [ board ]\n    }\n}\n\n"
            + "instance 1 of \"0b0c8f3e-5d5e-4a43-9a51-6f1c2d7e9a10\" as 'Installation 1' {\n    board = [ sprocket.get_screen(1) on [ 'Board' ] ]\n}\n";
        Assert.Equal("", ProgramReader.Load(program, sim.Editor.Program, sim.World.Values));
        var system = sim.Editor.Program.Systems[0];
        var sequence = system.Sequences[0];
        Assert.Equal("\"Reach \" + Reach", ((Act)sequence.Steps[0]).Arguments[0].Text);
        system.Enabled = false;
        sim.Editor.Program.Edited();
        sim.Editor.Invalidate();
        sim.Tick();
        sim.Editor.Push(new SequencePage(sim.Editor, sequence));
        for (var i = 0; i < 5; i++)
            sim.Tick();

        OpenStep(sim);
        var form = sim.Editor.Pick().Arguments!.Form;
        form.MarkOf(0)!.Command!.Execute();
        sim.Tick();
        sim.Editor.PressAnswer(sim.Editor.Texts.ValueExpression);
        sim.Tick();
        var builder = Assert.IsType<ExpressionPage>(sim.Editor.Current);
        var draft = builder.Panel.Draft;
        draft.Remove(draft.Root);
        draft.Fill((DraftHole)draft.Root, new DraftJoin(JoinKind.Add));
        draft.Fill((DraftHole)((DraftJoin)draft.Root).Parts[0], new DraftValue(new Framework.Model.Expressions.Operand(Literal.Of("Reach "))));
        draft.Fill((DraftHole)((DraftJoin)draft.Root).Parts[1], new DraftValue(new Framework.Model.Expressions.Operand(Literal.Setting("Reach"))));
        builder.Primary();
        sim.Tick();

        sim.Editor.Current.Primary();
        sim.Tick();

        var act = Assert.IsType<Act>(Assert.Single(sequence.Steps));
        Assert.True(act.Arguments[0].IsExpression, act.Arguments[0].Said);
        Assert.Equal("\"Reach \" + Reach", act.Arguments[0].Tree.Written);
    }

    /// <summary>A text panel's step, open on its page, its text written as <paramref name="text" />, the script's way.</summary>
    static Simulation Board(string text, out Sequence sequence)
    {
        var sim = Simulation.Fresh();
        sim.Machine.TextPanel(60, "Board");
        sim.Tick();
        var program = "sprocket 3 alpha 1\n\n"
            + "let 'Board' = block 'TextPanel/LargeTextPanel' [ 60, [ (60 0 0) ], \"Board\" ]\n\n"
            + "system blueprint \"0b0c8f3e-5d5e-4a43-9a51-6f1c2d7e9a10\" as Malware.Sign\n{\n"
            + "    required slot board expects sprocket.screen\n"
            + "    optional slot Reach expects real = 5\n\n"
            + "    sequence 1 {\n        call sprocket.show_text(" + text + ") on [ board ]\n    }\n}\n\n"
            + "instance 1 of \"0b0c8f3e-5d5e-4a43-9a51-6f1c2d7e9a10\" as 'Installation 1' {\n    board = [ sprocket.get_screen(1) on [ 'Board' ] ]\n}\n";
        Assert.Equal("", ProgramReader.Load(program, sim.Editor.Program, sim.World.Values));
        var system = sim.Editor.Program.Systems[0];
        sequence = system.Sequences[0];
        system.Enabled = false;
        sim.Editor.Program.Edited();
        sim.Editor.Invalidate();
        sim.Tick();
        sim.Editor.Push(new SequencePage(sim.Editor, sequence));
        for (var i = 0; i < 5; i++)
            sim.Tick();

        return sim;
    }

    /// <summary>Pressing a step's text in its sentence opens the page text is written on, and saving there gives the step that text.</summary>
    [Fact]
    public void PressingAStepsTextOpensItsPage()
    {
        var sim = Board("\"Shut\"", out var sequence);

        OpenValues(sim);
        var page = Assert.IsType<Framework.Ui.Writing.TextPage>(sim.Editor.Current);
        Assert.NotNull(page.View);
        Assert.Equal("Shut", page.Box.Text);
        page.Box.Text = "Open";
        page.Primary();
        sim.Tick();

        Assert.IsType<SequencePage>(sim.Editor.Current);
        Assert.Equal("Open", Assert.IsType<Act>(Assert.Single(sequence.Steps)).Arguments[0].Text);
    }

    /// <summary>Pressing a step's text in its sentence where an expression gives it opens the expression page on it.</summary>
    [Fact]
    public void PressingAStepsExpressionOpensItsPage()
    {
        var sim = Board("\"Reach \" + Reach", out _);

        OpenValues(sim);

        var builder = Assert.IsType<ExpressionPage>(sim.Editor.Current);
        Assert.Equal("\"Reach \" + Reach", builder.Panel.Draft.Built()!.Written);
    }

    static Simulation Moving(out AutomationSystem system, out Sequence sequence) =>
        Arm("call sprocket.move_to(" + Expression + ", 0.3) on [ arm ]", out system, out sequence);

    // Opens the first step in the selector, by its action, where its values can come from somewhere else.
    static void OpenStep(Simulation sim)
    {
        sim.Editor.StepRows()[0].Slots.OfType<ActionSlot>().First().Opens!.Execute();
        sim.Tick();
    }

    static void OpenValues(Simulation sim)
    {
        sim.Editor.StepRows()[0].Slots.OfType<ArgumentsSlot>().First().Opens!.Execute();
        sim.Tick();
    }

    /// <summary>A step given an expression says it as one chip, reading as mathematics writes it rather than as the script does.</summary>
    [Fact]
    public void AStepGivenAnExpressionShowsItsText()
    {
        var sim = Moving(out _, out _);

        var slot = sim.Editor.StepRows()[0].Slots.OfType<ArgumentsSlot>().First();
        Assert.Contains(Shown, SentenceText.Given(slot));
    }

    /// <summary>The panel opens on the expression as it reads, and closing it without a change leaves the step as it was.</summary>
    [Fact]
    public void ClosingThePanelUnchangedKeepsTheExpression()
    {
        var sim = Moving(out _, out var sequence);
        OpenValues(sim);
        var page = Assert.IsType<SequencePage>(sim.Editor.Current);
        Assert.Equal(Shown, page.Giving!.Form.ExpressionAt(0)!.Text);
        Assert.False(((Lumel.Element)page.Giving!.Form.Fields[0]).IsVisible);

        page.Giving!.Done();
        sim.Tick();

        Assert.Null(page.Giving);
        var act = Assert.IsType<Act>(Assert.Single(sequence.Steps));
        Assert.True(act.Arguments[0].IsExpression);
        Assert.Equal(Expression, act.Arguments[0].Tree.Written);
        Assert.Equal(0.3, act.Arguments[1].Number);
    }

    /// <summary>Changing the other value of the step leaves the expression as it is.</summary>
    [Fact]
    public void ChangingAnotherValueKeepsTheExpression()
    {
        var sim = Moving(out var system, out var sequence);
        OpenValues(sim);
        var page = Assert.IsType<SequencePage>(sim.Editor.Current);
        page.Giving!.Form.Fields[1].Text = "0.5";
        page.Giving!.Done();
        sim.Tick();

        var act = Assert.IsType<Act>(Assert.Single(sequence.Steps));
        Assert.True(act.Arguments[0].IsExpression);
        Assert.Equal(Expression, act.Arguments[0].Tree.Written);
        Assert.Equal(0.5, act.Arguments[1].Number);

        sim.Editor.Undo(system, sequence);
        var undone = Assert.IsType<Act>(Assert.Single(sequence.Steps));
        Assert.Equal(Expression, undone.Arguments[0].Tree.Written);
        Assert.Equal(0.3, undone.Arguments[1].Number);
    }

    /// <summary>The program saved holds the expression as it was written.</summary>
    [Fact]
    public void TheExpressionIsSavedAsWritten()
    {
        var sim = Moving(out _, out _);

        Assert.Contains("call sprocket.move_to(" + Expression + ", 0.3) on [ arm ]", ProgramWriter.Save(sim.Editor.Program, sim.World.Values));
    }

    /// <summary>The value's mark replaces the expression with a setting, as one change that undoing brings the expression back from.</summary>
    [Fact]
    public void TheMarkReplacesTheExpressionWithASetting()
    {
        var sim = Moving(out var system, out var sequence);
        OpenValues(sim);
        var page = Assert.IsType<SequencePage>(sim.Editor.Current);
        page.Giving!.Form.MarkOf(0)!.Command!.Execute();
        sim.Tick();

        sim.Editor.PressAnswer("Reach");
        sim.Tick();
        page.Giving!.Done();
        sim.Tick();

        var act = Assert.IsType<Act>(Assert.Single(sequence.Steps));
        Assert.True(act.Arguments[0].IsSetting);
        Assert.Equal("Reach", act.Arguments[0].Text);

        sim.Editor.Undo(system, sequence);
        Assert.Equal(Expression, Assert.IsType<Act>(Assert.Single(sequence.Steps)).Arguments[0].Tree.Written);
    }

    /// <summary>
    /// An expression stands in its value's place, not in its field, so its words cannot be typed over, and pressing it opens the
    /// expression page on it.
    /// </summary>
    [Fact]
    public void PressingTheExpressionOpensItsPage()
    {
        var sim = Moving(out _, out _);
        OpenValues(sim);
        var form = Assert.IsType<SequencePage>(sim.Editor.Current).Giving!.Form;
        Assert.False(((Lumel.Element)form.Fields[0]).IsVisible);
        Assert.True(form.MarkOf(0)!.Is(Framework.Ui.Look.LookStates.Chosen));

        form.ExpressionAt(0)!.Command!.Execute();
        sim.Tick();

        var builder = Assert.IsType<ExpressionPage>(sim.Editor.Current);
        Assert.Equal(Expression, builder.Panel.Draft.Built()!.Written);
    }

    /// <summary>Opening an expression and leaving it untouched changes nothing: the page has nothing to undo, and the step is as it was.</summary>
    [Fact]
    public void OpeningAnExpressionUntouchedChangesNothing()
    {
        // Every value given, its optional one too, so only the expression is left to tell apart.
        var sim = Arm("call sprocket.move_to(" + Expression + ", 0.3, each) on [ arm ]", out var system, out var sequence);
        var before = string.Join(" | ", ((Act)sequence.Steps[0]).Arguments.Select(a => a.Kind + ":" + a.Said));
        OpenValues(sim);
        var form = Assert.IsType<SequencePage>(sim.Editor.Current).Giving!.Form;
        form.ExpressionAt(0)!.Command!.Execute();
        sim.Tick();
        var builder = Assert.IsType<ExpressionPage>(sim.Editor.Current);
        Assert.False(builder.CanUndo, "the expression page had something to undo before anything was touched");

        Assert.True(builder.Panel.Reads, builder.Panel.Why);
        builder.Primary();
        sim.Tick();
        var page = Assert.IsType<SequencePage>(sim.Editor.Current);
        page.Giving!.Done();
        sim.Tick();

        Assert.False(sim.Editor.CanUndo(system, sequence), $"closing an untouched expression made a change: was [{before}] now [{string.Join(" | ", ((Act)sequence.Steps[0]).Arguments.Select(a => a.Kind + ":" + a.Said))}]");
    }

    /// <summary>A text panel's text held as an expression, opened in the selector and on its page and left untouched, is no change: leaving asks nothing, and Done changes nothing.</summary>
    [Fact]
    public void AnUntouchedTextExpressionInTheSelectorIsNoChange()
    {
        var sim = Simulation.Fresh();
        sim.Machine.TextPanel(60, "Board");
        sim.Tick();
        var program = "sprocket 3 alpha 1\n\n"
            + "let 'Board' = block 'TextPanel/LargeTextPanel' [ 60, [ (60 0 0) ], \"Board\" ]\n\n"
            + "system blueprint \"0b0c8f3e-5d5e-4a43-9a51-6f1c2d7e9a10\" as Malware.Sign\n{\n"
            + "    required slot board expects sprocket.screen\n"
            + "    optional slot Reach expects real = 5\n\n"
            + "    sequence 1 {\n        call sprocket.show_text(\"Reach \" + Reach) on [ board ]\n    }\n}\n\n"
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

        OpenStep(sim);
        var form = sim.Editor.Pick().Arguments!.Form;
        Assert.True(sim.Editor.Current.Leaving(), "the selector thought something moved before anything was touched");
        form.ExpressionAt(0)!.Command!.Execute();
        sim.Tick();
        Assert.IsType<ExpressionPage>(sim.Editor.Current).Primary();
        sim.Tick();

        Assert.True(sim.Editor.Current.Leaving(), "the selector thought something moved after the expression page closed untouched");
        sim.Editor.Current.Primary();
        sim.Tick();
        Assert.False(sim.Editor.CanUndo(system, sequence), $"Done on an untouched expression made a change: {((Act)sequence.Steps[0]).Arguments[0].Said}");
    }

    /// <summary>The menu under the mark of a value an expression gives marks Expression…, not a fixed value.</summary>
    [Fact]
    public void TheMarkSaysTheExpressionGivesTheValue()
    {
        var sim = Moving(out _, out _);
        OpenValues(sim);
        var form = Assert.IsType<SequencePage>(sim.Editor.Current).Giving!.Form;

        form.MarkOf(0)!.Command!.Execute();
        sim.Tick();

        var texts = sim.Editor.Texts;
        Assert.Equal(texts.ValueExpression, sim.Editor.Screen.Default?.Text);
    }

    /// <summary>The value's mark replaces the expression with a fixed value, its field back in the expression's place to type it in.</summary>
    [Fact]
    public void TheMarkReplacesTheExpressionWithAFixedValue()
    {
        var sim = Moving(out _, out var sequence);
        OpenValues(sim);
        var page = Assert.IsType<SequencePage>(sim.Editor.Current);
        var form = page.Giving!.Form;
        form.MarkOf(0)!.Command!.Execute();
        sim.Tick();
        sim.Editor.PressAnswer(sim.Editor.Texts.ValueFixed);
        sim.Tick();
        Assert.False(form.ExpressionAt(0)!.IsVisible);
        Assert.True(((Lumel.Element)form.Fields[0]).IsVisible);
        form.Fields[0].Text = "7";
        page.Giving!.Done();
        sim.Tick();

        var act = Assert.IsType<Act>(Assert.Single(sequence.Steps));
        Assert.False(act.Arguments[0].IsExpression);
        Assert.Equal(7, act.Arguments[0].Number);
    }

    /// <summary>A wait holding an expression shows it as it reads, its calculations as mathematics writes them, and is saved as written.</summary>
    [Fact]
    public void AWaitGivenAnExpressionShowsItsText()
    {
        var sim = Arm("wait setting(Pause) * 2", out _, out var sequence);

        var slot = sim.Editor.StepRows()[0].Slots.OfType<DurationSlot>().Single();
        Assert.Equal("Pause × 2", slot.Expression);
        Assert.Contains("wait setting(Pause) * 2\n", ProgramWriter.Save(sim.Editor.Program, sim.World.Values));
        Assert.NotNull(Assert.IsType<Wait>(Assert.Single(sequence.Steps)).Expression);
    }

    /// <summary>
    /// A wait holding an expression opens the page it is built on; a length given there takes its place, as one change that
    /// undoing brings the expression back from, and leaving the page as it is keeps it.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ALengthGivenReplacesAWaitsExpression(bool changed)
    {
        var sim = Arm("wait setting(Pause) * 2", out var system, out var sequence);
        sim.Editor.StepRows()[0].Slots.OfType<DurationSlot>().Single().Opens!.Execute();
        sim.Tick();
        sim.Tick();
        var builder = Assert.IsType<ExpressionPage>(sim.Editor.Current);
        if (changed)
        {
            builder.Panel.Draft.Remove(builder.Panel.Draft.Root);
            builder.Panel.Draft.Fill((DraftHole)builder.Panel.Draft.Root, new DraftValue(new Framework.Model.Expressions.Operand(Literal.Of(Duration.OfSeconds(4)))));
        }

        builder.Panel.Reckon();
        Assert.True(builder.Panel.Reads, builder.Panel.Why);
        builder.Primary();
        sim.Tick();

        var wait = Assert.IsType<Wait>(Assert.Single(sequence.Steps));
        if (!changed)
        {
            Assert.Equal("setting(Pause) * 2", wait.Expression?.Written);
            return;
        }

        Assert.Null(wait.Expression);
        Assert.True(wait.Length.Ticks > 0);

        sim.Editor.Undo(system, sequence);
        Assert.Equal("setting(Pause) * 2", wait.Expression?.Written);
    }

    /// <summary>A blueprint holding <paramref name="line" /> among its triggers and steps, its triggers tab open.</summary>
    internal static Simulation Loaded(string line, out AutomationSystem system)
    {
        var sim = Simulation.Fresh();
        sim.Machine.Piston(50, "Arm Piston");
        sim.Tick();
        var program = "sprocket 3 alpha 3\n\n"
            + "let 'Arm Piston' = block 'ExtendedPistonBase/LargePistonBase' [ 50, [ (50 0 0) ], \"Arm Piston\" ]\n\n"
            + "system blueprint \"0b0c8f3e-5d5e-4a43-9a51-6f1c2d7e9a10\" as Malware.Arm\n{\n"
            + "    required slot arm expects ExtendedPistonBase\n"
            + "    optional slot Reach expects real = 5\n"
            + "    optional slot Pause expects duration = 2s\n\n"
            + "    let lit = off\n\n"
            + "    " + line + "\n\n"
            + "    sequence 1 {\n        wait 1s\n    }\n}\n\n"
            + "instance 1 of \"0b0c8f3e-5d5e-4a43-9a51-6f1c2d7e9a10\" as 'Installation 1' {\n    arm = [ 'Arm Piston' ]\n}\n";
        Assert.Equal("", ProgramReader.Load(program, sim.Editor.Program, sim.World.Values));
        system = sim.Editor.Program.Systems[0];
        system.Enabled = false;
        sim.Editor.Program.Edited();
        sim.Editor.Invalidate();
        sim.Tick();
        sim.Editor.Push(new LogicSystemPage(sim.Editor, system));
        for (var i = 0; i < 5; i++)
            sim.Tick();

        return sim;
    }

    /// <summary>A clock whose interval is an expression shows it as it reads, and is saved as written.</summary>
    [Fact]
    public void AClockGivenAnExpressionShowsItsText()
    {
        var sim = Loaded("every Pause * 2 run 1", out _);

        var slot = sim.Editor.Triggers()[0].Starts.OfType<DurationSlot>().Single();
        Assert.Equal("Pause × 2", slot.Expression);
        Assert.Contains("    every Pause * 2 run 1\n", ProgramWriter.Save(sim.Editor.Program, sim.World.Values));
    }

    /// <summary>What a watcher compares with is an expression's chip, reading as mathematics writes it, and the watcher is saved as written.</summary>
    [Fact]
    public void AWatcherGivenAnExpressionShowsItsText()
    {
        var sim = Loaded("when all sprocket.position on [ arm ] > Reach * 2 then run 1", out _);

        var slot = sim.Editor.Triggers()[0].Starts.OfType<ArgumentsSlot>().Single(part => part.Holds == ArgumentRole.Checked);
        Assert.Contains("Reach × 2", SentenceText.Given(slot));
        Assert.Contains("    when all sprocket.position on [ arm ] > Reach * 2 then run 1\n", ProgramWriter.Save(sim.Editor.Program, sim.World.Values));
    }

    /// <summary>An abort whose message is an expression shows it as it reads, its text quoted, and keeps it when its level is changed.</summary>
    [Fact]
    public void AnAbortGivenAnExpressionShowsItsText()
    {
        var sim = Arm("abort saying \"Reach \" + Reach as warning", out _, out var sequence);

        var slot = sim.Editor.StepRows()[0].Slots.OfType<ArgumentsSlot>().Single();
        Assert.Contains("\"Reach \" + Reach", SentenceText.Given(slot));
        Assert.Contains("abort saying \"Reach \" + Reach as warning\n", ProgramWriter.Save(sim.Editor.Program, sim.World.Values));
        Assert.NotNull(Assert.IsType<Abort>(Assert.Single(sequence.Steps)).Expression);
    }

    /// <summary>A step setting a state to an expression shows the block value by its name, never in the script's words, and is saved as written.</summary>
    [Fact]
    public void ASetStateGivenAnExpressionShowsItsText()
    {
        var sim = Loaded("when all sprocket.position on [ arm ] > 1 then run 1", out var system);
        var sequence = system.Sequences[0];
        var reading = new Framework.Model.Expressions.BlockReading(new Framework.Game.Members.MemberKey("sprocket", "stopped"), Framework.Model.Expressions.Combination.None, new List<Literal>(), new List<string> { "arm" });
        sequence.Steps.Add(new SetState { State = "lit", Expression = reading });
        sim.Editor.Program.Edited();
        sim.Editor.Invalidate();
        sim.Tick();
        sim.Editor.Push(new SequencePage(sim.Editor, sequence));
        for (var i = 0; i < 5; i++)
            sim.Tick();

        var slot = sim.Editor.StepRows()[1].Slots.OfType<ArgumentsSlot>().Single();
        Assert.DoesNotContain("sprocket.", SentenceText.Given(slot));
        Assert.Contains("arm", SentenceText.Given(slot));
        Assert.Contains("set lit to sprocket.stopped on [ arm ]\n", ProgramWriter.Save(sim.Editor.Program, sim.World.Values));
    }

    /// <summary>What an if compares with is an expression's chip, kept as it is when the test's other parts change.</summary>
    [Fact]
    public void WhatAnIfComparesWithCanBeAnExpressionChip()
    {
        var sim = Arm("if all sprocket.position on [ arm ] > Reach * 2 then {\n            wait 1s\n        }", out _, out var sequence);

        var test = sequence.Steps.OfType<If>().Single().Compared!;
        Assert.True(test.Value.IsExpression);
        var slot = sim.Editor.StepRows()[0].Slots.OfType<ArgumentsSlot>().Single(part => part.Holds == ArgumentRole.Checked);
        Assert.Contains("Reach × 2", SentenceText.Given(slot));
        Assert.Contains("> Reach * 2 then", ProgramWriter.Save(sim.Editor.Program, sim.World.Values));
    }
}
