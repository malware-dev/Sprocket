using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Language.Reading;
using Sprocket.Host.SpaceEngineers.Framework.Language.Writing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Running;
using Sprocket.Host.SpaceEngineers.Framework.Text;
using Sprocket.Sim.Machine;
using VRage.Game.GUI.TextPanel;
using VRageMath;
using Xunit;

namespace Sprocket.Tests;

/// <summary>A block's screens, the type the host provides as sprocket.screen: each member acts on the screen its slot holds.</summary>
public class ScreensTests
{
    static (Rigged Ship, Billboard Board) Built()
    {
        var ship = new Rigged();
        return (ship, ship.Rig.Weld(new Billboard(1, "Billboard")));
    }

    /// <summary>
    /// A logic system whose one installation holds <paramref name="facet" /> of the billboard in its slot status, and one
    /// sequence doing <paramref name="actionId" /> on that slot with <paramref name="arguments" />.
    /// </summary>
    static (Automation Program, AutomationRunner Runner, Sequence Sequence, SystemInstance Installation) Slotted(Rigged ship,
        Billboard board, int facet, string actionId, params Literal[] arguments)
    {
        var program = new Automation();
        var system = program.AddSystem();
        system.Name = "Status";
        system.IsBlueprint = true;
        var installation = system.AddInstance();
        system.PartFor("status").Holds = HostTypes.All[0].Id;
        installation.Fill("status", new[] { program.Declare(ship.World, ship.Entity(board.Id)).Name }, new[] { facet });

        var sequence = program.AddSequence(system, null!);
        var act = new Act(actionId) { Form = ReachKind.Native, Provider = BlockKinds.Own };
        act.Targets.Add("status");
        foreach (var argument in arguments)
            act.Arguments.Add(argument);

        sequence.Steps.Add(act);
        return (program, new AutomationRunner(program, ship.World), sequence, installation);
    }

    static void Run(Rigged ship, AutomationRunner runner, Sequence sequence, SystemInstance installation)
    {
        ship.Tick();
        runner.Tick(1);
        runner.Call(sequence, installation);
        for (var frame = 2; frame < 6; frame++)
        {
            ship.Tick();
            runner.Tick(frame);
        }
    }

    /// <summary>Its show text, set up screen and each screen's settings are vetted members of the screen, and the game's own settings are not vetted.</summary>
    [Fact]
    public void ItsScreenMembersAreVettedMembersOfTheScreen()
    {
        var (ship, board) = Built();
        board.Carrying("FontSize", "Font size", ValueKind.Real, "0.1", "10");
        var kind = ship.Entity(board.Id).Kind;
        var screen = HostTypes.Returned("get_screen");

        Assert.Same(screen, kind.FindAction(ScreenType.ShowText, ReachKind.Native)!.Type);
        Assert.True(kind.FindAction(ScreenType.ShowText, ReachKind.Native)!.Vetted);
        Assert.True(kind.FindAction(ScreenType.SetUp, ReachKind.Native)!.Vetted);
        foreach (var id in new[] { "content", "app", "text_color", "font_size", "alignment", "keep_aspect_ratio" })
        {
            Assert.True(kind.FindAction(id, ReachKind.Value)!.Vetted, id + " is not vetted");
            Assert.Same(screen, kind.FindAction(id, ReachKind.Value)!.Type);
        }

        Assert.False(kind.FindAction("FontSize", ReachKind.Value)!.Vetted);
    }

    /// <summary>Text longer than a sentence has room for is cut short in its step, however long the text is.</summary>
    [Fact]
    public void LongTextIsCutShortInItsStep()
    {
        var (ship, board) = Built();
        var automation = new Automation();
        var sequence = automation.AddSequence((Trigger)null!);
        var show = new Act(ScreenType.ShowText) { Form = ReachKind.Native, Provider = BlockKinds.Own };
        show.Targets.Add(automation.Declare(ship.World, ship.Entity(board.Id)).Name);
        show.Arguments.Add(Literal.Of(new string('x', 5000)));
        sequence.Steps.Add(show);
        var runner = new AutomationRunner(automation, ship.World);
        ship.Tick();
        runner.Tick(1);

        var said = SequenceText.Step(runner.Bindings, sequence, show, null);
        Assert.EndsWith("…", said);
        Assert.True(said.Length < 100, "the step says " + said.Length + " characters");
    }

    /// <summary>Text of several lines is shown with its line breaks, and its step reads on one line, a slash for each break.</summary>
    [Fact]
    public void TextOfSeveralLinesIsShownWhole()
    {
        var (ship, board) = Built();
        var (_, runner, sequence, installation) = Slotted(ship, board, 2, ScreenType.ShowText, Literal.Of("Cycling\nStand clear"));
        ship.Tick();
        runner.Tick(1);

        Assert.Equal("show text Billboard Cycling / Stand clear", SequenceText.Step(runner.Bindings, sequence, (Act)sequence.Steps[0], installation));

        Run(ship, runner, sequence, installation);
        Assert.Equal("Cycling\nStand clear", board.Faces[1].Text);
    }

