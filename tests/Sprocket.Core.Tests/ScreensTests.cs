using Sprocket.Host.SpaceEngineers.Blocks;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Machine;
using VRage.Game.GUI.TextPanel;
using VRageMath;
using Xunit;

namespace Sprocket.Tests;

/// <summary>A block with several screens: each member names the screen it means.</summary>
public class ScreensTests
{
    static (Rigged Ship, Billboard Board) Built()
    {
        var ship = new Rigged();
        return (ship, ship.Rig.Weld(new Billboard(1, "Billboard")));
    }

    /// <summary>Its show text, set up screen and each screen's settings are vetted, and the game's own settings are not.</summary>
    [Fact]
    public void ItsScreenMembersAreVettedAndTheGamesSettingsAreNot()
    {
        var (ship, board) = Built();
        board.Carrying("FontSize", "Font size", ValueKind.Real, "0.1", "10");
        var kind = ship.Entity(board.Id).Kind;

        Assert.True(kind.FindAction(Screen.ShowText, Nature.Native)!.Vetted);
        Assert.True(kind.FindAction(Screens.SetUp, Nature.Native)!.Vetted);
        foreach (var id in new[] { "content", "text-color", "font-size", "alignment", "keep-aspect-ratio" })
            Assert.True(kind.FindAction(id, Nature.Value)!.Vetted, id + " is not vetted");

        Assert.False(kind.FindAction("FontSize", Nature.Value)!.Vetted);
    }

    /// <summary>Text longer than a sentence has room for is cut short in its step, however long the text is.</summary>
    [Fact]
    public void LongTextIsCutShortInItsStep()
    {
        var (ship, board) = Built();
        var automation = new Automation();
        var sequence = automation.AddSequence((Trigger)null!);
        var show = new Act(Screen.ShowText) { Form = Nature.Native, Provider = BlockKinds.Own };
        show.Targets.Add(automation.Declare(ship.World, ship.Entity(board.Id)).Name);
        show.Arguments.Add(Literal.Of("Back"));
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
        var automation = new Automation();
        var sequence = automation.AddSequence((Trigger)null!);
        var show = new Act(Screen.ShowText) { Form = Nature.Native, Provider = BlockKinds.Own };
        show.Targets.Add(automation.Declare(ship.World, ship.Entity(board.Id)).Name);
        show.Arguments.Add(Literal.Of("Back"));
        show.Arguments.Add(Literal.Of("Cycling\nStand clear"));
        sequence.Steps.Add(show);
        var runner = new AutomationRunner(automation, ship.World);
        ship.Tick();
        runner.Tick(1);

        Assert.Equal("show text Billboard Back Cycling / Stand clear", SequenceText.Step(runner.Bindings, sequence, show, null));

        runner.Call(sequence);
        for (var frame = 2; frame < 6; frame++)
        {
            ship.Tick();
            runner.Tick(frame);
        }

        Assert.Equal("Cycling\nStand clear", board.Faces[1].Text);
    }

    /// <summary>Text is shown on the screen a step names, and that screen is set to show text.</summary>
    [Fact]
    public void TextIsShownOnTheScreenNamed()
    {
        var (ship, board) = Built();
        board.Faces[1].Content = ContentType.SCRIPT;

        Ran.Step(ship, board, Screen.ShowText, Literal.Of("Back"), Literal.Of("Closed"));

        Assert.Equal("", board.Faces[0].Text);
        Assert.Equal("Closed", board.Faces[1].Text);
        Assert.Equal(ContentType.TEXT_AND_IMAGE, board.Faces[1].Content);
    }

    /// <summary>Setting up a screen sets what was given on the screen named and leaves everything left empty.</summary>
    [Fact]
    public void SettingUpAScreenSetsOnlyWhatWasGiven()
    {
        var (ship, board) = Built();
        var empty = Literal.Of("");

        Ran.Step(ship, board, Screens.SetUp, Literal.Of("Back"), empty, Literal.Colour(255, 0, 0, 255), empty, empty,
            Literal.Of(2.5), Literal.Of("center"), empty, empty, empty, empty, empty);

        Assert.Equal(new Color(255, 0, 0, 255), board.Faces[1].FontColor);
        Assert.Equal(2.5f, board.Faces[1].FontSize);
        Assert.Equal(TextAlignment.CENTER, board.Faces[1].Alignment);
        Assert.Equal(1f, board.Faces[0].FontSize);
        Assert.Equal(ContentType.NONE, board.Faces[1].Content);
    }

    /// <summary>A screen's setting is read off the screen a test names.</summary>
    [Fact]
    public void ASettingIsReadOffTheScreenNamed()
    {
        var (ship, board) = Built();
        board.Faces[1].FontSize = 3f;
        board.Faces[1].Alignment = TextAlignment.RIGHT;

        Assert.Equal(3.0, Ran.Read(ship, board, "font-size", Literal.Of("Back")).Real, 3);
        Assert.Equal(1.0, Ran.Read(ship, board, "font-size", Literal.Of("Front")).Real, 3);
        Assert.Equal(2L, Ran.Read(ship, board, "alignment", Literal.Of("Back")).Whole);
    }
}
