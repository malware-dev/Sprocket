using Sprocket.Host.SpaceEngineers.Features.LogicSystems;
using Sprocket.Host.SpaceEngineers.Features.Sequences;
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
    /// <summary>A new system's sequence given a step, and back pressed on it, which asks whether to keep the step.</summary>
    const string LeavingASequence =
        "60 196 type Airlock enter press trigger press when 200 384 560 340 1394 72 press step press task "
        + "150 203 type Tug 200 360 press \"Terminal ›\" press \"Forward Direction\" 1316 201 1316 319 enter 25 62";

    /// <summary>Enter takes the answer the page named, which for keeping changes is keeping them.</summary>
    [Fact]
    public void EnterTakesTheAnswerThePageNamed()
    {
        var screen = Screen.Fresh(LeavingASequence.Split(' '));

        Assert.Equal(screen.Editor.Texts.PickKeepQuestion, screen.Editor.Question());
        Assert.True(screen.Scene.IsQuestioning, "the question is not the thing owning the screen");

        Assert.True(screen.Scene.TakeDefault(), "Enter took no answer");
        screen.Run(2);

        // The safe one, which keeps what was made. A key pressed without reading must not throw work away.
        Assert.False(screen.Scene.IsQuestioning, "the question is still up");
        Assert.IsType<LogicSystemPage>(screen.Editor.Current);
        Assert.NotEmpty(screen.Editor.Program.Systems[0].Sequences[0].Steps);
    }

    /// <summary>And dismissing it is a third outcome rather than the safe answer.</summary>
    /// <remarks>
    /// Somebody who backs out of the question has said they did not mean to leave at all, so the sequence is left exactly
    /// as it was and the page is not popped.
    /// </remarks>
    [Fact]
    public void AndDismissingItAnswersNothing()
    {
        var screen = Screen.Fresh(LeavingASequence.Split(' '));

        screen.Scene.Dismiss();
        screen.Run(2);

        Assert.False(screen.Scene.IsQuestioning, "the question is still up");
        Assert.IsType<SequencePage>(screen.Editor.Current);
        Assert.NotEmpty(screen.Editor.Program.Systems[0].Sequences[0].Steps);
    }
}