    /// <summary>Text is written on the screen its slot holds, which keeps what it is set to show, so a mod drawing it still can.</summary>
    [Fact]
    public void TextIsShownOnTheScreenItsSlotHolds()
    {
        var (ship, board) = Built();
        board.Faces[1].Content = ContentType.SCRIPT;
        var (_, runner, sequence, installation) = Slotted(ship, board, 2, ScreenType.ShowText, Literal.Of("Closed"));

        Run(ship, runner, sequence, installation);

        Assert.Equal("", board.Faces[0].Text);
        Assert.Equal("Closed", board.Faces[1].Text);
        Assert.Equal(ContentType.SCRIPT, board.Faces[1].Content);
    }

    /// <summary>Text on a block named without a slot holding one of its screens is written on its first screen.</summary>
    [Fact]
    public void TextWithNoScreenHeldIsShownOnTheFirst()
    {
        var (ship, board) = Built();

        Ran.Step(ship, board, ScreenType.ShowText, Literal.Of("Closed"));

        Assert.Equal("Closed", board.Faces[0].Text);
        Assert.Equal("", board.Faces[1].Text);
    }

    /// <summary>Setting up a screen sets what was given on the screen its slot holds and leaves everything left empty.</summary>
    [Fact]
    public void SettingUpAScreenSetsOnlyWhatWasGiven()
    {
        var (ship, board) = Built();
        var empty = Literal.Of("");
        var (_, runner, sequence, installation) = Slotted(ship, board, 2, ScreenType.SetUp, empty, empty, Literal.Color(255, 0, 0, 255),
            empty, empty, Literal.Of(2.5), Literal.Of("center"), empty, empty, empty, empty, empty);

        Run(ship, runner, sequence, installation);

        Assert.Equal(new Color(255, 0, 0, 255), board.Faces[1].FontColor);
        Assert.Equal(2.5f, board.Faces[1].FontSize);
        Assert.Equal(TextAlignment.CENTER, board.Faces[1].Alignment);
        Assert.Equal(1f, board.Faces[0].FontSize);
        Assert.Equal(ContentType.NONE, board.Faces[1].Content);
    }

    /// <summary>A block lacking the screen its slot holds is said to, by name, and nothing is written on any of its screens.</summary>
    [Fact]
    public void ABlockLackingItsScreenIsSaidToAndLeftAlone()
    {
        var (ship, board) = Built();
        var (_, runner, sequence, installation) = Slotted(ship, board, 3, ScreenType.ShowText, Literal.Of("Closed"));
        ship.Tick();
        runner.Tick(1);

        var fault = SequenceSummary.Checked(ship.World, runner.Bindings, sequence, installation);
        Assert.Equal(SequenceProblem.MissingFacet, fault.Problem);
        Assert.Equal("Billboard", fault.Which);

        Run(ship, runner, sequence, installation);
        Assert.Equal("", board.Faces[0].Text);
        Assert.Equal("", board.Faces[1].Text);
    }

    /// <summary>A step using a screen through a block slot is warned of, naming the slot, and still runs; one through a screen slot is not warned of.</summary>
    [Fact]
    public void AScreenOnABlockSlotIsWarnedOfAndOnAScreenSlotIsNot()
    {
        var (ship, board) = Built();
        var (program, runner, sequence, installation) = Slotted(ship, board, 1, ScreenType.ShowText, Literal.Of("Closed"));
        program.Systems[0].FindPart("status")!.Holds = "";
        ship.Tick();
        runner.Tick(1);

        var fault = SequenceSummary.Checked(ship.World, runner.Bindings, sequence, installation);
        Assert.Equal(SequenceProblem.ScreenOnBlockSlot, fault.Problem);
        Assert.Equal("status", fault.Which);
        Assert.True(fault.IsWarning);
        Assert.False(fault.Parks);
        Assert.Equal("This step is outdated and now needs a screen slot. Press status to make one.",
            SequenceText.Problem(new Texts(), fault, sequence));

        var (screenShip, screenBoard) = Built();
        var (_, screenRunner, screenSequence, screenInstallation) = Slotted(screenShip, screenBoard, 1, ScreenType.ShowText, Literal.Of("Closed"));
        screenShip.Tick();
        screenRunner.Tick(1);

        Assert.NotEqual(SequenceProblem.ScreenOnBlockSlot, SequenceSummary.Checked(screenShip.World, screenRunner.Bindings, screenSequence, screenInstallation).Problem);
    }

    /// <summary>Setting up a screen to run an app sets which app, on the screen its slot holds, and to run no app sets it running none, so a script can draw on it.</summary>
    [Theory]
    [InlineData("", "TSS_Weather")]
    [InlineData("TSS_Weather", "")]
    public void SettingUpAScreenSetsItsApp(string before, string app)
    {
        var (ship, board) = Built();
        board.Faces[1].App = before;
        var empty = Literal.Of("");
        var (_, runner, sequence, installation) = Slotted(ship, board, 2, ScreenType.SetUp, Literal.Of("app"), Literal.Of(app),
            empty, empty, empty, empty, empty, empty, empty, empty, empty, empty);

        Run(ship, runner, sequence, installation);

        Assert.Equal(ContentType.SCRIPT, board.Faces[1].Content);
        Assert.Equal(app, board.Faces[1].App);
        Assert.Equal("", board.Faces[0].App);
    }

