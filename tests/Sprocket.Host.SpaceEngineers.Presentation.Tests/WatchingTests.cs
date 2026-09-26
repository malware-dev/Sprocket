using Sprocket.Host.SpaceEngineers.Editing;
using Xunit;
using Sprocket.Host.SpaceEngineers.Look;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// A locked sequence is the same page with nothing to press.
/// </summary>
/// <remarks>
/// Core withholding the commands is tested on its own side. This is the other half, and it is the
/// half that rots quietly: nothing else notices if locking starts taking controls away, and every one
/// taken away moves what stood beside or below it.
/// </remarks>
public class WatchingTests
{
    [Fact]
    public void NothingMovesWhenItIsSwitchedOn()
    {
        var screen = Screen.Fresh();
        var editor = screen.Editor;

        Sequence(editor);
        editor.PressAdd("do something");
        editor.PressBlock("Airlock Light");
        editor.PressChoice("Switch on");
        editor.PressPrimary();
        screen.Run(5);

        var parked = Places(screen);
        var adders = screen.Text("+ do something").At.Y;

        editor.SwitchOn();
        screen.Run(5);

        Assert.Equal(parked, Places(screen));

        // The adders' line says why there are none, on the same line.
        Assert.False(screen.Shows("+ do something"), "add button still offered while locked");
        Assert.Equal(adders, screen.Text(editor.Sentence().LockedText).At.Y);

        editor.SetToggle(editor.Sentence().ToggleCommand, false);
        screen.Run(5);

        Assert.Equal(parked, Places(screen));
        Assert.True(screen.Shows("+ do something"), "add button did not come back");
    }

    /// <summary>Where the step's words, its grips and every bin and arrow stand.</summary>
    /// <remarks>A grip is six small squares, and nothing else on the screen is.</remarks>
    static List<string> Places(Screen screen)
    {
        var places = new List<string> { "Airlock Light " + screen.Text("Airlock Light").At };

        places.AddRange(screen.Drawings
            .Where(d => !d.IsText && d.Where.Width == 3 && d.Where.Height == 3)
            .Select(d => "grip " + d.Where));

        foreach (var symbol in new[] { Symbols.Delete, Symbols.Undo, Symbols.Redo })
            places.AddRange(screen.Marks(symbol).Select(d => symbol + " " + d.At));

        return places;
    }

    /// <summary>The sentence stays readable while it runs, because reading it is the point.</summary>
    [Fact]
    public void TheSentenceIsStillFullyLegibleWhileItRuns()
    {
        var screen = Screen.Fresh();
        var editor = screen.Editor;

        Sequence(editor);
        editor.PressAdd("do something");
        editor.PressBlock("Airlock Light");
        editor.PressChoice("Switch on");
        editor.PressPrimary();
        screen.Run(5);

        var parked = screen.Text("Airlock Light").Ink;

        editor.SwitchOn();
        screen.Run(5);

        Assert.True(screen.Shows("Airlock Light"), "the sentence stopped being drawn while running");

        // Not dimmed - the same ink as when it was editable. It is simply not pressable, because
        // there is nothing there to press.
        Assert.Equal(parked, screen.Text("Airlock Light").Ink);
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
        editor.PressChoice("Switch on");
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

    /// <summary>A copy somebody else is editing says so at the head of every page, the first included.</summary>
    [Fact]
    public void SomebodyElsesSprocketSaysWhoseItIs()
    {
        var screen = Screen.Fresh();
        Sequence(screen.Editor);
        screen.Editor.GoToRoot();
        screen.Editor.Holder = "Enenra";
        screen.Run(60);

        Assert.True(screen.Shows("Enenra is editing this block's automation."), "the first page does not say whose it is");
    }

    static void Sequence(SprocketEditor editor)
    {
        editor.NewSystem("Airlock");
        editor.PressStart("When…");
        editor.PressBlock("Airlock Button");
        editor.PressChoice("is pressed");
        editor.PressPrimary();
    }
}
