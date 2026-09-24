using VRageMath;
using Sprocket.Host.SpaceEngineers.Look;
using Sprocket.Host.SpaceEngineers.Sequences;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// A sequence that is running says so with a ground, the way a broken one does.
/// </summary>
/// <remarks>
/// A light and the word "running" are both things somebody finds once they are already reading the
/// row. Scanning a list of twenty for the one that is going now is what the ground is for, and it is
/// the same green the step being run wears one screen in.
/// </remarks>
public class RunningTests
{
    /// <summary>A sequence long enough to still be going when the frame is drawn.</summary>
    static Screen WithOneRunning()
    {
        var screen = Screen.Fresh();
        screen.Editor.NewSystem("Airlock");
        screen.Editor.PressStart("When…");
        screen.Editor.PressBlock("Airlock Button");
        screen.Editor.PressChoice("is pressed");
        screen.Editor.PressPrimary();

        // Written into the model rather than through the screens: what this is about is the row, and
        // a wait is the shortest step that leaves a sequence running for longer than an instant.
        var sequence = screen.Editor.Program.Sequences[0];
        sequence.Steps.Add(new Wait(600));
        sequence.Enabled = true;

        screen.Editor.Back();
        screen.Run(2);

        screen.Editor.Runner.Call(sequence);
        return screen.Run(2);
    }

    static bool Washed(Screen screen, Color ground) =>
        screen.Drawings.Any(d => !d.IsText && d.Ink == ground);

    [Fact]
    public void ARunningSequenceIsWashed()
    {
        var screen = WithOneRunning();

        Assert.True(screen.Editor.Rows().Entries[0].Running);
        Assert.True(Washed(screen, Palette.LiveWash), "a running sequence's row has no ground");
    }

    /// <summary>
    /// And the row a screen further out, which is one of the system rather than a sequence.
    /// </summary>
    /// <remarks>
    /// The construct's own list shows systems with the ones of them underneath, and the one that is
    /// going is what somebody arriving at that screen is looking for. Asked of that one of the system
    /// and not of the system, because the port airlock cycling is not the front one cycling.
    /// </remarks>
    [Fact]
    public void AnInstanceThatIsRunningIsWashedToo()
    {
        var screen = WithOneRunning();
        screen.Editor.GoToRoot();
        screen.Run(2);

        var rows = screen.Editor.Rows().Entries;

        // The system's own row is a heading over the ones of it and is never marked - the same rule
        // the problem mark follows, for the same reason.
        Assert.False(rows[0].Running);
        Assert.True(rows[1].Running);
        Assert.True(Washed(screen, Palette.LiveWash), "a running airlock's row has no ground");
    }

    [Fact]
    public void AnIdleSequenceIsNotWashed()
    {
        var screen = Screen.Fresh();
        screen.Editor.NewSystem("Airlock");
        screen.Editor.PressStart("When…");
        screen.Editor.PressBlock("Airlock Button");
        screen.Editor.PressChoice("is pressed");
        screen.Editor.PressPrimary();
        screen.Editor.Back();
        screen.Run(2);

        Assert.False(screen.Editor.Rows().Entries[0].Running);
        Assert.False(Washed(screen, Palette.LiveWash), "an idle sequence's row is wearing the live ground");
    }
}
