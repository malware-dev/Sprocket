using System.Globalization;
using Xunit;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Shell;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// The heading turns into the box it is edited in, and holds its place while it does.
/// </summary>
public class RenameTests
{
    [Fact]
    public void PressingAnywhereOnTheHeadingEditsItInPlace()
    {
        var screen = Screen.Fresh();
        var editor = screen.Editor;

        Sequence(editor);
        screen.Run(5);

        // A new sequence arrives with its made-up name in hand, to be given one of its own.
        Assert.True(IsEditing(screen), "a new sequence did not arrive with its name in the box");
        screen.Do("enter");
        screen.Run(5);
        Assert.False(IsEditing(screen), "the box is still up after Enter");

        var before = Header(screen);
        var heading = screen.Text(editor.Screen.CustomName());

        Assert.True(screen.HasMark(Symbols.Pencil), "nothing said the heading could be renamed");

        // Pressed near the left edge of the heading, well away from the pencil: the whole heading is
        // the target, not the mark.
        var at = Screen.Ui(heading.At.X + 8f, screen.MiddleOf(heading));
        screen.Do(Say(at.X), Say(at.Y));

        Assert.True(IsEditing(screen), "the heading did not turn into a box");

        // The one thing an in-place swap can get wrong.
        Assert.Equal(before, Header(screen));

        screen.Do("clear", "type", "Let me out", "enter");
        screen.Run(5);

        Assert.Equal("Let me out", editor.Screen.CustomName());

        // In the heading, not the whole of it. A sequence's heading is two parts and the name takes
        // the half it is for; the trigger that starts it is not somebody's to name and stays put.
        Assert.EndsWith("Let me out", editor.Screen.Title());
        Assert.False(IsEditing(screen), "the box is still up after the name was accepted");
        Assert.Equal(before, Header(screen));
    }

    /// <summary>
    /// A screen's header holds its height for as long as that screen is up.
    /// </summary>
    /// <remarks>
    /// Everything on a screen is placed from the bottom of the header, so a line arriving in it or
    /// leaving takes the whole page with it and moves what somebody is reading. Whether there is room
    /// for a subtitle is settled as a screen goes up and never again while it is up.
    /// </remarks>
    [Fact]
    public void TheHeaderHoldsItsHeightForTheLifeOfAScreen()
    {
        var screen = Screen.Fresh();
        var editor = screen.Editor;

        editor.NewSystem("Airlock");
        editor.PressStart("when");
        screen.Run(5);

        var tall = Header(screen);

        editor.PressBlock("Airlock Button");
        screen.Run(5);
        Assert.Equal(tall, Header(screen));

        editor.PressChoice("button 1 is pressed");
        screen.Run(5);
        Assert.Equal(tall, Header(screen));
    }

    /// <summary>
    /// Back sits level with the heading's capitals: the middle of the heading's line, lifted as the heading lifts it.
    /// </summary>
    [Fact]
    public void TheHeadingLinesUpWithBack()
    {
        var screen = Screen.Fresh();
        var editor = screen.Editor;

        Sequence(editor);
        screen.Run(5);

        var back = screen.Marks(Symbols.Left)[0];
        var title = screen.Drawings.First(d => d.IsText && d.Scale > 0.8f);

        Assert.Equal(screen.MiddleOf(title) + EditorView.LevelWithCapitals.Top, screen.MiddleOf(back), 1);
    }

    /// <summary>The pointer resting on the grid's name says what it is and that pressing it renames the grid.</summary>
    [Fact]
    public void TheGridsNameSaysWhatItIs()
    {
        var screen = Screen.Fresh();
        screen.Run(5);

        var name = screen.Text("Test Rig");
        var at = Screen.Ui(name.At.X + 8f, screen.MiddleOf(name));
        screen.Do("move", Say(at.X), Say(at.Y));

        Assert.True(screen.Legible.Any(t => t.Contains("Renaming it here renames the grid")), "resting on the grid's name said nothing about it");
    }

    /// <summary>
    /// Escape goes back, in one press, keeping the name typed as pressing away does: the box is only the name, shown so it
    /// can be typed into, and not something opened over the page.
    /// </summary>
    [Fact]
    public void EscapeGivesTheNameAndGoesBack()
    {
        var screen = Renaming();

        screen.Do("clear", "type", "Hangar", "escape");
        screen.Run(5);

        Assert.Equal("Hangar", screen.Editor.Program.Systems.Single().Name);
        Assert.False(IsEditing(screen), "the box is still up after Escape");
        Assert.False(screen.Editor.Screen.CanGoBack, "Escape did not go back");
    }

    /// <summary>Pressing away from the box gives the name typed in it, as Enter does.</summary>
    [Fact]
    public void PressingAwayGivesTheName()
    {
        var screen = Renaming();

        screen.Do("clear", "type", "Hangar", "700", "700");
        screen.Run(5);

        Assert.Equal("Hangar", screen.Editor.Screen.Title());
        Assert.False(IsEditing(screen), "the box is still up after pressing away");
    }

    /// <summary>A press inside the open box puts the caret where it lands, in place of the whole name being selected.</summary>
    [Fact]
    public void APressInTheBoxPutsTheCaretThere()
    {
        var screen = Renaming();

        var name = Title(screen);
        var start = Screen.Ui(name.At.X, screen.MiddleOf(name));
        screen.Do(Say(start.X), Say(start.Y), "type", "Main ", "enter");
        screen.Run(5);

        Assert.Equal("Main Airlock", screen.Editor.Screen.Title());
    }

    /// <summary>A logic system's screen with its heading pressed, so its name is in the box, all of it selected.</summary>
    static Screen Renaming()
    {
        var screen = Screen.Fresh();
        screen.Editor.NewSystem("Airlock");
        screen.Run(5);

        var name = Title(screen);
        var at = Screen.Ui(name.At.X + 8f, screen.MiddleOf(name));
        screen.Do(Say(at.X), Say(at.Y));

        Assert.True(IsEditing(screen), "the heading did not turn into a box");
        return screen;
    }

    /// <summary>The heading's name, drawn larger than anything else on the screen.</summary>
    static Drawn Title(Screen screen) =>
        screen.Drawings.First(d => d.IsText && d.Scale > 1f);

    /// <summary>
    /// How tall the bar across the top of the screen stands.
    /// </summary>
    /// <remarks>
    /// The full-width filled rectangle flush with the top of the chrome. The page behind it is also
    /// full width, and is excluded by being the height of the whole screen.
    /// </remarks>
    static int Header(Screen screen) =>
        screen.Drawings
            .Where(d => !d.IsText && d.Where.Y <= 45 && d.Where.Width == 1440 && d.Where.Height < 200)
            .Select(d => d.Where.Height)
            .First();

    /// <summary>Whether a box holds the keyboard, which is what the box being up is.</summary>
    /// <remarks>Asked of the keyboard rather than read off the drawing: a caret is a two-wide bar, and so is the spine beside a logic system's installations.</remarks>
    static bool IsEditing(Screen screen) =>
        screen.Editor.Input.Focused is Lumel.Text.TextBox;

    static string Say(float value) =>
        value.ToString("0.##", CultureInfo.InvariantCulture);

    static void Sequence(SprocketEditor editor)
    {
        editor.NewButtonSystem("Airlock");
    }
}
