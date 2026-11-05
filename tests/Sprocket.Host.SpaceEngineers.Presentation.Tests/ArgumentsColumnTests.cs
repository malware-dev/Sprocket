using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
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
public class ArgumentsColumnTests
{
    const string ALight = "60 196 type Airlock enter press trigger press when 200 384 560 340 1394 72 press step press task 200 485";

    const string SetRadius = "press radius";
    const string SetColor = "press color";
    const string RadiusBox = "1348 232";
    const string Done = "1394 72";

    // The values part of the step's line, after the slot it names.
    const string Values = "420 192";

    // The slot the step makes, after the button trigger's, which the picker's heading names.
    const string Slot = "slot 2";

    /// <summary>Presses twice on the words <paramref name="text" /> where they are drawn.</summary>
    static void DoubleOn(Screen screen, string text)
    {
        var drawn = screen.Drawings.First(d => d.IsText && d.Text == text);
        var at = Screen.Ui(drawn.At.X + 4f, drawn.At.Y + 8f);
        screen.Do("double", at.X.ToString(System.Globalization.CultureInfo.InvariantCulture),
            at.Y.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    static Screen Open(string then)
    {
        return Screen.Fresh((ALight + " " + then).Split(' '));
    }

    /// <summary>The column comes once something to do is chosen, and not before.</summary>
    [Fact]
    public void TheColumnWaitsForSomethingToDo()
    {
        var screen = Screen.Fresh(ALight.Split(' '));

        Assert.False(screen.Shows("RADIUS"), "the options column came before anything was chosen");
        Assert.True(screen.Shows("WHAT TO DO"), "the second column is not there");
    }

    [Fact]
    public void ChosenThingsAskForTheirValuesInTheColumn()
    {
        var screen = Open(SetRadius);

        Assert.True(screen.Shows("RADIUS"), "the column is not headed by what was chosen");
        Assert.True(screen.Shows("radius"), "the column asks for nothing");
        Assert.False(screen.Shows("This needs"), "the column says what its fields already show");
        Assert.False(screen.Scene.IsQuestioning, "a dialog was raised over the picker");
        Assert.False(screen.Scene.IsFilling, "a field took the keyboard before it was pressed");
    }

    [Fact]
    public void AValueTypedAndDoneMakesTheStep()
    {
        var screen = Open(SetRadius + " " + RadiusBox + " type 5 " + Done);

        Assert.Equal("Sequence 1", screen.Ended);
        var act = Assert.IsType<Act>(screen.Editor.Program.Sequences[0].Steps[0]);
        Assert.Equal(5L, Assert.Single(act.Arguments).Whole);
    }

    /// <summary>Done with nothing given says so at the foot of the column, and the caret goes to the field.</summary>
    [Fact]
    public void DoneWithNothingGivenSaysWhat()
    {
        var screen = Open(SetRadius + " " + RadiusBox + " clear " + Done);

        Assert.Equal(Slot, screen.Ended);
        Assert.True(screen.Shows("Radius is missing."), "nothing said why it did not finish");
        Assert.True(screen.Scene.IsFilling, "the caret did not go to the field");
    }

    /// <summary>A value the field will not take is refused by Done, with the reason.</summary>
    [Fact]
    public void DoneWithAWrongValueSaysWhy()
    {
        var screen = Open(SetRadius + " " + RadiusBox + " type 50 " + Done);

        Assert.Equal(Slot, screen.Ended);
        Assert.True(screen.Shows("radius must be between 0.1 and 20."), "nothing said why");
    }

    /// <summary>Nothing is held against what is typed until Done has been tried.</summary>
    [Fact]
    public void TypingIsNotJudgedBeforeDone()
    {
        var screen = Open(SetRadius + " " + RadiusBox + " type 50");

        Assert.False(screen.Shows("must be"), "the value was refused before Done was pressed");
    }

    /// <summary>What was entered for one thing is there again on going back to it.</summary>
    [Fact]
    public void GoingBackToAThingKeepsWhatWasEntered()
    {
        var screen = Open(SetRadius + " " + RadiusBox + " type 7 " + SetColor + " " + SetRadius);

        Assert.True(screen.Shows("7.0"), "the radius typed earlier was lost");
    }

    /// <summary>Double pressing something that takes nothing finishes, as it always has.</summary>
    [Fact]
    public void ADoublePressOnSomethingThatTakesNothingFinishes()
    {
        var screen = Screen.Fresh(("60 196 type Airlock enter press trigger press when 200 384 560 340 1394 72 press step "
            + "press task press \"Airlock Inner Door\"").Split(' '));
        DoubleOn(screen, "reverse");

        Assert.Equal("Sequence 1", screen.Ended);
        var act = Assert.IsType<Act>(screen.Editor.Program.Sequences[0].Steps[0]);
        Assert.Empty(act.Arguments);
    }

    /// <summary>Double pressing something that takes values takes the caret to its first field.</summary>
    [Fact]
    public void ADoublePressGoesToTheFirstField()
    {
        var screen = Screen.Fresh(ALight.Split(' '));
        DoubleOn(screen, "radius");

        Assert.Equal(Slot, screen.Ended);
        Assert.True(screen.Scene.IsFilling, "the caret did not go to the field");
    }

    /// <summary>A double press on one of a line of words finishes as Done does, with that word.</summary>
    [Fact]
    public void ADoublePressOnAWordFinishesWithIt()
    {
        var screen = Screen.Fresh(("60 196 type Airlock enter press trigger press when 200 384 560 340 1394 72 press step "
            + "press task").Split(' '));
        var door = screen.Editor.World.Entities.First(entity => entity.Name == "Airlock Inner Door");
        var speed = 0;
        door.Kind.WithValue("speed", "speed",
                OwnValue.OneOf<Sandbox.ModAPI.IMyTerminalBlock>(new[] { "low", "middle", "high" }, _ => speed)
                    .Writing<Sandbox.ModAPI.IMyTerminalBlock>((_, given) => speed = (int)given.Whole))
            .Looked("A test value.");
        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Editor.PressChoice("speed");
        screen.Run(2);
        DoubleOn(screen, "high");

        Assert.Equal("Sequence 1", screen.Ended);
        var act = Assert.IsType<Act>(screen.Editor.Program.Sequences[0].Steps[0]);
        Assert.Equal(Literal.Name("high"), Assert.Single(act.Arguments));
    }

    /// <summary>
    /// Pressing a step's values opens its picker with the caret in them, where they were first given.
    /// </summary>
    [Fact]
    public void PressingAStepsValuesOpensItsPickerOnThem()
    {
        var screen = Open(SetRadius + " " + RadiusBox + " type 5 " + Done + " " + Values);

        Assert.Equal(Slot, screen.Ended);
        Assert.False(screen.Scene.IsQuestioning, "a dialog was raised");
        Assert.True(screen.Scene.IsFilling, "the caret is not in the values");
        Assert.Equal("5.0", screen.Scene.Filling!.Text);
    }

    /// <summary>
    /// Leaving asks about any change: a value, or what is done, as well as the blocks.
    /// </summary>
    [Theory]
    [InlineData("420 192 type 7 escape")]
    [InlineData("208 182 press color")]
    public void LeavingAsksAboutAnyChange(string change)
    {
        var screen = Open(SetRadius + " " + RadiusBox + " type 5 enter " + change + " 25 62");

        Assert.True(screen.Scene.IsQuestioning, "leaving asked nothing");
        Assert.True(screen.Shows("Keep your changes?"), "the question is not about changes");
    }

    /// <summary>And nothing about a step only looked at, although its values show differently in their field.</summary>
    [Fact]
    public void LeavingAsksNothingWhereNothingChanged()
    {
        var screen = Open(SetRadius + " " + RadiusBox + " type 5 enter " + Values + " escape");

        Assert.False(screen.Scene.IsQuestioning, "leaving asked about 5 shown as 5.0");
        Assert.Equal("Sequence 1", screen.Ended);
    }

    /// <summary>Escape goes back, keeping what was typed in a field, so leaving a changed pick asks first.</summary>
    [Fact]
    public void EscapeKeepsTheFieldAndAsksBeforeLeaving()
    {
        var screen = Open(SetRadius + " " + RadiusBox + " type 7 escape");

        Assert.False(screen.Scene.IsFilling, "the field kept the keyboard");
        Assert.True(screen.Scene.IsQuestioning, "leaving a changed pick did not ask first");
        Assert.Equal(Slot, screen.Ended);
        Assert.True(screen.Shows("7"), "what was typed was thrown away");
    }
}