    /// <summary>Showing an image shows it on the screen its slot holds alone, which is set to show text and images.</summary>
    [Fact]
    public void ShowingAnImageShowsItOnTheScreenItsSlotHolds()
    {
        var (ship, board) = Built();
        var (_, runner, sequence, installation) = Slotted(ship, board, 2, ScreenType.ShowImage, Literal.Of("Danger"));

        Run(ship, runner, sequence, installation);

        Assert.Equal(new[] { "Danger" }, board.Faces[1].Images);
        Assert.Equal(ContentType.TEXT_AND_IMAGE, board.Faces[1].Content);
        Assert.Empty(board.Faces[0].Images);
    }

    /// <summary>The text a screen shows, the app it runs and its settings are read off the screen its slot holds.</summary>
    [Fact]
    public void WhatAScreenShowsAndItsSettingsAreReadOffTheScreenItsSlotHolds()
    {
        var (ship, board) = Built();
        board.Faces[1].Text = "DOCKED";
        board.Faces[1].App = "TSS_Weather";
        board.Faces[1].FontSize = 3f;
        board.Faces[1].Alignment = TextAlignment.RIGHT;
        ship.Tick();
        var entity = ship.Entity(board.Id);

        Assert.True(Probe.Of(entity, BlockKinds.Own, ScreenType.TextShown, new Literal[0], Quantifier.Any, Relation.Is, Literal.Of("DOCKED"), facet: 2)!.Holds());
        Assert.False(Probe.Of(entity, BlockKinds.Own, ScreenType.TextShown, new Literal[0], Quantifier.Any, Relation.Is, Literal.Of("DOCKED"), facet: 1)!.Holds());
        Assert.True(Probe.Of(entity, BlockKinds.Own, ScreenType.App, new Literal[0], Quantifier.Any, Relation.Is, Literal.Name("TSS_Weather"), facet: 2)!.Holds());
        Assert.True(Probe.Of(entity, BlockKinds.Own, "font_size", new Literal[0], Quantifier.Any, Relation.Is, Literal.Of(3.0), facet: 2)!.Holds());
        Assert.True(Probe.Of(entity, BlockKinds.Own, "font_size", new Literal[0], Quantifier.Any, Relation.Is, Literal.Of(1.0), facet: 1)!.Holds());
        Assert.True(Probe.Of(entity, BlockKinds.Own, "alignment", new Literal[0], Quantifier.Any, Relation.Is, Literal.Name("right"), facet: 2)!.Holds());
    }

    /// <summary>A version 1 program names the function returning a screen <c>screen</c>; it reads as <c>get_screen</c>, and is written so.</summary>
    [Fact]
    public void AnEarlierProgramsScreenFunctionIsGetScreen()
    {
        var (ship, board) = Built();
        var (program, _, _, _) = Slotted(ship, board, 2, ScreenType.ShowText, Literal.Of("Docked"));
        var values = new Values();
        var text = ProgramWriter.Save(program, values).Replace("sprocket 2 alpha 2", "sprocket 1").Replace("sprocket.get_screen(", "sprocket.screen(");

        var read = new Automation();
        Assert.Equal("", ProgramReader.Load(text, read, values));
        Assert.Contains("status = [ sprocket.get_screen(2) on [ ", ProgramWriter.Save(read, values));
    }

    /// <summary>A slot of screens is written as one that expects sprocket.screen, each installation's answer a screen of each block, and read back the same.</summary>
    [Fact]
    public void ASlotOfScreensReadsAndWritesBackAsItWas()
    {
        var (ship, board) = Built();
        var (program, _, _, _) = Slotted(ship, board, 2, ScreenType.ShowText, Literal.Of("Docked"));
        var values = new Values();

        var text = ProgramWriter.Save(program, values);
        Assert.Contains("slot status expects sprocket.screen", text);
        Assert.Contains("status = [ sprocket.get_screen(2) on [ ", text);

        var read = new Automation();
        Assert.Equal("", ProgramReader.Load(text, read, values));
        var system = read.Systems[0];
        Assert.Equal(HostTypes.All[0].Id, system.FindPart("status")!.Holds);
        Assert.Equal(new[] { 2 }, system.Instances[0].Facets("status"));
        Assert.Equal(text, ProgramWriter.Save(read, values));
    }

    /// <summary>A slot can expect only a type the host provides; anything else qualified is refused, saying so.</summary>
    [Fact]
    public void ASlotExpectingNoTypeTheHostProvidesIsRefused()
    {
        var (ship, board) = Built();
        var (program, _, _, _) = Slotted(ship, board, 2, ScreenType.ShowText, Literal.Of("Docked"));
        var values = new Values();
        var text = ProgramWriter.Save(program, values).Replace("expects sprocket.screen", "expects sprocket.window");

        Assert.Contains("no type 'sprocket.window' the host provides", ProgramReader.Load(text, new Automation(), values));
    }
}
