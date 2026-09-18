using Reactor.Host.SpaceEngineers.Lists;
using Xunit;

namespace Reactor.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// Duplicating a sequence is done from the list and lands on the copy.
/// </summary>
/// <remarks>
/// The model's side of this is in the core tests. What is worth holding here is the screen: that the
/// button is on the row rather than inside the sequence, that the press opens the copy rather than
/// leaving somebody where they were, and that the copy says what it is waiting for.
/// </remarks>
public class DuplicatingTests
{
    /// <summary>The system's own screen, listing the one sequence somebody has built.</summary>
    static Screen AtTheList()
    {
        var screen = Screen.Fresh();
        screen.Editor.NewSystem("Airlock");
        screen.Editor.PressStart("When…");
        screen.Editor.PressBlock("Airlock Button");
        screen.Editor.PressChoice("is pressed");
        screen.Editor.PressPrimary();
        screen.Editor.Back();
        return screen.Run(2);
    }

    static StatusEntry Row(Screen screen, int which) =>
        screen.Editor.Component<StatusListComponent>().Entries[which];

    static void Duplicate(Screen screen, int which)
    {
        screen.Editor.Invoke(Row(screen, which).DuplicateCommand);
        screen.Run(2);
    }

    [Fact]
    public void DuplicatingFromTheListOpensTheCopy()
    {
        var screen = AtTheList();

        Duplicate(screen, 0);

        Assert.Equal("… > Sequence 2", screen.Ended);
        Assert.Equal(2, screen.Editor.Program.Sequences.Count);
    }

    /// <summary>
    /// The button is on the row, and only on a row there can be another of.
    /// </summary>
    /// <remarks>
    /// The row that adds a sequence is a button, not a thing, so duplicating it means nothing. A
    /// command there would draw a control that does something unsayable.
    /// </remarks>
    [Fact]
    public void OnlyASequenceRowOffersACopy()
    {
        var screen = AtTheList();
        var rows = screen.Editor.Component<StatusListComponent>().Entries;

        Assert.NotEqual(0, rows[0].DuplicateCommand);
        for (var i = 1; i < rows.Count; i++)
            Assert.Equal(0, rows[i].DuplicateCommand);
    }

    /// <summary>
    /// The copy is waiting to be told what starts it, and says so where the trigger goes.
    /// </summary>
    /// <remarks>
    /// The ellipsis is the same one an unfilled slot uses anywhere else, so nothing here is a word
    /// only this screen knows.
    /// </remarks>
    [Fact]
    public void ACopyIsStartedByNothingAndIsSwitchedOff()
    {
        var screen = AtTheList();

        Duplicate(screen, 0);

        var copy = screen.Editor.Sentence();
        Assert.False(copy.Enabled);
        // Asked for rather than read back through the clip: the heading's first part is clipped to
        // its own box, and the ellipsis is drawn a fraction of a pixel left of it.
        Assert.Contains("…", screen.Words);
        Assert.True(screen.Shows("switched off"));
    }

    /// <summary>
    /// A running sequence can be copied, and the copy is still switched off.
    /// </summary>
    /// <remarks>
    /// Unlike everything on the sequence's own screen, which waits for the switch. Copying does not
    /// touch what is running, and the copy cannot run: it has nothing to start it and its own switch
    /// is off.
    /// </remarks>
    [Fact]
    public void ARunningSequenceCanBeCopied()
    {
        var screen = AtTheList();
        screen.Editor.SetToggle(Row(screen, 0).SwitchCommand, true);
        screen.Run(2);

        Duplicate(screen, 0);

        Assert.False(screen.Editor.Sentence().Enabled);
        Assert.True(screen.Editor.Program.Sequences[0].Enabled);
    }

    /// <summary>The copy is a sequence of the same system, so the system's list shows both.</summary>
    [Fact]
    public void BothSequencesAreInTheSameSystem()
    {
        var screen = AtTheList();

        Duplicate(screen, 0);

        var system = screen.Editor.Program.Systems[0];
        Assert.Equal(2, system.Sequences.Count);
        Assert.Single(screen.Editor.Program.Systems);
    }
}
