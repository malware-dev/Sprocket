using Sprocket.Host.SpaceEngineers.Localization;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// The power family as the picker and a step show it: a battery's members in Sprocket's words and a recharge that may move
/// on, a thruster's override and a jump drive's distance set by a step.
/// </summary>
public class PowerTests
{
    /// <summary>The picker lists a battery's toggles and charge mode in Sprocket's words, and leaves out what they stand for.</summary>
    [Fact]
    public void ABatteryIsListedInSprocketsWords()
    {
        var screen = OnABattery();

        foreach (var words in new[] { "Recharge", "Toggle recharge", "Toggle discharge", "Charge mode" })
            Assert.True(screen.Shows(words), "the battery does not offer \"" + words + "\"");

        Assert.False(screen.Shows("Recharge On"), "the game's own recharge is listed beside Sprocket's");
        Assert.False(screen.Shows("Enable Auto"), "auto is listed beside charge mode");
    }

    /// <summary>A recharge told to move on is written so, and its sentence says it.</summary>
    [Fact]
    public void ARechargeThatMovesOnSaysSo()
    {
        var screen = OnABattery();
        screen.Editor.PressChoice("Recharge");
        screen.Run(2);
        screen.Do("press", "\"move", "on\"");
        screen.Editor.PressPrimary();
        screen.Run(2);

        var act = screen.Editor.Program.Sequences.SelectMany(sequence => sequence.Steps).OfType<Act>()
            .Single(step => step.ActionId == Blocks.Battery.Recharge);
        Assert.False(Assert.Single(act.Arguments).Flag);
        Assert.True(screen.Shows("and move on"), "the step does not say it moves on");
    }

    /// <summary>A recharge left as it starts waits, and its sentence leaves the waiting unsaid.</summary>
    [Fact]
    public void ARechargeLeftAloneWaits()
    {
        var screen = OnABattery();
        screen.Editor.PressChoice("Recharge");
        screen.Run(2);
        screen.Editor.PressPrimary();
        screen.Run(2);

        var act = screen.Editor.Program.Sequences.SelectMany(sequence => sequence.Steps).OfType<Act>()
            .Single(step => step.ActionId == Blocks.Battery.Recharge);
        Assert.True(Assert.Single(act.Arguments).Flag);
        Assert.False(screen.Shows("and wait"), "the step says what it does anyway");
    }

    /// <summary>
    /// A thruster's override is offered in percent, held to nought to a hundred, and set in Sprocket's namespace; the game's
    /// override in newtons, its setter and its steps are not listed.
    /// </summary>
    [Fact]
    public void AThrustersOverrideIsSetInPercent()
    {
        var screen = OnA(rig => rig.Thruster(30, "Main Thruster"), "Main Thruster");

        Assert.False(screen.Shows("Thrust override"), "the game's override in newtons is listed beside Sprocket's");
        Assert.False(screen.Shows("Set override"), "the game's setter is listed beside Sprocket's override");
        Assert.False(screen.Shows("Increase Thrust override"), "the game's step is listed beside Sprocket's override");

        screen.Editor.PressChoice("Override");
        screen.Run(2);
        var field = Assert.Single(screen.Editor.Picking().Fields);
        Assert.Equal("override (0 - 100)", field.Label(new Texts()));
        Assert.Equal("Override has to be between 0 and 100.", field.Refuses(new Texts(), "Override", "101"));

        screen.Editor.Typed("40");
        screen.Run(2);

        var set = screen.Editor.Program.Sequences.SelectMany(sequence => sequence.Steps).OfType<Act>()
            .Single(step => step.ActionId == "override");
        Assert.Equal(BlockKinds.Own, set.Provider);
        Assert.Equal(40.0, Assert.Single(set.Arguments).Number);
    }

    /// <summary>A system with a step being added on the block <paramref name="name" /> that <paramref name="fit" /> puts on the rig, its members on screen.</summary>
    static Screen OnA(Action<Rig> fit, string name)
    {
        var rig = new Rig();
        rig.Panel(1, "Airlock Button");
        fit(rig);
        var screen = Screen.On(rig);
        screen.Editor.NewButtonSystem("Power");
        screen.Run(2);
        screen.Editor.PressAdd("task");
        screen.Editor.PressBlock(name);
        return screen.Run(2);
    }

    /// <summary>A system with a step being added on a battery, its members on screen.</summary>
    /// <remarks>A rig of its own, because the demonstration rig's battery is a plain block of the same definition.</remarks>
    static Screen OnABattery()
    {
        return OnA(rig => rig.Battery(30, "Main Battery"), "Main Battery");
    }
}
