using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Xunit;

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
        editor.PressAdd("task");
        editor.PressBlock("Airlock Light");
        editor.PressChoice("power");
        editor.Pick().Arguments!.Form.Fields[0].Text = "on";
        editor.PressPrimary();
        screen.Run(5);

        var parked = Places(screen);
        var adders = screen.TopOf(screen.Text("+ step"));

        editor.SwitchOn();
        screen.Run(5);

        Assert.Equal(parked, Places(screen));

        // The adders' line says why there are none, on the same line.
        Assert.False(screen.Shows("+ step"), "add button still offered while locked");
        Assert.Equal(adders, screen.TopOf(screen.Text(editor.Sentence().LockedText)), 3);

        // A logic system with installations asks before it stops them.
        editor.SwitchSystem(false);
        screen.Run(5);
        editor.PressChoice("Switch them off");
        screen.Run(5);

        Assert.Equal(parked, Places(screen));
        Assert.True(screen.Shows("+ step"), "add button did not come back");
    }

    /// <summary>Where the step's words, its grips and every bin and arrow stand.</summary>
    /// <remarks>A grip is six small squares, and nothing else on the screen is.</remarks>
    static List<string> Places(Screen screen)
    {
        var places = new List<string> { "slot " + screen.Text(Slot(screen)).At };

        places.AddRange(screen.Drawings
            .Where(d => !d.IsText && d.Where.Width == 3 && d.Where.Height == 3)
            .Select(d => "grip " + d.Where));

        foreach (var symbol in new[] { Symbols.Delete, Symbols.Undo, Symbols.Redo })
            places.AddRange(screen.Marks(symbol).Select(d => symbol + " " + d.At));

        return places;
    }

    /// <summary>A sequence started while switched off counts the starts it refused, one start said as one.</summary>
    [Fact]
    public void TheRefusedStartsAreCounted()
    {
        var screen = Screen.Fresh();
        var editor = screen.Editor;

        Sequence(editor);
        editor.PressAdd("task");
        editor.PressBlock("Airlock Light");
        editor.PressChoice("power");
        editor.Pick().Arguments!.Form.Fields[0].Text = "on";
        editor.PressPrimary();
        screen.Run(5);

        screen.Scene.Ship.Press("Airlock Button");
        screen.Run(2);
        Assert.True(screen.Shows("1 start refused while switched off"), string.Join(" | ", screen.Legible));

        screen.Scene.Ship.Press("Airlock Button");
        screen.Run(2);
        Assert.True(screen.Shows("2 starts refused while switched off"), string.Join(" | ", screen.Legible));
    }

    /// <summary>The sentence stays readable while it runs, because reading it is the point.</summary>
    [Fact]
    public void TheSentenceIsStillFullyLegibleWhileItRuns()
    {
        var screen = Screen.Fresh();
        var editor = screen.Editor;

        Sequence(editor);
        editor.PressAdd("task");
        editor.PressBlock("Airlock Light");
        editor.PressChoice("power");
        editor.Pick().Arguments!.Form.Fields[0].Text = "on";
        editor.PressPrimary();
        screen.Run(5);

        var parked = screen.Text(Slot(screen)).Ink;

        editor.SwitchOn();
        screen.Run(5);

        Assert.True(screen.Shows(Slot(screen)), "the sentence stopped being drawn while running");

        // Not dimmed - the same ink as when it was editable. It is simply not pressable, because
        // there is nothing there to press.
        Assert.Equal(parked, screen.Text(Slot(screen)).Ink);
    }

    /// <summary>
    /// The undo pair holds its place whether or not there is anything to walk either way.
    /// </summary>
    /// <remarks>
    /// Hiding them would move the marks beside them sideways every time somebody made or undid a
    /// change - under the pointer they were about to use.
    /// </remarks>
    [Fact]
    public void TheUndoPairHoldsItsPlace()
    {
        var screen = Screen.Fresh();
        var editor = screen.Editor;

        Sequence(editor);
        screen.Run(5);

        var undo = screen.Marks(Symbols.Undo)[0];
        var redo = screen.Marks(Symbols.Redo)[0];
        var placed = new[] { undo.At, redo.At };
        var quiet = redo.Ink;

        // Freshly built: the trigger it was made with to walk back, nothing to walk forward, and both there.
        Assert.NotEqual(quiet, undo.Ink);

        editor.PressAdd("task");
        editor.PressBlock("Airlock Light");
        editor.PressChoice("power");
        editor.Pick().Arguments!.Form.Fields[0].Text = "on";
        editor.PressPrimary();
        screen.Run(5);

        undo = screen.Marks(Symbols.Undo)[0];
        redo = screen.Marks(Symbols.Redo)[0];

        Assert.NotEqual(quiet, undo.Ink);
        Assert.Equal(quiet, redo.Ink);
        Assert.Equal(placed[0], undo.At);
        Assert.Equal(placed[1], redo.At);

        editor.Screen.Undo!.Execute();
        screen.Run(5);

        undo = screen.Marks(Symbols.Undo)[0];
        redo = screen.Marks(Symbols.Redo)[0];

        // The step walked back: redo lit as well now, both in the same places.
        Assert.NotEqual(quiet, undo.Ink);
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
        editor.NewButtonSystem("Airlock");
    }

    /// <summary>The slot the first task names, which its line says.</summary>
    static string Slot(Screen screen) =>
        screen.Editor.Program.Sequences[0].Steps.OfType<Act>().First().Targets[0];
}
