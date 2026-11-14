using Sprocket.Host.SpaceEngineers.Features.Selector;
using System.Globalization;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// The strip at the head of a picker row, around its box and down its whole height, is the box's: a press there takes the
/// row as well as what is taken, and never takes it alone in place of the others.
/// </summary>
public class PickBoxTests
{
    static Screen Picking()
    {
        var screen = Screen.Fresh();
        screen.Editor.NewButtonSystem("Airlock");
        screen.Run(2);
        screen.Editor.PressAdd("task");
        return screen.Run(2);
    }

    static PickRow Row(Screen screen, string name) => screen.Editor.Pick().Stages[0].Entries().Single(e => e.Text == name);

    /// <summary>Pressing beside a row's box, in its corner well clear of the box, ticks it and leaves the others ticked.</summary>
    [Fact]
    public void APressBesideTheBoxTakesTheRowAsWell()
    {
        var screen = Picking();
        Row(screen, "Airlock Inner Door").Ticks!.Execute();
        screen.Run(2);

        var outer = Row(screen, "Airlock Outer Door");
        var x = outer.Bounds.X + 4f;
        var y = outer.Bounds.Y + outer.Bounds.Height - 4f;
        screen.Do(x.ToString(CultureInfo.InvariantCulture), y.ToString(CultureInfo.InvariantCulture));
        screen.Run(2);

        Assert.True(Row(screen, "Airlock Outer Door").IsChosen, "the press beside the box did not take the row");
        Assert.True(Row(screen, "Airlock Inner Door").IsChosen, "the press beside the box took the row alone");
    }

    /// <summary>Pressing a row's name still takes it alone.</summary>
    [Fact]
    public void APressOnTheNameTakesTheRowAlone()
    {
        var screen = Picking();
        Row(screen, "Airlock Inner Door").Ticks!.Execute();
        screen.Run(2);

        var outer = Row(screen, "Airlock Outer Door");
        var x = outer.Bounds.X + outer.Bounds.Width * 0.6f;
        var y = outer.Bounds.Y + outer.Bounds.Height * 0.5f;
        screen.Do(x.ToString(CultureInfo.InvariantCulture), y.ToString(CultureInfo.InvariantCulture));
        screen.Run(2);

        Assert.True(Row(screen, "Airlock Outer Door").IsChosen);
        Assert.False(Row(screen, "Airlock Inner Door").IsChosen, "a press on the name took the row as well");
    }
}
