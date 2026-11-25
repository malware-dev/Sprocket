using Sprocket.Host.SpaceEngineers.Features.Expressions;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Model.Expressions;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Tips;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// A condition an if or a when tests is built on the expression page from comparisons and what joins flags, a flag value put
/// where a condition stands compared with on; a flag set takes one value alone.
/// </summary>
public class ConditionBuilderTests
{
    const string Program = "sprocket 3 alpha 3\n\n"
        + "system \"3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43\" as Airlock\n{\n"
        + "    let cycling = off\n"
        + "    let count = 0\n\n"
        + "    sequence 1 {\n    }\n}\n";

    static Screen Builder(bool tests, string wanted = "flag")
    {
        var screen = Screen.Holding(Program);
        screen.Run(3);
        var editor = screen.Editor;
        editor.Push(new ExpressionPage(editor, editor.Program.Systems[0], wanted, null, null, _ => { }, tests));
        screen.Run(5);
        return screen;
    }

    static ExpressionPanel Panel(Screen screen) => Assert.IsType<ExpressionPage>(screen.Editor.Current).Panel;

    /// <summary>A condition is offered the comparisons and what joins flags, beside the calculations a comparison can take.</summary>
    [Fact]
    public void AConditionOffersComparisonsAndJoins()
    {
        var tray = Panel(Builder(true)).Tray;

        foreach (var kind in ExpressionOffers.Conditions)
            Assert.True(tray.PieceFor(kind).IsVisible, kind + " was not offered");

        Assert.True(tray.PieceFor(JoinKind.Add).IsVisible);
    }

    /// <summary>A flag set takes one value alone: no calculation, comparison or join is offered, and none can be dropped.</summary>
    [Fact]
    public void AFlagSetTakesOneValueAlone()
    {
        var tray = Panel(Builder(false)).Tray;

        foreach (var kind in ExpressionOffers.Conditions)
            Assert.False(tray.PieceFor(kind).IsVisible, kind + " was offered");

        foreach (var kind in ExpressionOffers.Calculations)
            Assert.False(tray.PieceFor(kind).IsVisible, kind + " was offered");
    }

    /// <summary>A value that is not a flag offers no comparison: comparing comes only where a condition is built.</summary>
    [Fact]
    public void ANumberIsOfferedNoComparison()
    {
        var tray = Panel(Builder(false, "real")).Tray;

        Assert.False(tray.PieceFor(JoinKind.Is).IsVisible);
        Assert.False(tray.PieceFor(JoinKind.And).IsVisible);
        Assert.True(tray.PieceFor(JoinKind.Add).IsVisible);
    }

    /// <summary>A flag state put where a condition stands is compared with on, as a box that is really there.</summary>
    [Fact]
    public void AFlagStateWhereAConditionStandsIsComparedWithOn()
    {
        var screen = Builder(true);
        var panel = Panel(screen);
        Choose(screen, panel, (DraftHole)panel.Draft.Root, "cycling");

        var compared = Assert.IsType<Comparing>(panel.Draft.Built());
        Assert.Equal(Relation.Is, compared.Relation);
        Assert.Equal("cycling", Assert.IsType<StateOperand>(compared.Left).Name);
        Assert.True(Assert.IsType<Operand>(compared.Right).Value.Flag);
    }

    /// <summary>Each side of an and is a condition, so a flag state put there is compared with on too.</summary>
    [Fact]
    public void EachSideOfAnAndIsACondition()
    {
        var screen = Builder(true);
        var panel = Panel(screen);
        panel.Draft.Fill((DraftHole)panel.Draft.Root, new DraftJoin(JoinKind.And));
        screen.Run(2);
        Choose(screen, panel, (DraftHole)((DraftJoin)panel.Draft.Root).Parts[0], "cycling");

        var and = Assert.IsType<DraftJoin>(panel.Draft.Root);
        Assert.Equal(JoinKind.Is, Assert.IsType<DraftJoin>(and.Parts[0]).Kind);
    }

