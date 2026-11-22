using System.Globalization;
using Lumel;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Expressions;
using Sprocket.Host.SpaceEngineers.Features.Sequences;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Model.Expressions;
using VRageMath;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>The page a value's expression is built on by dragging calculations and values into place, opened from the value's mark.</summary>
public class ExpressionBuilderTests
{
    const string Program = "sprocket 3 alpha 1\n\n"
        + "system blueprint \"0b0c8f3e-5d5e-4a43-9a51-6f1c2d7e9a10\" as Malware.Arm\n{\n"
        + "    optional slot Reach expects real = 5\n"
        + "    optional slot Label expects text = \"x\"\n\n"
        + "    sequence 1 {\n    }\n}\n";

    /// <summary>The builder for a real value of the arm, opened on <paramref name="start" />, which hands what Done gives to <paramref name="done" />.</summary>
    static Screen Builder(Literal? start, Action<Literal> done, string wanted = "real")
    {
        var screen = Screen.Holding(Program);
        screen.Run(3);
        var editor = screen.Editor;
        editor.Push(new ExpressionPage(editor, editor.Program.Systems[0], wanted, "Reach", start, done));
        screen.Run(5);
        return screen;
    }

    static ExpressionPanel Panel(Screen screen) => Assert.IsType<ExpressionPage>(screen.Editor.Current).Panel;

    static Vector2 Middle(Element element)
    {
        var bounds = element.Bounds;
        return element.ToScreen(new Vector2(bounds.X + bounds.Width / 2f, bounds.Y + bounds.Height / 2f));
    }

    static string Say(float coordinate) => coordinate.ToString("0.###", CultureInfo.InvariantCulture);

    /// <summary>Drags the grip of <paramref name="piece" /> onto <paramref name="target" />, letting go there unless <paramref name="held" />.</summary>
    static void Drag(Screen screen, Element piece, Element target, bool held = false)
    {
        var grip = Middle(((HStack)piece).Children[0]);
        var to = Middle(target);
        screen.Do(held ? "carry" : "drag", Say(grip.X), Say(grip.Y), Say(to.X), Say(to.Y));
        screen.Run(3);
    }

    static Literal Reach => Literal.Setting("Reach");

    /// <summary>A calculation dragged onto the empty place opens its box there, every place in it empty, and nothing is done until they are filled.</summary>
    [Fact]
    public void ACalculationOnTheEmptyPlaceOpensItsBox()
    {
        var screen = Builder(null, _ => { });
        var panel = Panel(screen);

        Drag(screen, panel.Tray.PieceFor(JoinKind.Add), panel.ShownFor(panel.Draft.Root)!);

        var join = Assert.IsType<DraftJoin>(panel.Draft.Root);
        Assert.Equal(JoinKind.Add, join.Kind);
        Assert.All(join.Parts, part => Assert.IsType<DraftHole>(part));
        Assert.False(panel.Reads);
    }

    /// <summary>A setting dragged into a place is chosen once it is dropped, from the logic system's settings, and the value it comes to is said.</summary>
    [Fact]
    public void ASettingIsChosenOnceItIsDropped()
    {
        var screen = Builder(null, _ => { });
        var panel = Panel(screen);

        Drag(screen, panel.Tray.PieceFor(ValueSource.Setting), panel.ShownFor(panel.Draft.Root)!);
        screen.Editor.PressAnswer("Reach");
        screen.Run(3);

        var value = Assert.IsType<DraftValue>(panel.Draft.Root);
        Assert.Equal("Reach", ((Operand)value.Atom).Value.Text);
        Assert.True(panel.Reads, panel.Why);
        Assert.Equal(Template.Fill(screen.Editor.Texts.ExpressionNow, "value", "5"), panel.Now);
    }

    /// <summary>A number dropped in a place starts at nothing and is typed where it stands, and the typing is one change Undo takes back.</summary>
    [Fact]
    public void ANumberIsTypedWhereItStands()
    {
        var screen = Builder(null, _ => { });
        var panel = Panel(screen);

        Drag(screen, panel.Tray.PieceFor(KindClass.Number), panel.ShownFor(panel.Draft.Root)!);
        var value = Assert.IsType<DraftValue>(panel.Draft.Root);
        Assert.Equal("0", value.Atom.Written);

        var box = Middle(panel.TypedFor(value)!.Box!);
        screen.Do("drag", Say(box.X), Say(box.Y), Say(box.X), Say(box.Y), "clear", "type", "7.5");
        screen.Run(3);

        Assert.Equal("7.5", value.Atom.Written);
        Assert.True(panel.Reads, panel.Why);
        Assert.Equal(Template.Fill(screen.Editor.Texts.ExpressionNow, "value", "7.5"), panel.Now);

        screen.Editor.Screen.Undo!.Execute();
        screen.Run(3);
        Assert.Equal("0", Assert.IsType<DraftValue>(panel.Draft.Root).Atom.Written);
    }

