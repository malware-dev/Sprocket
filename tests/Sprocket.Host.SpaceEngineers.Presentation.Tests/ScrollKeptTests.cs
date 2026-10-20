using VRageMath;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A screen keeps how far down it was scrolled while a screen opened from it is open.</summary>
public class ScrollKeptTests
{
    /// <summary>Scrolled down the home screen, opening a logic system and coming back finds the home screen where it was left: its third logic system where it stood.</summary>
    [Fact]
    public void ComingBackFindsTheScreenWhereItWasLeft()
    {
        var screen = Screen.Fresh();
        for (var i = 1; i <= 8; i++)
        {
            screen.Editor.NewSystem($"Airlock {i}");
            screen.Run(2);
            screen.Editor.GoToRoot();
            screen.Run(2);
        }

        var before = screen.Scene.Find("Airlock 3");
        screen.Scene.ScrollBy(new Vector2(800f, 600f), 400f);
        screen.Run(2);
        var scrolled = screen.Scene.Find("Airlock 3");
        Assert.True(scrolled.Y < before.Y - 100f, "the home screen did not scroll");

        screen.Editor.RootHeadings()[2].Opens!.Execute();
        screen.Run(2);
        screen.Editor.Back();
        screen.Run(2);

        Assert.Equal(scrolled.Y, screen.Scene.Find("Airlock 3").Y, 1);
    }
}
