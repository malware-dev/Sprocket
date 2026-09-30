using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Features.LogicSystems;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A logic system's switch, which is also what keeps it from being changed while it may run.</summary>
public class SystemSwitchTests
{
    /// <summary>Switched on, a logic system offers nothing that changes it, and says why.</summary>
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
        Assert.Equal("Airlock is switched on. Switch it off to change it.", page.LockedText);
        Assert.Null(sim.Editor.Triggers()[0].Deletes);
        Assert.False(page.CanUndo);

        sim.Editor.Screen.Flip!.Execute();
        sim.Tick();

        Assert.True(page.Editable);
        Assert.False(sim.Editor.Program.Systems[0].Enabled);
    }

    /// <summary>Switched on, every part that cannot be pressed says how to change it, and says nothing once it can be.</summary>
    [Fact]
    public void ALockedPartSaysHowToEditIt()
    {
        var sim = Simulation.Fresh();
        sim.Editor.NewButtonSystem("Airlock");
        sim.Editor.Back();
        sim.Tick();

        sim.Editor.SwitchOn();
        sim.Tick();

        var chips = sim.Editor.Triggers()[0].Within().OfType<Slot>().ToList();
        Assert.NotEmpty(chips);
        Assert.All(chips, chip => Assert.Equal("Turn the system off to edit.", chip.Tip?.Text));

        sim.Editor.Screen.Flip!.Execute();
        sim.Tick();

        Assert.All(sim.Editor.Triggers()[0].Within().OfType<Slot>(), chip => Assert.Null(chip.Tip));
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

        Assert.StartsWith("Switch off the Airlock blueprint?", sim.Editor.Question());
        Assert.Contains("Airlock 2", sim.Editor.Question());

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

    /// <summary>The question before a blueprint is switched off lists its blocksets a line each, and past five counts the rest.</summary>
    [Fact]
    public void SwitchingOffABlueprintWithManyBlocksetsListsSomeAndCountsTheRest()
    {
        var sim = Blueprinted();
        var system = sim.Editor.Program.Systems.Single();
        for (var i = 1; i <= 5; i++)
            system.AddInstance().Name = "Extra " + i;

        sim.Editor.OpenBlueprintSystem(0);
        sim.Tick();
        sim.Editor.SwitchOn();
        sim.Tick();
        sim.Editor.Screen.Flip!.Execute();
        sim.Tick();

        var question = sim.Editor.Question();
        Assert.Contains("\n- Airlock 2\n- Extra 1\n- Extra 2\n- and 3 more", question);
        Assert.DoesNotContain("Extra 3", question);
    }

    /// <summary>With the blueprint off, its blocksets show off, and switching one on asks about the blueprint first.</summary>
    [Fact]
    public void SwitchingOnABlocksetOfABlueprintSwitchedOffAsksAboutTheBlueprint()
    {
        var sim = Blueprinted();
        var system = sim.Editor.Program.Systems.Single();
        system.Enabled = false;
        sim.Editor.Tick(1000);

        var row = sim.Editor.RootRows()[0];
        Assert.True(system.Instances[0].Enabled);
        Assert.False(row.SwitchIsOn);

        row.Flips!.Execute();
        sim.Editor.Tick(1001);
        Assert.Equal("Airlock is switched off. Switch it on?", sim.Editor.Question());
        Assert.False(system.Enabled);

        sim.Editor.PressAnswer("Switch it on");
        sim.Editor.Tick(1002);

        Assert.True(system.Enabled);
        Assert.True(system.Instances[0].Enabled);
        Assert.True(sim.Editor.RootRows()[0].SwitchIsOn);
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

        // Something done to it, switched off, so it stays when the panel is left.
        sim.Editor.Program.Systems[0].Instances[sim.Editor.Program.Systems[0].Instances.Count - 1].Enabled = false;
        sim.Editor.GoToRoot();
        sim.Tick();

        return sim;
    }
}
