using Xunit;
using Sprocket.Host.SpaceEngineers.Features.Shell;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

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
        screen.Editor.NewButtonSystem("Airlock");
        screen.Editor.Back();
        return screen.Run(2);
    }

    static StatusRow Row(Screen screen, int which) =>
        screen.Editor.Rows()[which];

    static void Duplicate(Screen screen, int which)
    {
        Row(screen, which).Copies!.Execute();
        screen.Run(2);
    }

    [Fact]
    public void DuplicatingFromTheListOpensTheCopy()
    {
        var screen = AtTheList();

        Duplicate(screen, 0);

        Assert.Equal("Sequence 2", screen.Ended);
        Assert.Equal(2, screen.Editor.Program.Sequences.Count);
    }

    /// <summary>
    /// A sequence is thrown away from its row as well as from its own screen.
    /// </summary>
    /// <remarks>
    /// The row has the switch that parks it and the button that copies it; the one thing it did not
    /// have was the way to be rid of it, which left the bin on the sequence's own screen as
    /// something to be found rather than seen. It asks first, wherever it is pressed from: a
    /// sequence is work, and this is the one act in the editor that cannot be walked back.
    /// </remarks>
    [Fact]
    public void ASequenceIsDeletedFromItsRow()
    {
        var screen = AtTheList();

        var row = Row(screen, 0);
        Assert.NotNull(row.Deletes);

        row.Deletes!.Execute();
        screen.Run(2);

        Assert.StartsWith("Delete this sequence?", screen.Editor.Question());

        // The safe answer first, and taking it leaves the sequence where it was.
        screen.Editor.PressChoice("Keep it");
        screen.Run(2);

        Assert.Single(screen.Editor.Program.Sequences);

        Row(screen, 0).Deletes!.Execute();
        screen.Editor.PressChoice("Delete it");
        screen.Run(2);

        Assert.Empty(screen.Editor.Program.Sequences);
    }

    /// <summary>
    /// While its system is on, a sequence's bin stays where it is, cannot be pressed, and says why.
    /// </summary>
    [Fact]
    public void ASequenceOfASwitchedOnSystemKeepsAQuietBin()
    {
        var screen = AtTheList();

        screen.Editor.Screen.Flip!.Execute();
        screen.Run(2);

        var row = Row(screen, 0);
        Assert.Null(row.Deletes);
        Assert.True(row.DeletePart.IsVisible, "the bin went away while the system is on");
        Assert.False(row.DeletePart.IsEnabled, "the bin can still be pressed while the system is on");
        Assert.Equal("Turn the system off to edit.", row.DeletePart.Tip?.Text);
    }

    /// <summary>
    /// The button is on the row, and only on a row there can be another of.
    /// </summary>
    /// <remarks>
    /// The way to add a sequence is a button of its own, not a row, so every row in the list is a
    /// sequence and offers a copy of itself.
    /// </remarks>
    [Fact]
    public void OnlyASequenceRowOffersACopy()
    {
        var screen = AtTheList();
        var rows = screen.Editor.Rows();

        Assert.NotNull(rows[0].Copies);
        Assert.All(rows, row => Assert.NotNull(row.Copies));
    }

    /// <summary>
    /// The copy is started by nothing, and switched off.
    /// </summary>
    /// <remarks>
    /// A toolbar slot holds one claim on Sprocket's action, so a copy carrying the trigger it came
    /// from would be a second claim on a slot that is already taken - and two live sequences on one
    /// button would cycle an airlock twice with nothing on screen saying why.
    /// </remarks>
    [Fact]
    public void ACopyIsStartedByNothingAndIsSwitchedOff()
    {
        var screen = AtTheList();

        Duplicate(screen, 0);

        var copy = screen.Editor.Sentence();
        Assert.False(screen.Editor.Screen.SwitchIsOn);
        Assert.True(screen.Shows("switched off"));

        var made = screen.Editor.Program.Sequences[1];
        Assert.Equal(copy.SequenceId, made.Id);
        Assert.Null(made.Trigger);
    }

    /// <summary>
    /// A sequence of a system switched on can be copied, and the copy starts on nothing.
    /// </summary>
    /// <remarks>
    /// Unlike everything on the sequence's own screen, which waits for the switch. Copying does not
    /// touch what is running, and the copy cannot run: it has nothing to start it.
    /// </remarks>
    [Fact]
    public void ARunningSequenceCanBeCopied()
    {
        var screen = AtTheList();
        screen.Editor.Screen.Flip!.Execute();
        screen.Run(2);

        Duplicate(screen, 0);

        Assert.True(screen.Editor.Program.Systems[0].Enabled);
        Assert.Equal(2, screen.Editor.Program.Sequences.Count);
        Assert.Null(screen.Editor.Program.Sequences[1].Trigger);
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
