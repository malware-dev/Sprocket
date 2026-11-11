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

    /// <summary>In the game's fonts, the grid's name is drawn a step brighter than the words before it, a space after them.</summary>
    [Fact]
    public void TheGridsNameIsDrawnBrighterAfterASpace()
    {
        var rig = Rig.Demo();
        Rotor rotor = rig.Rotor(70, "Turret Rotor");
        rig.OnHeadOf(rotor);
        rig.Light(71, "Turret Light");
        rig.OnBase();

        var screen = Screen.WithGameFonts(rig);
        screen.Editor.NewButtonSystem("Turret");
        screen.Run(2);
        screen.Editor.PressAdd("task");
        screen.Run(2);

        Assert.True(screen.HasGameFonts, "the game's font was not found, so nothing was measured as the game measures it");
        var before = screen.Text("Interior Light on ");
        var grid = screen.Text("Turret Rotor Head");
        Assert.Equal(before.At.Y, grid.At.Y);
        Assert.Equal(before.Scale, grid.Scale);
        Assert.True(grid.At.X > before.At.X, "the grid's name stands before the words that lead to it");
        Assert.True(grid.Ink.R + grid.Ink.G + grid.Ink.B > before.Ink.R + before.Ink.G + before.Ink.B, $"the grid's name is drawn {grid.Ink}, the words before it {before.Ink}");
        Assert.Empty(screen.Overlapping);
    }
}
