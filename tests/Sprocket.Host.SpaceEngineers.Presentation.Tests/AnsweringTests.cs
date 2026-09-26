using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// A question with answers can be answered with the keyboard, and dismissed without answering.
/// </summary>
/// <remarks>
/// Which answer Enter takes is the page's to say. A key that guessed would be worse than one that
/// did nothing, because the guess is only noticed after it has been acted on.
/// </remarks>
public class AnsweringTests
{
    const string LeavingASequence =
        "60 133 type Airlock enter 60 164 200 384 560 340 1353 314 70 200 "
        + "150 203 type Tug 200 360 800 587 1316 201 1316 319 enter 25 62";

    [Fact]
    public void EnterTakesTheAnswerThePageNamed()
    {
        var screen = Screen.Fresh(LeavingASequence.Split(' '));

        Assert.True(screen.Shows("Switch this sequence on?"), "leaving did not ask");
        Assert.True(screen.Scene.IsQuestioning, "the question is not the thing owning the screen");

        Assert.True(screen.Scene.TakeDefault(), "Enter took no answer");
        screen.Run(2);

        // The safe one, which is the one that leaves it off. A key pressed without reading must not
        // switch something on.
        Assert.False(screen.Scene.IsQuestioning, "the question is still up");
        Assert.False(screen.Editor.Program.Sequences[0].Enabled, "Enter switched it on");
    }

    /// <summary>And dismissing it is a third outcome rather than the safe answer.</summary>
    /// <remarks>
    /// Somebody who backs out of "switch this on?" has said they did not mean to leave at all, so
    /// the sequence is left exactly as it was and the page is not popped.
    /// </remarks>
    [Fact]
    public void AndDismissingItAnswersNothing()
    {
        var screen = Screen.Fresh(LeavingASequence.Split(' '));

        screen.Scene.Dismiss();
        screen.Run(2);

        Assert.False(screen.Scene.IsQuestioning, "the question is still up");
        Assert.False(screen.Editor.Program.Sequences[0].Enabled, "dismissing switched it on");
        Assert.True(screen.Shows("Switch this sequence on?") == false, "the question still shows");
    }
}