    /// <summary>The tray offers only what can go somewhere in the value being built: for a number, no text, length of time or text setting.</summary>
    [Fact]
    public void TheTrayOffersOnlyWhatFits()
    {
        var screen = Builder(null, _ => { });
        var panel = Panel(screen);

        Assert.True(panel.Tray.PieceFor(KindClass.Number).IsVisible);
        Assert.False(panel.Tray.PieceFor(KindClass.Text).IsVisible);
        Assert.False(panel.Tray.PieceFor(KindClass.Duration).IsVisible);
        Assert.False(panel.Tray.PieceFor(KindClass.Flag).IsVisible);

        Drag(screen, panel.Tray.PieceFor(ValueSource.Setting), panel.ShownFor(panel.Draft.Root)!);
        Assert.True(screen.Shows("Reach"));
        Assert.False(screen.Shows("Label"));
    }

    /// <summary>While a piece is dragged, only the places it can go are lit: a calculation is not lit on a text where a number is wanted, and the bin only for a part of the expression.</summary>
    [Fact]
    public void OnlyThePlacesAPieceFitsAreLit()
    {
        var screen = Builder(Literal.Setting("Label"), _ => { });
        var panel = Panel(screen);
        var label = panel.ShownFor(panel.Draft.Root)!;

        Drag(screen, panel.Tray.PieceFor(JoinKind.Multiply), label, held: true);
        Assert.True(panel.IsDragging);
        Assert.False(ExpressionPanel.IsLit(label));
        Assert.False(ExpressionPanel.IsLit(panel.Tray.Bin));
        screen.Scene.Up();
        screen.Run(3);

        Assert.False(panel.IsDragging);
        Assert.IsType<DraftValue>(panel.Draft.Root);
    }

    /// <summary>A calculation dropped on a value puts the value in its first place and leaves the rest empty.</summary>
    [Fact]
    public void ACalculationOnAValueTakesItIn()
    {
        var screen = Builder(Reach, _ => { });
        var panel = Panel(screen);

        Drag(screen, panel.Tray.PieceFor(JoinKind.Add), panel.ShownFor(panel.Draft.Root)!);

        var join = Assert.IsType<DraftJoin>(panel.Draft.Root);
        Assert.IsType<DraftValue>(join.Parts[0]);
        Assert.IsType<DraftHole>(join.Parts[1]);
    }

    /// <summary>A value dropped in the bin leaves its place empty, and Undo puts it back.</summary>
    [Fact]
    public void TheBinEmptiesAPlaceAndUndoFillsItAgain()
    {
        var screen = Builder(Literal.Worked(new Arithmetic(Operator.Add, new Operand(Reach), new Operand(Literal.Of(2L)))), _ => { });
        var panel = Panel(screen);
        var join = (DraftJoin)panel.Draft.Root;

        Drag(screen, panel.ShownFor(join.Parts[0])!, panel.Tray.Bin);
        Assert.IsType<DraftHole>(((DraftJoin)panel.Draft.Root).Parts[0]);

        screen.Editor.Screen.Undo!.Execute();
        screen.Run(3);
        Assert.Equal("Reach + 2", panel.Draft.Built()!.Written);
    }

    /// <summary>A value dragged to an empty place moves there, leaving its own place empty.</summary>
    [Fact]
    public void AValueMovesToAnEmptyPlace()
    {
        var screen = Builder(Reach, _ => { });
        var panel = Panel(screen);
        Drag(screen, panel.Tray.PieceFor(JoinKind.Subtract), panel.ShownFor(panel.Draft.Root)!);
        var join = (DraftJoin)panel.Draft.Root;

        Drag(screen, panel.ShownFor(join.Parts[0])!, panel.ShownFor(join.Parts[1])!);

        join = (DraftJoin)panel.Draft.Root;
        Assert.IsType<DraftHole>(join.Parts[0]);
        Assert.IsType<DraftValue>(join.Parts[1]);
    }

