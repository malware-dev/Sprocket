using Sprocket.Host.SpaceEngineers.Features.LogicSystems;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A logic system's switch, which is also what keeps it from being changed while it may run.</summary>
public class SystemSwitchTests
{
    /// <summary>Switched on, a logic system offers nothing that changes it, says why, and is switched off from where it says so.</summary>
    [Fact]
    public void ASystemSwitchedOnIsLockedAndSaysHowToUnlockIt()
    {
        var sim = Simulation.Fresh();
        sim.Editor.NewButtonSystem("Airlock");
        sim.Editor.Back();
        sim.Tick();

        var page = Assert.IsType<LogicSystemPage>(sim.Editor.Current);
        Assert.True(page.Editable);
        Assert.NotNull(sim.Editor.Triggers()[0].Deletes);

        sim.Editor.SwitchOn();
        sim.Tick();

        Assert.False(page.Editable);
        Assert.Equal("Airlock is switched on.", page.LockedText);
        Assert.Null(sim.Editor.Triggers()[0].Deletes);
        Assert.False(page.CanUndo);

        sim.Editor.PressStart("Switch it off to edit");
        sim.Tick();

        Assert.True(page.Editable);
        Assert.False(sim.Editor.Program.Systems[0].Enabled);
    }

    /// <summary>Switching a blueprint off asks first, naming each logic system it stops; keeping them running changes nothing.</summary>
    [Fact]
    public void SwitchingABlueprintOffAsksFirst()
    {
        var sim = Blueprinted();
        var system = sim.Editor.Program.Systems.Single();
        sim.Editor.OpenBlueprintSystem(0);
        sim.Tick();

        sim.Editor.SwitchOn();
        sim.Tick();
        Assert.True(system.Enabled);
        Assert.Equal(sim.Texts.RowSwitchBlueprintOff, sim.Editor.Screen.SwitchAbout);

        sim.Editor.Screen.Flip!.Execute();
        sim.Tick();

        Assert.Equal("Switch off the Airlock blueprint?", sim.Editor.Screen.Title());
        Assert.Contains("Airlock 2", Assert.IsType<ChoicePage>(sim.Editor.Current).Caption);

        sim.Editor.PressChoice("Keep them running");
        sim.Tick();
        Assert.True(system.Enabled);
        Assert.IsType<LogicSystemPage>(sim.Editor.Current);

        sim.Editor.Screen.Flip!.Execute();
        sim.Tick();
        sim.Editor.PressChoice("Switch them off");
        sim.Tick();

        Assert.False(system.Enabled);
        Assert.IsType<LogicSystemPage>(sim.Editor.Current);
    }

    /// <summary>A logic system with a button, made a blueprint, with a second logic system made from it.</summary>
    static Simulation Blueprinted()
    {
        var sim = Simulation.Fresh();
        sim.Editor.NewButtonSystem("Airlock");
        sim.Editor.GoToRoot();
        sim.Tick();

        sim.Editor.RootRows()[0].Offers!.Execute();
        sim.Editor.GoToRoot();
        sim.Tick();

        sim.Editor.RootRows().Single(row => row.IsAction).Opens!.Execute();
        sim.Tick();

        // Something done to it, so it stays when the panel is left.
        sim.Editor.Screen.Flip!.Execute();
        sim.Editor.GoToRoot();
        sim.Tick();

        return sim;
    }
}
