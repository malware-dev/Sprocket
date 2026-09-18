using Reactor.Host.SpaceEngineers.Lists;
using Xunit;

namespace Reactor.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// A sequence nothing starts can be told what starts it, from its own screen.
/// </summary>
/// <remarks>
/// There was no way to. The first line said what was missing - an ellipsis where the trigger goes -
/// and nothing on the screen could fill it in, so a copied sequence, one sent in a hand-written
/// program, or one whose trigger block had been ground off was a dead end.
/// </remarks>
public class StartingTests
{
    /// <summary>A copy, which is the easiest way to arrive at a sequence with no trigger.</summary>
    static Screen AtASequenceNothingStarts()
    {
        var screen = Screen.Fresh();
        screen.Editor.NewSystem("Airlock");
        screen.Editor.PressStart("When…");
        screen.Editor.PressBlock("Airlock Button");
        screen.Editor.PressChoice("is pressed");
        screen.Editor.PressPrimary();
        screen.Editor.Back();
        screen.Run(2);

        var row = screen.Editor.Component<StatusListComponent>().Entries[0];
        screen.Editor.Invoke(row.DuplicateCommand);
        return screen.Run(2);
    }

    [Fact]
    public void TheFirstLineOffersToSayWhatStartsIt()
    {
        var screen = AtASequenceNothingStarts();

        Assert.NotEqual(0, screen.Editor.Sentence().StartCommand);
        Assert.True(screen.Shows("+ say what starts this"));
    }

    [Fact]
    public void ASequenceSomethingStartsDoesNotOfferIt()
    {
        var screen = AtASequenceNothingStarts();
        screen.Editor.Back();
        screen.Run(2);

        // The original, which has a trigger: its first line is the sentence, not the offer.
        var row = screen.Editor.Component<StatusListComponent>().Entries[0];
        screen.Editor.Invoke(row.Command);
        screen.Run(2);

        Assert.Equal(0, screen.Editor.Sentence().StartCommand);
        Assert.False(screen.Shows("+ say what starts this"));
    }

    [Fact]
    public void ChoosingAMomentStartsIt()
    {
        var screen = AtASequenceNothingStarts();

        screen.Editor.Invoke(screen.Editor.Sentence().StartCommand);
        screen.Editor.PressChoice("When something happens");
        screen.Editor.PressBlock("Airlock Button");
        screen.Editor.PressChoice("is pressed");
        screen.Editor.PressPrimary();
        screen.Run(2);

        Assert.Equal("When Airlock Button is pressed > Sequence 2", screen.Ended);
        Assert.Equal(0, screen.Editor.Sentence().StartCommand);
    }

    /// <summary>
    /// And a repeat, which is the other way a sequence can begin.
    /// </summary>
    /// <remarks>
    /// The same two questions the starters on a system's screen ask. A path that only offered the
    /// moment would leave a copied sequence unable to become a repeat at all.
    /// </remarks>
    [Fact]
    public void ChoosingARepeatStartsIt()
    {
        var screen = AtASequenceNothingStarts();

        screen.Editor.Invoke(screen.Editor.Sentence().StartCommand);
        screen.Editor.PressChoice("Every so often");
        screen.Editor.PressChoice("slow - every 300 ticks");
        screen.Run(2);

        Assert.Equal("Every 300 ticks > Sequence 2", screen.Ended);
        Assert.Equal(0, screen.Editor.Sentence().StartCommand);
    }

    /// <summary>
    /// It is an edit, so it walks back.
    /// </summary>
    /// <remarks>
    /// Unlike making a sequence, which has no history to record into - this one is a change to a
    /// sequence that already exists, and arming something by accident is exactly what undo is for.
    /// </remarks>
    [Fact]
    public void ItCanBeUndone()
    {
        var screen = AtASequenceNothingStarts();

        screen.Editor.Invoke(screen.Editor.Sentence().StartCommand);
        screen.Editor.PressChoice("Every so often");
        screen.Editor.PressChoice("slow - every 300 ticks");
        screen.Run(2);

        screen.Editor.Invoke(screen.Editor.Sentence().UndoCommand);
        screen.Run(2);

        Assert.NotEqual(0, screen.Editor.Sentence().StartCommand);
        Assert.True(screen.Shows("+ say what starts this"));
    }
}