    /// <summary>Done gives the expression as it was built, its fixed parts as they stand.</summary>
    [Fact]
    public void DoneGivesTheExpressionAsBuilt()
    {
        Literal given = default;
        var screen = Builder(Literal.Worked(TwiceThreePlusReach()), value => given = value);
        var panel = Panel(screen);

        Assert.Equal(Template.Fill(screen.Editor.Texts.ExpressionNow, "value", "11"), panel.Now);

        screen.Do("press", screen.Editor.Texts.ChromeDone);
        screen.Run(3);
        Assert.True(given.IsExpression);
        Assert.Equal("2 * 3 + Reach", given.Tree.Written);
    }

    /// <summary>Optimize works out the fixed parts as one change Undo takes back, and pressed again says there is nothing left to optimize.</summary>
    [Fact]
    public void OptimizeWorksOutTheFixedParts()
    {
        var screen = Builder(Literal.Worked(TwiceThreePlusReach()), _ => { });
        var panel = Panel(screen);

        screen.Do("press", screen.Editor.Texts.ExpressionOptimize);
        screen.Run(3);
        Assert.Equal("6 + Reach", panel.Draft.Built()!.Written);
        Assert.Equal("", panel.Optimized);

        screen.Do("press", screen.Editor.Texts.ExpressionOptimize);
        screen.Run(3);
        Assert.Equal(screen.Editor.Texts.ExpressionNothingToOptimize, panel.Optimized);

        screen.Editor.Screen.Undo!.Execute();
        screen.Run(3);
        Assert.Equal("2 * 3 + Reach", panel.Draft.Built()!.Written);
        Assert.Equal("", panel.Optimized);
    }

    /// <summary>Optimize works out a box with no empty place in it while another place is still empty.</summary>
    [Fact]
    public void OptimizeWorksOutWhatIsWholeAroundAnEmptyPlace()
    {
        var screen = Builder(Literal.Worked(TwiceThreePlusReach()), _ => { });
        var panel = Panel(screen);
        var join = (DraftJoin)panel.Draft.Root;
        Drag(screen, panel.ShownFor(join.Parts[1])!, panel.Tray.Bin);

        panel.Optimize();
        screen.Run(3);

        var optimized = (DraftJoin)panel.Draft.Root;
        Assert.Equal("6", ((DraftValue)optimized.Parts[0]).Atom.Written);
        Assert.IsType<DraftHole>(optimized.Parts[1]);
    }

    static Expression TwiceThreePlusReach() => new Arithmetic(Operator.Add,
        new Arithmetic(Operator.Multiply, new Operand(Literal.Of(2L)), new Operand(Literal.Of(3L))), new Operand(Reach));

    /// <summary>A value of the wrong kind says so, and cannot be done.</summary>
    [Fact]
    public void AValueOfTheWrongKindSaysSoAndCannotBeDone()
    {
        var screen = Builder(Literal.Setting("Label"), _ => { });
        var panel = Panel(screen);

        Assert.False(panel.Reads);
        Assert.NotEqual("", panel.Why);
        Assert.False(Assert.IsType<ExpressionPage>(screen.Editor.Current).CanPrimary);
    }

    /// <summary>
    /// Expression… on a step's value opens the builder on the value as it stands, and Done brings what was built back to
    /// the values panel, which gives it to the step.
    /// </summary>
    [Fact]
    public void ExpressionOpensTheBuilderOnTheValueAndDoneGivesItToTheStep()
    {
        var sim = ValuesPanelTests.Arm(out _, out var sequence);
        var page = Assert.IsType<SequencePage>(sim.Editor.Current);
        var form = page.Giving!.Form;
        form.MarkOf(0)!.Command!.Execute();
        sim.Tick();
        sim.Editor.PressAnswer(sim.Editor.Texts.ValueExpression);
        sim.Tick();
        sim.Tick();

        var builder = Assert.IsType<ExpressionPage>(sim.Editor.Current);
        var panel = builder.Panel!;
        var start = Assert.IsType<DraftValue>(panel.Draft.Root);
        panel.Draft.Wrap(start, JoinKind.Add);
        panel.Draft.Fill((DraftHole)((DraftJoin)panel.Draft.Root).Parts[1], new DraftValue(new Operand(Reach)));
        sim.Tick();
        Assert.True(panel.Reads, panel.Why);

        builder.Primary();
        sim.Tick();
        Assert.Same(page, sim.Editor.Current);
        Assert.True(form.HeldAt(0).IsExpression);

        page.Giving!.Done();
        sim.Tick();
        var act = Assert.IsType<Act>(Assert.Single(sequence.Steps));
        Assert.Equal("10 + Reach", act.Arguments[0].Tree.Written);
    }
}
