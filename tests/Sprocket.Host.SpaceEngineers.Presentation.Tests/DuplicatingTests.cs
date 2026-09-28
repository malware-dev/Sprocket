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

        Assert.Equal("Delete this sequence?", screen.Editor.Screen.Title());

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
    /// Running or not, which is unlike everything that rewires one.
    /// </summary>
    /// <remarks>
    /// Throwing a sequence away is not editing it: nothing half-written can result, the question
    /// asks first, and a machine somebody wants rid of while it is running is exactly when they
    /// want rid of it. Parking it first was an errand on the way to the same place.
    /// </remarks>
    [Fact]
    public void ARunningSequenceIsStillDeletedFromItsRow()
    {
        var screen = AtTheList();

        var row = Row(screen, 0);
        row.Flips!.Execute();
        screen.Run(2);

        Row(screen, 0).Deletes!.Execute();
        screen.Editor.PressChoice("Delete it");
        screen.Run(2);

        Assert.Empty(screen.Editor.Program.Sequences);
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
        Assert.All(rows, row => Assert.False(row.IsAction));
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
        Row(screen, 0).Flips!.Execute();
        screen.Run(2);

        Duplicate(screen, 0);

        Assert.False(screen.Editor.Screen.SwitchIsOn);
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