    /// <summary>A flag state moved within the line to where a condition stands is compared with on there, as one put there is.</summary>
    [Fact]
    public void AFlagStateMovedToAConditionIsComparedWithOn()
    {
        var screen = Builder(true);
        var panel = Panel(screen);
        var cycling = new DraftValue(new StateOperand("cycling"));
        panel.Draft.Fill((DraftHole)panel.Draft.Root, new DraftJoin(JoinKind.And,
            new DraftPiece[] { new DraftJoin(JoinKind.Is, new DraftPiece[] { cycling, new DraftValue(new Operand(Literal.Of(true))) }), new DraftHole() }));
        screen.Run(2);
        var grip = ExpressionBuilderTests.Middle(((Lumel.Layout.HStack)panel.ShownFor(cycling)!).Children[0]);
        var at = ExpressionBuilderTests.Middle(panel.ShownFor(((DraftJoin)panel.Draft.Root).Parts[1])!);
        screen.Do("drag", ExpressionBuilderTests.Say(grip.X), ExpressionBuilderTests.Say(grip.Y), ExpressionBuilderTests.Say(at.X), ExpressionBuilderTests.Say(at.Y));
        screen.Run(2);

        var moved = Assert.IsType<DraftJoin>(((DraftJoin)panel.Draft.Root).Parts[1]);
        Assert.Equal(JoinKind.Is, moved.Kind);
        Assert.Equal("cycling", ((StateOperand)((DraftValue)moved.Parts[0]).Atom).Name);
    }

    /// <summary>A flag state a not, an and or an or is put around stands where a condition does, so it is compared with on.</summary>
    [Fact]
    public void AFlagStateAnAndIsPutAroundIsComparedWithOn()
    {
        var screen = Builder(true);
        var panel = Panel(screen);
        var cycling = new DraftValue(new StateOperand("cycling"));
        panel.Draft.Fill((DraftHole)panel.Draft.Root, cycling);
        screen.Run(2);
        var grip = ExpressionBuilderTests.Middle(((Lumel.Layout.HStack)panel.Tray.PieceFor(JoinKind.And)).Children[0]);
        var at = ExpressionBuilderTests.Middle(panel.ShownFor(cycling)!);
        screen.Do("drag", ExpressionBuilderTests.Say(grip.X), ExpressionBuilderTests.Say(grip.Y), ExpressionBuilderTests.Say(at.X), ExpressionBuilderTests.Say(at.Y));
        screen.Run(2);

        var and = Assert.IsType<DraftJoin>(panel.Draft.Root);
        Assert.Equal(JoinKind.And, and.Kind);
        Assert.Equal(JoinKind.Is, Assert.IsType<DraftJoin>(and.Parts[0]).Kind);
        panel.Draft.Undo();
        Assert.Equal("cycling", ((StateOperand)Assert.IsType<DraftValue>(panel.Draft.Root).Atom).Name);
    }

    /// <summary>A not or an or dropped on a flag state takes it in, compared with on, and a not on the empty place waits for its one.</summary>
    [Theory]
    [InlineData(JoinKind.Not, 1)]
    [InlineData(JoinKind.Or, 2)]
    public void ANotAndAnOrTakeAFlagStateIn(JoinKind kind, int places)
    {
        var screen = Builder(true);
        var panel = Panel(screen);
        var grip = ExpressionBuilderTests.Middle(((Lumel.Layout.HStack)panel.Tray.PieceFor(kind)).Children[0]);
        var at = ExpressionBuilderTests.Middle(panel.ShownFor(panel.Draft.Root)!);
        screen.Do("drag", ExpressionBuilderTests.Say(grip.X), ExpressionBuilderTests.Say(grip.Y), ExpressionBuilderTests.Say(at.X), ExpressionBuilderTests.Say(at.Y));
        screen.Run(2);
        var opened = Assert.IsType<DraftJoin>(panel.Draft.Root);
        Assert.Equal(places, opened.Parts.Count);
        Assert.All(opened.Parts, part => Assert.IsType<DraftHole>(part));

        panel.Draft.Undo();
        var cycling = new DraftValue(new StateOperand("cycling"));
        panel.Draft.Fill((DraftHole)panel.Draft.Root, cycling);
        screen.Run(2);
        at = ExpressionBuilderTests.Middle(panel.ShownFor(cycling)!);
        screen.Do("drag", ExpressionBuilderTests.Say(grip.X), ExpressionBuilderTests.Say(grip.Y), ExpressionBuilderTests.Say(at.X), ExpressionBuilderTests.Say(at.Y));
        screen.Run(2);

        var join = Assert.IsType<DraftJoin>(panel.Draft.Root);
        Assert.Equal(kind, join.Kind);
        Assert.Equal(JoinKind.Is, Assert.IsType<DraftJoin>(join.Parts[0]).Kind);
    }

