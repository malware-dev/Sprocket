using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>The block picker says which grid a block is on where that is not the programmable block's own.</summary>
public class SubgridPickTests
{
    /// <summary>A block on a rotor's head names the head's grid under its name; a block beside the programmable block names its kind alone.</summary>
    [Fact]
    public void ABlockOnASubgridNamesItsGrid()
    {
        var rig = Rig.Demo();
        Rotor rotor = rig.Rotor(70, "Turret Rotor");
        rig.OnHeadOf(rotor);
        rig.Light(71, "Turret Light");
        rig.OnBase();

        var screen = Screen.On(rig);
        screen.Editor.NewButtonSystem("Turret");
        screen.Run(2);
        screen.Editor.PressAdd("task");
        screen.Run(2);

        var rows = screen.Editor.Offered();
        Assert.Equal("Interior Light on Turret Rotor Head", rows.Single(row => row.Text == "Turret Light").Secondary);
        Assert.Equal("Interior Light", rows.Single(row => row.Text == "Airlock Light").Secondary);
    }
}
