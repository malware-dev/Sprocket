using System.Globalization;
using Lumel.Controls;
using Lumel.Input;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>Where a question raised by a press stands, away from the root.</summary>
public class QuestionTests
{
    /// <summary>A question a control's own command asks about that control hangs under it, where it was pressed.</summary>
    [Fact]
    public void AQuestionAskedByAControlHangsUnderIt()
    {
        var screen = Screen.Fresh("60", "130", "type", "Airlock", "enter", "tick", "2");
        var button = screen.Editor.Current.View!.Within().OfType<Button>().Last();
        button.Command = new Relay(delegate
        {
            screen.Editor.AskHere("Which one?");
            screen.Editor.Answer("This one", delegate { });
        });

        var bounds = button.Bounds;
        screen.Do(Say(bounds.X + bounds.Width / 2f), Say(bounds.Y + bounds.Height / 2f));
        screen.Run(2);

        var below = bounds.Y + bounds.Height;
        Assert.InRange(screen.Text("Which one?").At.Y, below, below + 200f);
    }

    static string Say(float value) =>
        value.ToString("0.##", CultureInfo.InvariantCulture);
}