    /// <summary>A flag state in a set takes no comparison: it is the value.</summary>
    [Fact]
    public void AFlagStateInASetIsTheValue()
    {
        var screen = Builder(false);
        var panel = Panel(screen);
        Choose(screen, panel, (DraftHole)panel.Draft.Root, "cycling");

        Assert.IsType<StateOperand>(panel.Draft.Built());
    }

    /// <summary>Expression… in the if's sources builds the if's condition, which a comparison of a state alone writes as the test of a state.</summary>
    [Fact]
    public void AnIfIsMadeFromAnExpression()
    {
        var screen = StateEditingTests.OnSequence(Program, out var sequence);
        var editor = screen.Editor;
        editor.PressAdd(SentenceText.IfWord, editor.Texts.ValueExpression);
        screen.Run(2);
        var draft = Panel(screen).Draft;
        draft.Fill((DraftHole)draft.Root, new DraftJoin(JoinKind.And));
        var and = (DraftJoin)draft.Root;
        draft.Fill((DraftHole)and.Parts[0], new DraftJoin(JoinKind.Is, new DraftPiece[] { new DraftValue(new StateOperand("cycling")), new DraftValue(new Operand(Literal.Of(false))) }));
        draft.Fill((DraftHole)and.Parts[1], new DraftJoin(JoinKind.Above, new DraftPiece[] { new DraftValue(new StateOperand("count")), new DraftValue(new Operand(Literal.Of(2L))) }));
        screen.Run(2);
        Assert.True(Panel(screen).Reads, Panel(screen).Why);
        editor.Current.Primary();
        screen.Run(2);

        var test = Assert.IsType<ExpressionTest>(Assert.IsType<If>(sequence.Steps[0]).Test);
        Assert.Equal("cycling == off and count > 2", test.Condition.Written);
        Assert.IsType<ArgumentsSlot>(Assert.Single(editor.StepRows()[0].Slots)).Opens!.Execute();
        screen.Run(2);
        Assert.Equal("cycling == off and count > 2", Panel(screen).Draft.Built()!.Written);
    }

    /// <summary>A comparison of a state alone, built for an if, is the test of a state the if's sentence says.</summary>
    [Fact]
    public void AStateComparedAloneIsATestOfTheState()
    {
        var screen = StateEditingTests.OnSequence(Program, out var sequence);
        var editor = screen.Editor;
        editor.PressAdd(SentenceText.IfWord, editor.Texts.ValueExpression);
        screen.Run(2);
        var draft = Panel(screen).Draft;
        draft.Fill((DraftHole)draft.Root, new DraftJoin(JoinKind.Is, new DraftPiece[] { new DraftValue(new StateOperand("cycling")), new DraftValue(new Operand(Literal.Of(true))) }));
        screen.Run(2);
        editor.Current.Primary();
        screen.Run(2);

        var test = Assert.IsType<StateTest>(Assert.IsType<If>(sequence.Steps[0]).Test);
        Assert.Equal("cycling", test.State);
    }

    /// <summary>Expression… in the when's sources makes a when on the condition, which runs a new sequence.</summary>
    [Fact]
    public void AWhenIsMadeFromAnExpression()
    {
        var screen = StateEditingTests.OnSystem(Program, out var system);
        var editor = screen.Editor;
        var sequences = system.Sequences.Count;
        editor.PressStart(SentenceText.WhenWord, editor.Texts.ValueExpression);
        screen.Run(2);
        var draft = Panel(screen).Draft;
        draft.Fill((DraftHole)draft.Root, new DraftJoin(JoinKind.Or, new DraftPiece[]
        {
            new DraftJoin(JoinKind.Is, new DraftPiece[] { new DraftValue(new StateOperand("cycling")), new DraftValue(new Operand(Literal.Of(true))) }),
            new DraftJoin(JoinKind.AtLeast, new DraftPiece[] { new DraftValue(new StateOperand("count")), new DraftValue(new Operand(Literal.Of(3L))) })
        }));
        screen.Run(2);
        editor.Current.Primary();
        screen.Run(2);

        var when = Assert.IsType<ExpressionTrigger>(system.Triggers[system.Triggers.Count - 1]);
        Assert.Equal("cycling == on or count >= 3", when.Condition.Written);
        Assert.Equal(sequences + 1, system.Sequences.Count);
    }

