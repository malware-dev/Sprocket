using Sprocket.Host.SpaceEngineers.Sequences;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// Backing out of a picker that has been filled in asks what to do with it.
/// </summary>
/// <remarks>
/// Back means back and both answers go back, so the question is only about what survives it. The
/// same question whichever way somebody arrived, so there is no rule to remember about how they
/// got here.
/// </remarks>
public class LeavingTests
{
    const string Filled = "60 130 type Airlock enter 60 161 200 375 560 357";
    const string Back = "19 62";

    /// <summary>A picker whose action will not do anything until it is given a value.</summary>
    const string WantsValues = "60 130 type Airlock enter 60 161 200 375 560 357 1380 1000 "
        + "1300 918 70 193 200 476 800 676";

    [Fact]
    public void LeavingAFilledPickerAsksFirst()
    {
        var picker = Screen.Fresh((Filled + " " + Back).Split(' '));

        Assert.True(picker.Shows("Keep your changes?"), "leaving asked nothing");
        Assert.True(picker.Scene.IsQuestioning, "the question does not own the screen");

        // Enter keeps them, because a key pressed without reading must not be the one that loses
        // the work the question exists to protect.
        Assert.True(picker.Scene.TakeDefault(), "Enter took no answer");
        picker.Run(2);

        Assert.True(picker.Editor.Program.Systems[0].Sequences.Count > 0
            || picker.Shows("What should it do?"), "keeping did not carry the choice through");
    }

    [Fact]
    public void AndDiscardingThrowsThemAway()
    {
        var picker = Screen.Fresh((Filled + " " + Back).Split(' '));

        picker.Editor.PressChoice("Discard them");
        picker.Run(2);

        Assert.False(picker.Shows("Keep your changes?"), "the question is still up");
        Assert.Empty(picker.Editor.Program.Systems[0].Sequences);
    }

    /// <summary>A picker opened and read asks nothing, because there is nothing to lose.</summary>
    [Fact]
    public void AndOneNobodyTouchedJustGoesBack()
    {
        var picker = Screen.Fresh(("60 130 type Airlock enter 60 161 " + Back).Split(' '));

        Assert.False(picker.Scene.IsQuestioning, "leaving an untouched picker asked something");
        Assert.False(picker.Shows("Keep your changes?"), "it asked anyway");
    }

    /// <summary>
    /// And keeping is pressing Done, including the values Done would have asked for.
    /// </summary>
    /// <remarks>
    /// The two are the same act arrived at from two directions, so an answer that did half of it -
    /// applying the blocks and dropping the values - would be a third behaviour wearing the words
    /// of the second.
    /// </remarks>
    [Fact]
    public void AndKeepingAsksForValuesTheSameWayDoneDoes()
    {
        var picker = Screen.Fresh((WantsValues + " " + Back).Split(' '));

        Assert.True(picker.Shows("Keep your changes?"), "leaving asked nothing");

        picker.Scene.TakeDefault();
        picker.Run(2);

        // Keeping is Done, and Done with nothing given says so and puts the caret in the field.
        Assert.True(picker.Shows("Radius needs a value."), "keeping did not ask for the value");
        Assert.True(picker.Scene.IsFilling, "the caret did not go to the field");

        picker.Do("type", "5");
        picker.Do("enter");
        picker.Run(2);

        // The step the picker was making, holding the value the column asked for.
        var act = Assert.IsType<Act>(picker.Editor.Program.Sequences[0].Steps[0]);
        Assert.Equal("Radius", act.ActionId);
        Assert.Single(act.Values);
    }
}
