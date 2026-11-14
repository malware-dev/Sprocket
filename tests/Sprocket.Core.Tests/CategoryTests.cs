using Sprocket.Host.SpaceEngineers.Features.Selector.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Text;
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

        Assert.Equal(Category.Behaviour, Categories.Of("UseConveyor"));
        Assert.Equal(Category.Behaviour, Categories.Of("useConveyor"));

        Assert.Equal(Category.Behaviour, Categories.Of("Auto-Refill"));

        // A tank holds hydrogen as often as oxygen, so its store is gas; the air in a room is the vent's alone.
        Assert.Equal(Category.Gas, Categories.Of("Stockpile"));
        Assert.Equal(Category.Gas, Categories.Of("Stockpile_On"));
        Assert.Equal(Category.Air, Categories.Of("Depressurize"));
    }

    /// <summary>
    /// Some families are built one id per definition, so they are read by what they start with.
    /// </summary>
    [Fact]
    public void AFamilyBuiltPerDefinitionIsStillPlaced()
    {
        Assert.Equal(Category.Weapons, Categories.Of("TargetingGroup_Anything"));
        Assert.Equal(Category.Weapons, Categories.Of("TargetingGroup_CycleSubsystems"));
        Assert.Equal(Category.Behaviour, Categories.Of("Detect Small Ships_On"));
        Assert.Equal(Category.Signals, Categories.Of("Transmit Message 3"));
        Assert.Equal(Category.Access, Categories.Of("SafeZoneDrillingCb"));
    }

    /// <summary>
    /// A member whose only id begins with Set is found by what it sets.
    /// </summary>
    /// <remarks>
    /// The stem is what the table answers, so a button the game only ever registers one way still
    /// has to be entered without the prefix. Written down because it is an easy entry to make wrong
    /// and a silent one: the case simply never matches and the member falls to Other.
    /// </remarks>
    [Fact]
    public void AButtonNamedForWhatItSetsIsStillFound()
    {
        Assert.Equal(Category.Access, Categories.Of("SetFaction"));
        Assert.Equal(Category.Automation, Categories.Of("SetCurrentPositionHome"));
        Assert.Equal(Category.Signals, Categories.Of("SetChannelValue"));
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
            Assert.False(string.IsNullOrEmpty(CategoryText.Of(new Texts(), category)), category.ToString());
    }
}
