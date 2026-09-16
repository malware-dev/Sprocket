using Reactor.Host.SpaceEngineers.Editing;
using Xunit;
using Reactor.Host.SpaceEngineers.Layout;

namespace Reactor.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// A running sequence is drawn as a thing being watched, not as an editor with its hands tied.
/// </summary>
/// <remarks>
/// Core withholding the commands is tested on its own side. This is the other half, and it is the
/// half that rots quietly: nothing else notices if a target starts drawing arrows and crosses that
/// cannot do anything, and the first attempt at this greyed them instead of removing them, which read
/// as broken rather than as a readout.
/// </remarks>
public class WatchingTests
{
    [Fact]
    public void TheEditingAffordancesAreAbsentWhileItRuns()
    {
        var screen = Screen.Fresh();
        var editor = screen.Editor;

        Sequence(editor);
        editor.PressAdd("do something");
        editor.PressBlock("Airlock Light");
        editor.PressChoice("switch on");
        editor.PressPrimary();
        screen.Run(5);

        // Parked, which is how a new sequence starts: the whole toolkit is there.
        Assert.True(HasGrip(screen), "no drag grip while parked");
        Assert.True(screen.HasMark(Symbols.Delete), "no remove mark while parked");
        Assert.True(screen.Shows("+ do something"), "no add button while parked");

        editor.SwitchOn();
        screen.Run(5);

        // Running: gone, not greyed. There is nothing left drawn wearing a disabled look, because
        // there is nothing left drawn - grey says "broken, or not yours to touch", and neither is
        // true of a system that is simply running.
        Assert.False(HasGrip(screen), "drag grip still drawn while running");
        Assert.False(screen.HasMark(Symbols.Delete), "remove mark still drawn while running");
        Assert.False(screen.Shows("+ do something"), "add button still drawn while running");

        editor.SetToggle(editor.Sentence().ToggleCommand, false);
        screen.Run(5);

        // Back to parked, and the toolkit returns rather than staying gone.
        Assert.True(HasGrip(screen), "drag grip did not come back");
        Assert.True(screen.Shows("+ do something"), "add button did not come back");
    }

    /// <summary>The sentence stays readable while it runs, because reading it is the point.</summary>
    [Fact]
    public void TheSentenceIsStillFullyLegibleWhileItRuns()
    {
        var screen = Screen.Fresh();
        var editor = screen.Editor;

        Sequence(editor);
        screen.Run(5);

        var parked = screen.Text("Airlock Button").Ink;

        editor.SwitchOn();
        screen.Run(5);

        Assert.True(screen.Shows("Airlock Button"), "the sentence stopped being drawn while running");

        // Not dimmed - the same ink as when it was editable. It is simply not pressable, because
        // there is nothing there to press.
        Assert.Equal(parked, screen.Text("Airlock Button").Ink);
    }

    /// <summary>
    /// The undo pair holds its place whether or not there is anything to walk back.
    /// </summary>
    /// <remarks>
    /// Hiding them would move the marks beside them sideways every time somebody made or undid a
    /// change - under the pointer they were about to use.
    /// </remarks>
    [Fact]
    public void TheUndoPairHoldsItsPlaceWithNothingToUndo()
    {
        var screen = Screen.Fresh();
        var editor = screen.Editor;

        Sequence(editor);
        screen.Run(5);

        var undo = screen.Marks(Symbols.Undo)[0];
        var redo = screen.Marks(Symbols.Redo)[0];
        var placed = new[] { undo.At, redo.At };
        var quiet = undo.Ink;

        // Freshly built: nothing to walk back either way, but both are there and quiet.
        Assert.Equal(quiet, redo.Ink);

        editor.PressAdd("do something");
        editor.PressBlock("Airlock Light");
        editor.PressChoice("switch on");
        editor.PressPrimary();
        screen.Run(5);

        undo = screen.Marks(Symbols.Undo)[0];
        redo = screen.Marks(Symbols.Redo)[0];

        Assert.NotEqual(quiet, undo.Ink);
        Assert.Equal(quiet, redo.Ink);
        Assert.Equal(placed[0], undo.At);
        Assert.Equal(placed[1], redo.At);

        editor.Invoke(editor.Sentence().UndoCommand);
        screen.Run(5);

        undo = screen.Marks(Symbols.Undo)[0];
        redo = screen.Marks(Symbols.Redo)[0];

        Assert.Equal(quiet, undo.Ink);
        Assert.NotEqual(quiet, redo.Ink);
        Assert.Equal(placed[0], undo.At);
        Assert.Equal(placed[1], redo.At);
    }

    /// <summary>A grip is six small squares, and nothing else on the screen is.</summary>
    static bool HasGrip(Screen screen) =>
        screen.Drawings.Any(d => !d.IsText && d.Where.Width == 3 && d.Where.Height == 3);

    static void Sequence(ReactorEditor editor)
    {
        editor.NewSystem("Airlock");
        editor.PressStart("When…");
        editor.PressBlock("Airlock Button");
        editor.PressChoice("is pressed");
        editor.PressPrimary();
    }
}
