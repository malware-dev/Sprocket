using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Features.LogicSystems;
using Sprocket.Host.SpaceEngineers.Features.Look;
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
        sim.Editor.BackKeeping();
        sim.Tick();

        var page = Assert.IsType<LogicSystemPage>(sim.Editor.Current);
        Assert.True(page.Editable);
        Assert.NotNull(sim.Editor.Triggers()[0].Deletes);

        sim.Editor.SwitchOn();
        sim.Tick();

        Assert.False(page.Editable);
        Assert.Equal("Airlock must be turned off in order to be edited safely.", page.LockedText);
        Assert.Null(sim.Editor.Triggers()[0].Deletes);
        Assert.False(page.CanUndo);

        // A logic system with installations asks before it stops them.
        sim.Editor.Screen.Flip!.Execute();
        sim.Tick();
        sim.Editor.PressChoice("Switch them off");
        sim.Tick();

        Assert.True(page.Editable);
        Assert.False(sim.Editor.Program.Systems[0].Enabled);
    }

    /// <summary>A sequence's row shows no state while its logic system is off.</summary>
    [Fact]
    public void ASequenceRowSaysNothingWhileItsSystemIsOff()
    {
        var sim = Simulation.Fresh();
        sim.Editor.NewButtonSystem("Airlock");
        sim.Editor.BackKeeping();
        sim.Tick();

        var row = sim.Editor.Rows()[0];
        Assert.False(sim.Editor.Program.Systems[0].Enabled);
        Assert.Equal("", row.Status);

        sim.Editor.SwitchOn();
        sim.Tick();

        Assert.Equal("ready", sim.Editor.Rows()[0].Status);
    }

    /// <summary>A logic system or installation switched off is washed as a warning, an installation as well while its logic system is off.</summary>
    [Fact]
    public void ASwitchedOffRowIsWashedAsAWarning()
    {
        var sim = Simulation.Fresh();
        sim.Editor.NewButtonSystem("Airlock");
        var system = sim.Editor.Program.Systems[0];
        sim.Editor.GoToRoot();
        sim.Tick();

        var heading = Assert.Single(sim.Editor.RootHeadings());
        var installation = Assert.Single(sim.Editor.RootRows());
        Assert.Same(Styles.Parked, heading.Style);
        Assert.Same(Styles.Parked, installation.Style);

        sim.Editor.SwitchSystem(system, true);
        sim.Tick();
        Assert.NotSame(Styles.Parked, heading.Style);
        Assert.NotSame(Styles.Parked, installation.Style);

        sim.Editor.SwitchInstance(system, system.First, false);
        sim.Tick();
        Assert.NotSame(Styles.Parked, heading.Style);
        Assert.Same(Styles.Parked, installation.Style);
    }

    /// <summary>Switched on, every part that cannot be pressed says how to change it, and stops saying so once it can be.</summary>
    [Fact]
    public void ALockedPartSaysHowToEditIt()
    {
        var sim = Simulation.Fresh();
        sim.Editor.NewButtonSystem("Airlock");
        sim.Editor.BackKeeping();
        sim.Tick();

        sim.Editor.SwitchOn();
        sim.Tick();

        var chips = sim.Editor.Triggers()[0].Within().OfType<SlotChip>().ToList();
        Assert.NotEmpty(chips);
        Assert.All(chips, chip => Assert.EndsWith("The system must be turned off in order to make changes safely.", chip.Tip?.Text?.TrimEnd()));

        sim.Editor.Screen.Flip!.Execute();
        sim.Tick();
        sim.Editor.PressChoice("Switch them off");
        sim.Tick();

        Assert.All(sim.Editor.Triggers()[0].Within().OfType<SlotChip>(), chip => Assert.DoesNotContain("The system must be turned off in order to make changes safely.", chip.Tip?.Text ?? ""));
    }

    /// <summary>Switched on, every button and part of a sentence that cannot be pressed, on each of the logic system's tabs, says why.</summary>
    [Fact]
    public void NothingLockedIsSilentOnAnyTab()
    {
        var sim = Simulation.Fresh();
        sim.Editor.NewButtonSystem("Airlock");
        sim.Editor.PressAdd("task");
        sim.Editor.PressBlock("Airlock Light");
        sim.Editor.PressChoice("power");
        sim.Editor.PressPrimary();
        sim.Tick();
        sim.Editor.BackKeeping();
        sim.Tick();
        sim.Editor.SwitchOn();
        sim.Tick();

        var texts = sim.Editor.Texts;
        foreach (var tab in new[] { texts.SystemTriggers, texts.SystemSequences, texts.SystemState, texts.SystemInstallationPanel })
        {
            sim.Editor.PressTab(tab);
            sim.Tick();
            var silent = sim.Editor.Current!.View.Within()
                .Where(element => (element is Lumel.Controls.Button || element is SlotChip) && element.Command == null && Shown(element) && element.Tip == null)
                .ToList();
            Assert.True(silent.Count == 0, $"{tab}: {silent.Count} locked without saying why");
        }
    }

    /// <summary>Whether <paramref name="element" /> and everything it stands in is shown.</summary>
    static bool Shown(Lumel.Element element)
    {
        for (var at = element; at != null; at = at.Parent)
        {
            if (!at.IsVisible)
                return false;
        }

        return true;
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
        Assert.Equal(sim.Texts.Codex.RowSwitchBlueprintOff, sim.Editor.Screen.SwitchAbout);

        sim.Editor.Screen.Flip!.Execute();
        sim.Tick();

        Assert.StartsWith("Switch off the Airlock logic system?", sim.Editor.Question());
        Assert.Contains("Installation 2", sim.Editor.Question());

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
        Assert.Contains("\n- Installation 2\n- Extra 1\n- Extra 2\n- and 3 more", question);
        Assert.DoesNotContain("Extra 3", question);
    }

    /// <summary>With the blueprint off, a blockset's switch keeps showing its own setting and waits for the blueprint's.</summary>
    [Fact]
    public void ABlocksetsSwitchWaitsForTheBlueprint()
    {
        var sim = Blueprinted();
        var system = sim.Editor.Program.Systems.Single();
        system.Enabled = false;
        sim.Editor.Tick(1000);

        var row = sim.Editor.RootRows()[0];
        Assert.True(row.SwitchIsOn);
        Assert.Equal("The logic system must be switched on first.", row.SwitchLocked);

        row.Flips!.Execute();
        sim.Editor.Tick(1001);
        Assert.Equal("", sim.Editor.Question());
        Assert.True(system.Instances[0].Enabled);

        system.Enabled = true;
        sim.Editor.Tick(1002);
        Assert.Null(sim.Editor.RootRows()[0].SwitchLocked);
    }

    /// <summary>A logic system with a button, made a blueprint, with a second logic system made from it.</summary>
    static Simulation Blueprinted()
    {
        var sim = Simulation.Fresh();
        sim.Editor.NewButtonSystem("Airlock");
        sim.Editor.GoToRoot();
        sim.Tick();

        sim.Editor.GoToRoot();
        sim.Tick();

        sim.Editor.AddInstallation(0);
        sim.Tick();

        // Something done to it, switched on, so it stays when the panel is left.
        sim.Editor.Program.Systems[0].Instances[sim.Editor.Program.Systems[0].Instances.Count - 1].Enabled = true;
        sim.Editor.GoToRoot();
        sim.Tick();

        return sim;
    }
}
