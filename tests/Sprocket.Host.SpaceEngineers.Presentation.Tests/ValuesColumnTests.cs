using Sprocket.Host.SpaceEngineers.Sequences;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// What a step does is given its values in the picker's third column, and finished there.
/// </summary>
/// <remarks>
/// The column is part of the screen rather than a question raised over it: its fields take the
/// keyboard when they are pressed, Done sits at its foot, and what was entered for one thing is
/// there again on going back to it while the picker is open.
/// </remarks>
public class ValuesColumnTests
{
    /// <summary>A sequence with a light picked in a step's picker, and nothing chosen for it yet.</summary>
    const string ALight = "60 130 type Airlock enter 60 161 200 375 560 357 1353 311 70 193 200 476";

    const string SetRadius = "800 676";
    const string SetColour = "800 584";
    const string RadiusBox = "1315 206";
    const string Done = "1365 260";

    static Screen Open(string then)
    {
        return Screen.Fresh((ALight + " " + then).Split(' '));
    }

    /// <summary>The column comes once something to do is chosen, and not before.</summary>
    [Fact]
    public void TheColumnWaitsForSomethingToDo()
    {
        var screen = Screen.Fresh(ALight.Split(' '));

        Assert.False(screen.Shows("VALUES"), "the values column came before anything was chosen");
        Assert.True(screen.Shows("WHAT TO DO"), "the second column is not there");
    }

    [Fact]
    public void ChosenThingsAskForTheirValuesInTheColumn()
    {
        var screen = Open(SetRadius);

        Assert.True(screen.Shows("VALUES"), "there is no values column");
        Assert.True(screen.Shows("Radius (0.1 - 20)"), "the column asks for nothing");
        Assert.False(screen.Shows("This needs"), "the column says what its fields already show");
        Assert.False(screen.Scene.IsAsking, "a dialog was raised over the picker");
        Assert.False(screen.Scene.IsFilling, "a field took the keyboard before it was pressed");
    }

    [Fact]
    public void AValueTypedAndDoneMakesTheStep()
    {
        var screen = Open(SetRadius + " " + RadiusBox + " type 5 " + Done);

        Assert.Equal("Sequence 1", screen.Ended);
        var act = Assert.IsType<Act>(screen.Editor.Program.Sequences[0].Steps[0]);
        Assert.Equal(5L, Assert.Single(act.Values).Whole);
    }

    /// <summary>Done with nothing given says so at the foot of the column, and the caret goes to the field.</summary>
    [Fact]
    public void DoneWithNothingGivenSaysWhat()
    {
        var screen = Open(SetRadius + " " + Done);

        Assert.Equal("Do something", screen.Ended);
        Assert.True(screen.Shows("Radius needs a value."), "nothing said why it did not finish");
        Assert.True(screen.Scene.IsFilling, "the caret did not go to the field");
    }

    /// <summary>A value the field will not take is refused by Done, with the reason.</summary>
    [Fact]
    public void DoneWithAWrongValueSaysWhy()
    {
        var screen = Open(SetRadius + " " + RadiusBox + " type 50 " + Done);

        Assert.Equal("Do something", screen.Ended);
        Assert.True(screen.Shows("Radius has to be between 0.1 and 20."), "nothing said why");
    }

    /// <summary>Nothing is held against what is typed until Done has been tried.</summary>
    [Fact]
    public void TypingIsNotJudgedBeforeDone()
    {
        var screen = Open(SetRadius + " " + RadiusBox + " type 50");

        Assert.False(screen.Shows("has to be"), "the value was refused before Done was pressed");
    }

    /// <summary>What was entered for one thing is there again on going back to it.</summary>
    [Fact]
    public void GoingBackToAThingKeepsWhatWasEntered()
    {
        var screen = Open(SetRadius + " " + RadiusBox + " type 7 " + SetColour + " " + SetRadius);

        Assert.True(screen.Shows("7.0"), "the radius typed earlier was lost");
    }

    /// <summary>Double pressing something that takes nothing finishes, as it always has.</summary>
    [Fact]
    public void ADoublePressOnSomethingThatTakesNothingFinishes()
    {
        var screen = Open("double 800 457");

        Assert.Equal("Sequence 1", screen.Ended);
        var act = Assert.IsType<Act>(screen.Editor.Program.Sequences[0].Steps[0]);
        Assert.Empty(act.Values);
    }

    /// <summary>Double pressing something that takes values takes the caret to its first field.</summary>
    [Fact]
    public void ADoublePressGoesToTheFirstField()
    {
        var screen = Open("double " + SetRadius);

        Assert.Equal("Do something", screen.Ended);
        Assert.True(screen.Scene.IsFilling, "the caret did not go to the field");
    }

    /// <summary>
    /// Pressing a step's values opens its picker with the caret in them, where they were first given.
    /// </summary>
    [Fact]
    public void PressingAStepsValuesOpensItsPickerOnThem()
    {
        var screen = Open(SetRadius + " " + RadiusBox + " type 5 " + Done + " 447 185");

        Assert.Equal("Do something", screen.Ended);
        Assert.False(screen.Scene.IsAsking, "a dialog was raised");
        Assert.True(screen.Scene.IsFilling, "the caret is not in the values");
        Assert.Equal("5.0", screen.Scene.Typing.Text);
    }

    /// <summary>
    /// Leaving asks about any change: a value, or what is done, as well as the blocks.
    /// </summary>
    [Theory]
    [InlineData("447 185 type 7 escape")]
    [InlineData("270 175 800 584")]
    public void LeavingAsksAboutAnyChange(string change)
    {
        var screen = Open(SetRadius + " " + RadiusBox + " type 5 enter " + change + " 19 62");

        Assert.True(screen.Scene.IsQuestioning, "leaving asked nothing");
        Assert.True(screen.Shows("Keep your changes?"), "the question is not about changes");
    }

    /// <summary>And nothing about a step only looked at, although its values show differently in their field.</summary>
    [Fact]
    public void LeavingAsksNothingWhereNothingChanged()
    {
        var screen = Open(SetRadius + " " + RadiusBox + " type 5 enter 447 185 escape 19 62");

        Assert.False(screen.Scene.IsQuestioning, "leaving asked about 5 shown as 5.0");
        Assert.Equal("Sequence 1", screen.Ended);
    }

    /// <summary>Escape puts a field down and leaves what is in it.</summary>
    [Fact]
    public void EscapeLeavesTheField()
    {
        var screen = Open(SetRadius + " " + RadiusBox + " type 7 escape");

        Assert.False(screen.Scene.IsFilling, "the field kept the keyboard");
        Assert.Equal("Do something", screen.Ended);
        Assert.True(screen.Shows("7"), "what was typed was thrown away");
    }
}
