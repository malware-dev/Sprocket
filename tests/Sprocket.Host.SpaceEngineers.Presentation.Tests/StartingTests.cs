using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// A sequence nothing starts: how one is made, what it says, and where it is mended.
/// </summary>
/// <remarks>
/// Ordinary rather than broken. Somebody who knows what the machine should do writes it first and
/// gives it a trigger afterwards, and a copy arrives the same way. What it is not is something the
/// sequence's own screen can mend: what starts a sequence is a row on its system's screen, and a
/// sequence nothing starts has no such row - so its screen says the reason and the way to it is one
/// press back.
/// </remarks>
public class StartingTests
{
    /// <summary>A sequence made on its own, with a step, so it is work rather than a start.</summary>
    static Screen WithOne()
    {
        var screen = Screen.Fresh();
        screen.Editor.NewSystem("Airlock");
        screen.Run(2);

        screen.Editor.PressStart("sequence");
        screen.Editor.PressAdd("task");
        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Editor.PressChoice("Close");
        screen.Editor.PressPrimary();

        return screen.Run(120);
    }

    [Fact]
    public void ASequenceCanBeMadeOnItsOwn()
    {
        var screen = WithOne();

        Assert.Single(screen.Editor.Program.Sequences);
        Assert.Equal("Sequence 1", screen.Ended);
        Assert.False(screen.Editor.Program.Systems[0].Enabled);
        Assert.Null(screen.Editor.Program.Sequences[0].Trigger);
    }

    /// <summary>
    /// It says so, and says it quietly.
    /// </summary>
    /// <remarks>
    /// A warning rather than a fault: nothing is wrong with the sequence, and somebody halfway
    /// through building is not being told off. It is the reason it never runs, said on the screen
    /// they are standing on when they wonder why.
    /// </remarks>
    [Fact]
    public void ItSaysNothingStartsIt()
    {
        var screen = WithOne();

        var sentence = screen.Editor.Sentence();
        Assert.Equal("Nothing starts this yet.", sentence.Bar.Problem);
        Assert.True(sentence.Bar.ProblemIsWarning);

        // And nothing on this screen offers to mend it, because what starts a sequence is not here.
        Assert.False(screen.Shows("+ say what starts this"));
    }

    /// <summary>
    /// The mend is a trigger on the system, pointed at it by name.
    /// </summary>
    /// <remarks>
    /// The same presses as making any other trigger, which is the point of there being one way to
    /// make one. A name is what lets a trigger be pointed at a sequence, and a sequence made this
    /// way has one from birth.
    /// </remarks>
    [Fact]
    public void ATriggerOnTheSystemIsWhereItIsMended()
    {
        var screen = WithOne();

        // Leaving a sequence for its system asks nothing, since that is not leaving the system.
        screen.Editor.Back();
        screen.Run(120);

        // The row for it says the same thing, one screen out.
        Assert.Equal("Nothing starts this yet.", screen.Editor.Rows()[0].Problem);

        screen.Editor.PressStart("when");
        screen.Editor.PressBlock("Airlock Button");
        screen.Editor.PressChoice("button 1 is pressed");
        screen.Editor.PressChoice("Sequence 1");
        screen.Editor.PressPrimary();
        screen.Run(120);

        Assert.NotNull(screen.Editor.Program.Sequences[0].Trigger);
        Assert.Equal("then run Sequence 1", screen.Editor.Doing(0));

        // Mended.
        Assert.NotEqual("Nothing starts this yet.", screen.Editor.Rows()[0].Problem);
    }
}
