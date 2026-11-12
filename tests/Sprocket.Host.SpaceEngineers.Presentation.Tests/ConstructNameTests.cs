using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>The home screen's heading names the construct for its largest grid, renames that grid, and follows it as it changes.</summary>
public class ConstructNameTests
{
    /// <summary>
    /// A construct this machine holds no Sprocket of, as a client turned away from it holds none, is still called for its largest
    /// grid, so the notice turning the player away names it.
    /// </summary>
    [Fact]
    public void AConstructWithNoSprocketHereIsCalledForItsLargestGrid()
    {
        var rig = Rig.Demo();
        var other = rig.Apart("Other Ship");
        rig.Light(300, "Other Light");
        rig.OnBase();
        var sim = Simulation.On(rig);

        Assert.Equal("Other Ship", new Constructs(sim.Roster).Called(other.Block));
    }

    /// <summary>A rotor's head holding more blocks than the base names the construct in the heading.</summary>
    [Fact]
    public void TheHeadingNamesTheLargestGrid()
    {
        var screen = Screen.On(Armed(out _));
        screen.Run(2);

        Assert.Equal("Arm Rotor Head", screen.Editor.Screen.CustomName());
    }

    /// <summary>Renaming the construct in the heading renames the largest grid, and the base keeps its name.</summary>
    [Fact]
    public void RenamingInTheHeadingRenamesTheLargestGrid()
    {
        var rig = Armed(out var rotor);
        var screen = Screen.On(rig);
        screen.Run(2);

        screen.Editor.Name("Crane");
        screen.Run(2);

        Assert.Equal("Crane", rotor.TopGrid.CustomName);
        Assert.Equal("Test Rig", rig.Sprocket.Block.CubeGrid.CustomName);
        Assert.Equal("Crane", screen.Editor.Screen.CustomName());
    }

    /// <summary>Armor ground off the head while the screen is up moves the heading to the base's name.</summary>
    [Fact]
    public void TheHeadingFollowsAGrindWhileUp()
    {
        var rig = Armed(out var rotor);
        rig.OnHeadOf(rotor);
        var armor = rig.Armor();
        rig.OnBase();
        var screen = Screen.On(rig);
        screen.Run(2);
        Assert.Equal("Arm Rotor Head", screen.Editor.Screen.CustomName());

        rig.Strip(armor);
        rig.Grind(rig.Fitted.First(fitting => fitting.Id == 10));
        screen.Run(61);

        Assert.Equal("Test Rig", screen.Editor.Screen.CustomName());
    }

    /// <summary>A construct of the base, holding the programmable block and a rotor, and the rotor's head with its top and two doors.</summary>
    static Rig Armed(out Rotor rotor)
    {
        var rig = new Rig();
        rotor = rig.Rotor(2, "Arm Rotor");
        rig.OnHeadOf(rotor);
        rig.Door(10, "Arm Door");
        rig.Door(11, "Arm Hatch");
        rig.OnBase();
        return rig;
    }
}
