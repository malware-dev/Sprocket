using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>What the picker offers for a vent and a tank, in their own words.</summary>
public class GasPickerTests
{
    /// <summary>A vent offers the switch under pressurize and depressurize as depressurizing, beside the act that flips it.</summary>
    [Fact]
    public void AVentOffersItsSwitchInItsOwnWords()
    {
        var screen = Screen.On(Rig.Demo());
        screen.Editor.NewButtonSystem("Airlock");
        screen.Run(2);
        screen.Editor.PressAdd("task");
        screen.Editor.PressBlock("Airlock Vent");
        screen.Run(2);

        Assert.True(screen.Shows("Pressurize"), "the vent's own actions are not listed");
        Assert.True(screen.Shows("Toggle depressurize"), "the act that flips the switch is not said in the vent's words");
        Assert.False(screen.Shows("Depressurize On/Off"), "the game's own words are listed beside Sprocket's");
    }

    /// <summary>A tank's stockpile asks whether to wait until full, and starts waiting.</summary>
    [Fact]
    public void StockpileAsksWhetherToWait()
    {
        var rig = Rig.Demo();
        // A definition of its own, since a kind is described once per definition from the first block of it seen, and the
        // demo's hydrogen tank is a plain block.
        rig.Weld(new Tank(30, "Reserve Tank", typeof(Sandbox.Common.ObjectBuilders.MyObjectBuilder_OxygenTank),
            "OxygenTankSmall", "Oxygen Tank"));
        var screen = Screen.On(rig);
        screen.Editor.NewButtonSystem("Airlock");
        screen.Run(2);
        screen.Editor.PressAdd("task");
        screen.Editor.PressBlock("Reserve Tank");
        screen.Editor.PressChoice("Stockpile");
        screen.Run(2);

        Assert.True(screen.Shows("wait until full"), "stockpile does not ask whether to wait");
        Assert.True(screen.Shows("until full"), "waiting is not what it starts with");

        screen.Editor.PressPrimary();
        screen.Run(2);

        var act = screen.Editor.Program.Sequences.SelectMany(sequence => sequence.Steps).OfType<Act>()
            .Single(step => step.ActionId == Blocks.Tank.Stockpile);
        Assert.True(Assert.Single(act.Arguments).Flag);
    }
}
