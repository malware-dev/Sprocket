using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A battery as the picker and a step show it: its members in Sprocket's words, and a recharge that may move on.</summary>
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

    /// <summary>A system with a step being added on a battery, its members on screen.</summary>
    /// <remarks>A rig of its own, because the demonstration rig's battery is a plain block of the same definition.</remarks>
    static Screen OnABattery()
    {
        var rig = new Rig();
        rig.Panel(1, "Airlock Button");
        rig.Battery(30, "Main Battery");
        var screen = Screen.On(rig);
        screen.Editor.NewButtonSystem("Power");
        screen.Run(2);
        screen.Editor.PressAdd("task");
        screen.Editor.PressBlock("Main Battery");
        return screen.Run(2);
    }
}