    /// <summary>
    /// A block value for a condition, read on several blocks, is offered any and all beside the ways of combining them, and
    /// checks a comparison on each block; built alone for an if, it is the test of blocks the if's sentence says.
    /// </summary>
    [Fact]
    public void AnyOfSeveralBlocksIsChecked()
    {
        var screen = Screen.On(Rig.Demo());
        var editor = screen.Editor;
        editor.NewButtonSystem("Airlock");
        screen.Run(2);
        var sequence = editor.Program.Systems[0].Sequences[0];
        editor.PressAdd(SentenceText.IfWord, editor.Texts.ValueExpression);
        screen.Run(2);
        var panel = Panel(screen);
        var piece = panel.Tray.PieceFor(ValueSource.Reading);
        var grip = ExpressionBuilderTests.Middle(((Lumel.Layout.HStack)piece).Children[0]);
        var at = ExpressionBuilderTests.Middle(panel.ShownFor(panel.Draft.Root)!);
        screen.Do("drag", ExpressionBuilderTests.Say(grip.X), ExpressionBuilderTests.Say(grip.Y), ExpressionBuilderTests.Say(at.X), ExpressionBuilderTests.Say(at.Y));
        screen.Run(3);

        editor.TickBlock("Airlock Inner Door");
        editor.TickBlock("Airlock Outer Door");
        screen.Run(2);
        var reads = editor.Pick().Stages[0].Options.OfType<Framework.Ui.Fields.ListField>().Single();
        Assert.Contains(SentenceText.Quantified(Quantifier.Any), reads.Items.OfType<Lumel.Controls.DropDownItem>().Select(item => item.Text));
        for (var i = 0; i < 6 && reads.Text != SentenceText.Quantified(Quantifier.Any); i++)
        {
            ((Lumel.Input.IInputTarget)reads).Text('a');
            screen.Run(1);
        }

        Assert.Equal(SentenceText.Quantified(Quantifier.Any), reads.Text);
        editor.PressChoice("power");
        screen.Run(2);
        editor.Typed("is on");
        screen.Run(2);
        Assert.IsType<ExpressionPage>(editor.Current);
        Assert.True(Panel(screen).Reads, Panel(screen).Why);
        editor.Current.Primary();
        screen.Run(2);

        var test = Assert.IsType<Comparison>(sequence.Steps.OfType<If>().Last().Test);
        Assert.Equal(Quantifier.Any, test.Quantifier);
        Assert.Equal(2, editor.Seen(editor.Program.Systems[0], test.Targets).Count);
    }

    /// <summary>A block value for a value that is no condition is offered only the ways of combining several blocks.</summary>
    [Fact]
    public void AValueIsOfferedNoCheckOfBlocks()
    {
        var screen = Screen.On(Rig.Demo());
        var editor = screen.Editor;
        editor.NewButtonSystem("Airlock");
        screen.Run(2);
        var system = editor.Program.Systems[0];
        editor.Push(new ExpressionPage(editor, system, "real", null, null, _ => { }));
        screen.Run(3);
        var panel = Panel(screen);
        var grip = ExpressionBuilderTests.Middle(((Lumel.Layout.HStack)panel.Tray.PieceFor(ValueSource.Reading)).Children[0]);
        var at = ExpressionBuilderTests.Middle(panel.ShownFor(panel.Draft.Root)!);
        screen.Do("drag", ExpressionBuilderTests.Say(grip.X), ExpressionBuilderTests.Say(grip.Y), ExpressionBuilderTests.Say(at.X), ExpressionBuilderTests.Say(at.Y));
        screen.Run(3);

        editor.TickBlock("Airlock Inner Door");
        editor.TickBlock("Airlock Outer Door");
        screen.Run(2);
        var reads = editor.Pick().Stages[0].Options.OfType<Framework.Ui.Fields.ListField>().Single();
        Assert.DoesNotContain(SentenceText.Quantified(Quantifier.Any), reads.Items.OfType<Lumel.Controls.DropDownItem>().Select(item => item.Text));
    }

