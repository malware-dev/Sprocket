using Sprocket.Host.SpaceEngineers.Words;
using Sprocket.Host.SpaceEngineers.World;
using Xunit;

namespace Sprocket.Tests;

/// <summary>
/// What a block's members are grouped under, worked out from the game's own ids.
/// </summary>
/// <remarks>
/// The table is keyed by id rather than by block, so what is worth testing is the reading of an id
/// rather than any one block's list: the game spells one member several ways, and every spelling has
/// to land in the same place.
/// </remarks>
public class CategoryTests
{
    [Fact]
    public void AMemberIsFoundByItsPlainId()
    {
        Assert.Equal(Category.Power, Categories.Of("OnOff"));
        Assert.Equal(Category.Opening, Categories.Of("Open"));
        Assert.Equal(Category.Movement, Categories.Of("Velocity"));
    }

    /// <summary>A switch is three ids, and all three are the same member.</summary>
    [Fact]
    public void TheOnAndOffOfASwitchGoWhereTheSwitchGoes()
    {
        Assert.Equal(Category.Opening, Categories.Of("Open_On"));
        Assert.Equal(Category.Opening, Categories.Of("Open_Off"));
        Assert.Equal(Category.Power, Categories.Of("OnOff_Off"));
    }

    /// <summary>And a slider is four more.</summary>
    [Fact]
    public void WhatMovesASliderGoesWhereTheSliderGoes()
    {
        Assert.Equal(Category.Movement, Categories.Of("IncreaseVelocity"));
        Assert.Equal(Category.Movement, Categories.Of("DecreaseVelocity"));
        Assert.Equal(Category.Movement, Categories.Of("ResetVelocity"));
        Assert.Equal(Category.Movement, Categories.Of("SetVelocity"));
    }

    /// <summary>
    /// The same member reaches the table spelled several ways.
    /// </summary>
    /// <remarks>
    /// A slider keeps the spaces a person reads and the action that sets it drops them, and the game
    /// is not consistent about the first letter either.
    /// </remarks>
    [Fact]
    public void SpacesAndCapitalsDoNotDecideWhereAMemberGoes()
    {
        Assert.Equal(Category.Appearance, Categories.Of("Blink Interval"));
        Assert.Equal(Category.Appearance, Categories.Of("SetBlinkInterval"));
        Assert.Equal(Category.Appearance, Categories.Of("IncreaseBlink Interval"));

        Assert.Equal(Category.Stock, Categories.Of("UseConveyor"));
        Assert.Equal(Category.Stock, Categories.Of("useConveyor"));

        Assert.Equal(Category.Air, Categories.Of("Auto-Refill"));
    }

    /// <summary>
    /// Some families are built one id per definition, so they are read by what they start with.
    /// </summary>
    [Fact]
    public void AFamilyBuiltPerDefinitionIsStillPlaced()
    {
        Assert.Equal(Category.Weapons, Categories.Of("TargetingGroup_Anything"));
        Assert.Equal(Category.Weapons, Categories.Of("TargetingGroup_CycleSubsystems"));
        Assert.Equal(Category.Detection, Categories.Of("Detect Small Ships_On"));
        Assert.Equal(Category.Signals, Categories.Of("Transmit Message 3"));
        Assert.Equal(Category.Access, Categories.Of("SafeZoneDrillingCb"));
    }

    /// <summary>An id nothing has been said about is a place in the list rather than a failure.</summary>
    [Fact]
    public void AnUnknownIdIsOther()
    {
        Assert.Equal(Category.Other, Categories.Of("SomethingAModAdded"));
        Assert.Equal(Category.Other, Categories.Of(""));
        Assert.Equal(Category.Other, Categories.Of(null));
    }

    /// <summary>Every category has a word over it, including the one nothing was said about.</summary>
    [Fact]
    public void EveryCategoryIsHeaded()
    {
        for (var category = Category.Power; category <= Category.Other; category++)
            Assert.False(string.IsNullOrEmpty(CategoryText.Of(category)), category.ToString());
    }
}
