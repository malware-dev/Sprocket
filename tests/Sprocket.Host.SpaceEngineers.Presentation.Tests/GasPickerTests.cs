using Sprocket.Host.SpaceEngineers.Features.Picking;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>What the picker offers for a vent and a tank, in their own words.</summary>
public class GasPickerTests
{
    /// <summary>A vent offers the switch under pressurize and depressurize as depressurizing, which flips on its own switch.</summary>
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
        Assert.False(screen.Shows("Toggle depressurize"), "the toggle is listed beside the switch that flips");
        Assert.False(screen.Shows("Depressurize On/Off"), "the game's own words are listed beside Sprocket's");
    }

    /// <summary>
    /// A switch is set on, off, or toggled on its own three chips; toggle makes the step the game's toggle, and the step
    /// opens on toggle again.
    /// </summary>
    [Fact]
    public void ToggleOnASwitchIsTheGamesToggle()
    {
        var screen = Screen.On(Rig.Demo());
        screen.Editor.NewButtonSystem("Airlock");
        screen.Run(2);
        screen.Editor.PressAdd("task");
        screen.Editor.PressBlock("Airlock Vent");
        screen.Editor.PressChoice("Depressurizing");
        screen.Run(2);

        Assert.True(screen.Shows("on") && screen.Shows("off") && screen.Shows("toggle"), "the switch does not offer on, off and toggle");
        Assert.Equal(new[] { "on", "off", "toggle" }, Assert.IsType<PickPage>(screen.Editor.Current).Fields[0].Words);

        screen.Do("press", "toggle");
        screen.Editor.PressPrimary();
        screen.Run(2);

        var step = Assert.IsType<Act>(screen.Editor.Program.Sequences[0].Steps[0]);
        Assert.Equal(Form.Action, step.Form);
        Assert.Equal("Depressurize", step.ActionId);
        Assert.Empty(step.Arguments);

        screen.Editor.StepRows()[0].Slots[0].Opens!.Execute();
        screen.Run(2);
        var chosen = screen.Editor.Pick().Stages.Last().Entries().Single(entry => entry.IsChosen);
        Assert.Equal("Depressurizing", chosen.Text);
        Assert.Equal(EntityAction.Toggle, screen.Editor.Pick().Arguments!.Form.Answer);
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
