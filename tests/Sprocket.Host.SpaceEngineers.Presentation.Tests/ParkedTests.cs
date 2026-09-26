using VRageMath;
using Sprocket.Host.SpaceEngineers.Look;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// A sequence somebody switched off says so with a ground, and with nothing else.
/// </summary>
/// <remarks>
/// It is the commonest reason a button does nothing, and everything else about such a sequence
/// reads as working: the trigger is right, the blocks are there, and the one column that said
/// otherwise said it in the place every row says something. Found by reading the screen rather
/// than by being shown, which is the failure the marks exist to prevent.
/// <para>
/// The words go in the line the row already has for what it is doing, because being switched off is
/// what it is doing. A line of its own would come and go as the switch beside it is pressed, moving
/// the row out from under the press that moved it.
/// </para>
/// </remarks>
public class ParkedTests
{
    static Screen WithOneParked()
    {
        var screen = Screen.Fresh();
        screen.Editor.NewButtonSystem("Airlock");
        screen.Editor.PressAdd("do something");
        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Editor.PressChoice("Close");
        screen.Editor.PressPrimary();

        screen.Editor.Back();
        screen.Editor.PressAnswer("Leave it off");
        return screen.Run(120);
    }

    static bool Washed(Screen screen, Color ground) =>
        screen.Drawings.Any(d => !d.IsText && d.Ink == ground);

    [Fact]
    public void AParkedSequenceIsWashed()
    {
        var screen = WithOneParked();

        Assert.True(screen.Editor.Rows()[0].Parked);
        Assert.True(Washed(screen, Palette.NoticeWash), "a parked sequence's row has no ground");
    }

    /// <summary>
    /// The words take the place of the line the row already had, rather than adding one.
    /// </summary>
    /// <remarks>
    /// "Switched off" beside a switch that says off is a word spent on nothing, so what follows
    /// from it is shown there instead. The row is the same height either way, so nothing moves when
    /// the switch is pressed - and a control that moves as it is used is the one thing a screen must
    /// never do.
    /// <para>
    /// The two are separate readings and only one of them is ever shown, which is what lets the
    /// warning be drawn as a warning rather than in the colour of an ordinary state.
    /// </para>
    /// </remarks>
    [Fact]
    public void TheWordsReplaceWhatTheRowSaidThere()
    {
        var screen = WithOneParked();

        Assert.Equal("This is switched off, so nothing starts it on its own.",
            screen.Editor.Rows()[0].Problem);
        Assert.Equal("switched off", screen.Editor.Rows()[0].Status);

        Assert.True(screen.Shows("This is switched off, so nothing starts it on its own."),
            "the row does not say what being switched off costs");
        Assert.False(screen.Shows("switched off"), "the row says it twice");
    }

    /// <summary>
    /// A remark, not a fault: it is not counted and the row is not washed as broken.
    /// </summary>
    /// <remarks>
    /// Switching a sequence off is a thing somebody did on purpose, so a red row sending them to
    /// hunt for a missing block would be the screen lying about what it knows.
    /// </remarks>
    [Fact]
    public void BeingParkedIsNotAFault()
    {
        var screen = WithOneParked();

        Assert.Equal(0, screen.Editor.Rows()[0].Problems);
        Assert.False(Washed(screen, Palette.ProblemWash), "a parked sequence's row reads as broken");
    }

    /// <summary>Switching it on takes the ground and the words away.</summary>
    [Fact]
    public void SwitchingItOnClearsIt()
    {
        var screen = WithOneParked();

        screen.Editor.Program.Sequences[0].Enabled = true;
        screen.Run(120);

        Assert.False(screen.Editor.Rows()[0].Parked);
        Assert.Equal("ready", screen.Editor.Rows()[0].Status);
        Assert.False(Washed(screen, Palette.NoticeWash), "a live sequence's row is wearing the parked ground");
    }

    /// <summary>
    /// One nobody has written yet is not one to switch on.
    /// </summary>
    /// <remarks>
    /// The same rule the trigger's own warning follows: somebody half way through building is not
    /// told they have got it wrong, because they have not finished getting it right.
    /// </remarks>
    [Fact]
    public void ASequenceWithNoStepsIsNotRemarkedOn()
    {
        var screen = Screen.Fresh();
        screen.Editor.NewButtonSystem("Airlock");
        screen.Editor.Back();
        screen.Run(120);

        Assert.False(screen.Editor.Rows()[0].Parked);
        Assert.Equal("switched off", screen.Editor.Rows()[0].Status);
        Assert.False(Washed(screen, Palette.NoticeWash), "an unwritten sequence is being remarked on");
    }
}
