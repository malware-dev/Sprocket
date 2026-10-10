using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A gun and a warhead in the picker: their members said in Sprocket's words, and made into steps by them.</summary>
public class WeaponTests
{
    /// <summary>Opening fire is offered in Sprocket's words and makes a step with the game's own action.</summary>
    [Fact]
    public void OpenFireIsPickedByItsOwnWords()
    {
        var rig = Rig.Demo();
        rig.Turret(30, "Turret");
        var screen = Screen.On(rig);
        screen.Editor.NewButtonSystem("Defense");
        screen.Run(2);
        screen.Editor.PressAdd("task");
        screen.Editor.PressBlock("Turret");
        screen.Run(2);

        Assert.True(screen.Shows("Open fire"), "open fire is not offered");
        Assert.True(screen.Shows("Toggle firing"), "toggle firing is not offered");
        Assert.False(screen.Shows("Shoot On"), "the game's own words are offered beside Sprocket's");

        screen.Editor.PressChoice("Open fire");
        screen.Run(2);
        screen.Editor.PressPrimary();
        screen.Run(2);

        Assert.Contains(screen.Editor.Program.Sequences.SelectMany(sequence => sequence.Steps).OfType<Act>(),
            step => step.ActionId == "Shoot_On");
    }

    /// <summary>A warhead's safety is offered as whether it is armed.</summary>
    [Fact]
    public void AWarheadIsArmed()
    {
        var rig = Rig.Demo();
        rig.Warhead(30, "Warhead");
        var screen = Screen.On(rig);
        screen.Editor.NewButtonSystem("Demolition");
        screen.Run(2);
        screen.Editor.PressAdd("task");
        screen.Editor.PressBlock("Warhead");
        screen.Run(2);

        Assert.True(screen.Shows("Armed"), "armed is not offered");
        Assert.True(screen.Shows("Toggle armed"), "toggle armed is not offered");
        Assert.True(screen.Shows("Detonate"), "detonate is not offered");
        Assert.False(screen.Shows("Arm"), "the game's own word is offered beside Sprocket's");
    }
}
