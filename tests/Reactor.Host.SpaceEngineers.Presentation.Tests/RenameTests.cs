using System.Globalization;
using Reactor.Host.SpaceEngineers.Core.Editing;
using Xunit;
using Reactor.Host.SpaceEngineers.Presentation.Ui;

namespace Reactor.Host.SpaceEngineers.Presentation.Tests;

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

        var before = Header(screen);
        var heading = screen.Drawings.Last(d => d.IsText && d.Scale > 0.8f);

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
    /// A picker's subtitle empties the moment the pick is complete, and a header that shrank when it
    /// did would move the screen under somebody mid-task. The line stays reserved instead.
    /// <para>
    /// Only for as long as the screen is up, and only because the picker is currently the odd one out.
    /// The header is designed without a subtitle - one was rare enough that reserving its line made the
    /// header look wrong on every other screen - so a picker standing 77 units against a sequence's 53
    /// is the picker disagreeing with the design rather than the header bending for it. The picker is
    /// what wants redesigning; see docs/Editor.md.
    /// </para>
    /// </remarks>
    [Fact]
    public void TheHeaderHoldsItsHeightWhileItsSubtitleComesAndGoes()
    {
        var screen = Screen.Fresh();
        var editor = screen.Editor;

        editor.NewSystem("Airlock");
        editor.PressStart("When…");
        screen.Run(5);

        var tall = Header(screen);
        Assert.NotEqual("", editor.Screen.Subtitle);

        editor.PressBlock("Airlock Button");
        screen.Run(5);

        // A different subtitle, same screen.
        Assert.NotEqual("", editor.Screen.Subtitle);
        Assert.Equal(tall, Header(screen));

        editor.PressChoice("is pressed");
        screen.Run(5);

        // Complete: the subtitle emptied, and the header must not have moved.
        Assert.Equal("", editor.Screen.Subtitle);
        Assert.Equal(tall, Header(screen));
    }

    /// <summary>
    /// The heading and back sit on the same line.
    /// </summary>
    /// <remarks>
    /// Back used to be centred against the title and subtitle together, which put it half a line
    /// below the title on every screen with no subtitle. Two pieces of text at different sizes share
    /// a line by their middles, not by their tops.
    /// </remarks>
    [Fact]
    public void TheHeadingLinesUpWithBack()
    {
        var screen = Screen.Fresh();
        var editor = screen.Editor;

        Sequence(editor);
        screen.Run(5);

        var back = screen.Marks(Symbols.Left)[0];
        var title = screen.Drawings.First(d => d.IsText && d.Scale > 0.8f);

        Assert.Equal(screen.MiddleOf(back), screen.MiddleOf(title), 1);
    }

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

    /// <summary>A caret is a two-wide bar, and it is only ever drawn into a box being typed in.</summary>
    static bool IsEditing(Screen screen) =>
        screen.Drawings.Any(d => !d.IsText && d.Where.Width == 2 && d.Where.Height > 20);

    static string Say(float value) =>
        value.ToString("0.##", CultureInfo.InvariantCulture);

    static void Sequence(ReactorEditor editor)
    {
        editor.NewSystem("Airlock");
        editor.PressStart("When…");
        editor.PressBlock("Airlock Button");
        editor.PressChoice("is pressed");
        editor.PressPrimary();
    }
}