    /// <summary>
    /// A check of blocks says on the board whether it holds on any or all of them and what it compares, and pressing any asks
    /// for all in its place, keeping the comparison.
    /// </summary>
    [Fact]
    public void AnyIsChangedToAllByPressingIt()
    {
        var screen = Builder(true);
        var panel = Panel(screen);
        var texts = screen.Editor.Texts;
        var check = new BlockReading(new MemberKey("sprocket", "status"), Combination.Any, new List<Literal>(),
            new List<string> { "left", "right" }, Relation.Is, new Operand(Literal.Name("open")));
        panel.Draft.Fill((DraftHole)panel.Draft.Root, new DraftValue(check));
        screen.Run(2);
        var chip = (Lumel.Layout.HStack)panel.ShownFor(panel.Draft.Root)!;
        var any = chip.Children.OfType<Lumel.Controls.Button>().Single(button => button.Text == texts.ExpressionKindAny);
        Assert.Contains(chip.Children.OfType<Lumel.Controls.Button>(), button => button.Text.EndsWith(" open"));

        any.Command!.Execute();
        screen.Run(2);
        screen.Editor.PressAnswer(texts.ExpressionKindAll);
        screen.Run(2);

        var all = Assert.IsType<BlockReading>(Assert.IsType<DraftValue>(panel.Draft.Root).Atom);
        Assert.Equal(Combination.All, all.Combine);
        Assert.Equal(Relation.Is, all.Relation);
        Assert.Equal("open", ((Operand)all.Compared).Value.Text);
    }

    /// <summary>A box that always comes to one fixed value is marked, its tip, which stays while tips are off, saying the value Optimize puts in its place.</summary>
    [Fact]
    public void ABoxThatAlwaysComesToOneValueIsMarked()
    {
        var screen = Builder(true);
        var panel = Panel(screen);
        var and = new DraftJoin(JoinKind.And, new DraftPiece[] { Cycling(), new DraftValue(new Operand(Literal.Of(false))) });
        panel.Draft.Fill((DraftHole)panel.Draft.Root, and);
        screen.Run(2);

        var box = panel.ShownFor(and)!;
        Assert.True(box.Is(LookStates.Warned));
        Assert.IsType<KeptTip>(box.Tip);
        Assert.False(panel.ShownFor(and.Parts[1])!.Is(LookStates.Warned));
        Assert.Equal(screen.Editor.Texts.Codex.ExpressionAlwaysAbout.Replace("{value}", "off"), TipOf(screen, box));
    }

    /// <summary>A part that changes nothing beside the rest of its box is marked, its tip, which stays while tips are off, saying Optimize takes it out.</summary>
    [Fact]
    public void APartThatChangesNothingIsMarked()
    {
        var screen = Builder(true);
        var panel = Panel(screen);
        var and = new DraftJoin(JoinKind.And, new DraftPiece[] { Cycling(), new DraftValue(new Operand(Literal.Of(true))) });
        panel.Draft.Fill((DraftHole)panel.Draft.Root, and);
        screen.Run(2);

        var on = panel.ShownFor(and.Parts[1])!;
        Assert.False(panel.ShownFor(and)!.Is(LookStates.Warned));
        Assert.False(panel.ShownFor(and.Parts[0])!.Is(LookStates.Warned));
        Assert.True(on.Is(LookStates.Warned));
        Assert.IsType<KeptTip>(on.Tip);
        Assert.Equal(screen.Editor.Texts.Codex.ExpressionNoDifferenceAbout, TipOf(screen, on));
    }

    // The cycling state compared with on.
    static DraftJoin Cycling() =>
        new DraftJoin(JoinKind.Is, new DraftPiece[] { new DraftValue(new StateOperand("cycling")), new DraftValue(new Operand(Literal.Of(true))) });

    // The words of the tip that opens once the pointer has rested on element.
    static string TipOf(Screen screen, Lumel.Element element)
    {
        var at = ExpressionBuilderTests.Middle(element);
        screen.Do("move", ExpressionBuilderTests.Say(at.X), ExpressionBuilderTests.Say(at.Y), "tick", Styles.TipDelay.ToString());
        return screen.Scene.CodexTip!.Page!.Text;
    }

    // Drops the state piece on hole and chooses the state named name.
    static void Choose(Screen screen, ExpressionPanel panel, DraftHole hole, string name)
    {
        var piece = panel.Tray.PieceFor(ValueSource.State);
        var to = panel.ShownFor(hole)!;
        var grip = ExpressionBuilderTests.Middle(((Lumel.Layout.HStack)piece).Children[0]);
        var at = ExpressionBuilderTests.Middle(to);
        screen.Do("drag", ExpressionBuilderTests.Say(grip.X), ExpressionBuilderTests.Say(grip.Y), ExpressionBuilderTests.Say(at.X), ExpressionBuilderTests.Say(at.Y));
        screen.Run(3);
        screen.Editor.PressAnswer(name);
        screen.Run(2);
    }
}
