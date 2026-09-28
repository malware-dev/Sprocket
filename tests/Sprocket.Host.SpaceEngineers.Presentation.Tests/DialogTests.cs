using System.Globalization;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// A question with answers asked on the root, as a dialog: pressed through, taken with Enter, put away with Escape,
/// and deaf to presses anywhere else.
/// </summary>
/// <remarks>The question comes from a block holding a program this build cannot read, whose first edit asks before replacing it.</remarks>
public class DialogTests
{
    const string Older = "sprocket 1 alpha 6\n"
        + "block|entity1|MyObjectBuilder_ButtonPanel/ButtonPanelLarge|i:87472652339029116,t:Button Panel\n"
        + "system|1|on|Test|\n"
        + "one|1|\n"
        + "sequence|1|on||\n"
        + "when|any|pressed_1|entity1\n";

    /// <summary>The first edit on the root raises the question in a dialog, with both answers in it.</summary>
    [Fact]
    public void TheQuestionIsAskedInADialog()
    {
        var screen = Asked();

        Assert.True(screen.Scene.IsQuestioning, "no question is up");
        Assert.True(screen.Shows("Leave it alone"), "the safe answer is missing");
        Assert.True(screen.Shows("Replace it"), "the other answer is missing");
    }

    /// <summary>Pressing an answer gives it.</summary>
    [Fact]
    public void PressingAnAnswerGivesIt()
    {
        var screen = Asked();

        Press(screen, "Replace it");
        screen.Run(5);

        Assert.False(screen.Scene.IsQuestioning, "the dialog is still up");
        Assert.Single(screen.Editor.Program.Systems);
    }

    /// <summary>Enter gives the answer the page named, which here is the safe one.</summary>
    [Fact]
    public void EnterGivesTheSafeAnswer()
    {
        var screen = Asked();

        screen.Do("enter");
        screen.Run(5);

        Assert.False(screen.Scene.IsQuestioning, "the dialog is still up");
        Assert.Empty(screen.Editor.Program.Systems);
    }

    /// <summary>Escape puts the dialog away and changes nothing.</summary>
    [Fact]
    public void EscapePutsItAway()
    {
        var screen = Asked();

        screen.Do("escape");
        screen.Run(5);

        Assert.False(screen.Scene.IsQuestioning, "the dialog is still up");
        Assert.Empty(screen.Editor.Program.Systems);
    }

    /// <summary>A press anywhere but the dialog does nothing, the dialog stays, and nothing behind it answers.</summary>
    [Fact]
    public void APressElsewhereDoesNothing()
    {
        var screen = Asked();

        screen.Do("130", "187", "100", "900");
        screen.Run(5);

        Assert.True(screen.Scene.IsQuestioning, "the dialog went away");
        Assert.Empty(screen.Editor.Program.Systems);
    }

    /// <summary>The root of a block holding the older program, with New logic system pressed.</summary>
    static Screen Asked()
    {
        var screen = Screen.Holding(Older, "tick", "30");
        Press(screen, "+ Logic system");
        screen.Run(5);
        return screen;
    }

    static void Press(Screen screen, string words)
    {
        var drawn = screen.Text(words);
        var at = Screen.Ui(drawn.At.X + 8f, screen.MiddleOf(drawn));
        screen.Do(Say(at.X), Say(at.Y));
    }

    static string Say(float value) =>
        value.ToString("0.##", CultureInfo.InvariantCulture);
}
