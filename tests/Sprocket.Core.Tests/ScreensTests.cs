using Sprocket.Host.SpaceEngineers.Blocks;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Storage;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Machine;
using VRage.Game.GUI.TextPanel;
using VRageMath;
using Xunit;

namespace Sprocket.Tests;

/// <summary>A block's screens, the type the host provides as ext.screen: each member acts on the screen its slot holds.</summary>
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
        var act = new Act(actionId) { Form = Nature.Native, Provider = BlockKinds.Own };
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
        var screen = HostTypes.Returned("screen");

        Assert.Same(screen, kind.FindAction(ScreenType.ShowText, Nature.Native)!.Type);
        Assert.True(kind.FindAction(ScreenType.ShowText, Nature.Native)!.Vetted);
        Assert.True(kind.FindAction(ScreenType.SetUp, Nature.Native)!.Vetted);
        foreach (var id in new[] { "content", "text-color", "font-size", "alignment", "keep-aspect-ratio" })
        {
            Assert.True(kind.FindAction(id, Nature.Value)!.Vetted, id + " is not vetted");
            Assert.Same(screen, kind.FindAction(id, Nature.Value)!.Type);
        }

        Assert.False(kind.FindAction("FontSize", Nature.Value)!.Vetted);
    }

    /// <summary>A block offers each of its screens by number and by the name the game gives it.</summary>
    [Fact]
    public void ABlockOffersItsScreensByNumberAndName()
    {
        var (ship, board) = Built();
        var facets = ship.Entity(board.Id).Kind.Facets(HostTypes.Returned("screen")!);

        Assert.Equal(new[] { 1, 2 }, facets.Select(facet => facet.Number));
        Assert.Equal(new[] { "Front", "Back" }, facets.Select(facet => facet.Name));
    }

    /// <summary>Text longer than a sentence has room for is cut short in its step, however long the text is.</summary>
    [Fact]
    public void LongTextIsCutShortInItsStep()
    {
        var (ship, board) = Built();
        var automation = new Automation();
        var sequence = automation.AddSequence((Trigger)null!);
        var show = new Act(ScreenType.ShowText) { Form = Nature.Native, Provider = BlockKinds.Own };
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
        var (_, runner, sequence, installation) = Slotted(ship, board, 2, ScreenType.SetUp, empty, Literal.Colour(255, 0, 0, 255),
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

    /// <summary>A screen's setting is read off the screen its slot holds.</summary>
    [Fact]
    public void ASettingIsReadOffTheScreenItsSlotHolds()
    {
        var (ship, board) = Built();
        board.Faces[1].FontSize = 3f;
        board.Faces[1].Alignment = TextAlignment.RIGHT;
        ship.Tick();
        var entity = ship.Entity(board.Id);

        Assert.True(Probe.Of(entity, BlockKinds.Own, "font-size", new Literal[0], Quantifier.Any, Relation.Is, Literal.Of(3.0), facet: 2)!.Holds());
        Assert.True(Probe.Of(entity, BlockKinds.Own, "font-size", new Literal[0], Quantifier.Any, Relation.Is, Literal.Of(1.0), facet: 1)!.Holds());
        Assert.True(Probe.Of(entity, BlockKinds.Own, "alignment", new Literal[0], Quantifier.Any, Relation.Is, Literal.Name("right"), facet: 2)!.Holds());
    }

    /// <summary>A slot of screens is written as one that expects ext.screen, each installation's answer a screen of each block, and read back the same.</summary>
    [Fact]
    public void ASlotOfScreensReadsAndWritesBackAsItWas()
    {
        var (ship, board) = Built();
        var (program, _, _, _) = Slotted(ship, board, 2, ScreenType.ShowText, Literal.Of("Docked"));
        var values = new Values();

        var text = ProgramText.Save(program, values);
        Assert.Contains("slot status expects ext.screen", text);
        Assert.Contains("status = [ ext.screen(2) on [ ", text);

        var read = new Automation();
        Assert.Equal("", ProgramText.Load(text, read, values));
        var system = read.Systems[0];
        Assert.Equal(HostTypes.All[0].Id, system.FindPart("status")!.Holds);
        Assert.Equal(new[] { 2 }, system.Instances[0].Facets("status"));
        Assert.Equal(text, ProgramText.Save(read, values));
    }

    /// <summary>A slot can expect only a type the host provides; anything else qualified is refused, saying so.</summary>
    [Fact]
    public void ASlotExpectingNoTypeTheHostProvidesIsRefused()
    {
        var (ship, board) = Built();
        var (program, _, _, _) = Slotted(ship, board, 2, ScreenType.ShowText, Literal.Of("Docked"));
        var values = new Values();
        var text = ProgramText.Save(program, values).Replace("expects ext.screen", "expects ext.window");

        Assert.Contains("no type 'ext.window' the host provides", ProgramText.Load(text, new Automation(), values));
    }
}
