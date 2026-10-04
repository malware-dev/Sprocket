using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Storage;
using Sprocket.Host.SpaceEngineers.Features.Terminal;
using Sprocket.Sim.Machine;
using VRage.Game.ModAPI;
using Xunit;
using TerminalActionParameter = Sandbox.ModAPI.Ingame.TerminalActionParameter;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A button picked for a blockset's slot is written into its panel, after asking about anything already there.</summary>
public class ButtonWiringTests
{
    /// <summary>Picking a button again writes its slot again, where it was cleared by hand since.</summary>
    [Fact]
    public void PickingAButtonAgainMendsItsSlot()
    {
        var sim = Blueprinted();
        var port = PanelNamed(sim, "Port Button");

        FillWith(sim, "Port Button");
        Assert.EndsWith("/2/1", KeyIn(port));

        port.Toolbar.Changing().Clear(0).Apply();
        Tick(sim);
        Assert.False(port.Toolbar.TryGetItem(0, out _));

        // The same button, which the picker opens on.
        sim.Editor.Parts()[0].Fills!.Execute();
        sim.Tick();
        sim.Editor.PressPrimary();
        Tick(sim);

        Assert.EndsWith("/2/1", KeyIn(port));
    }

    /// <summary>A button whose slot holds the game's own action is asked about; left alone nothing changes, and taken it is written over.</summary>
    [Fact]
    public void AButtonDoingSomethingElseIsAskedAbout()
    {
        var sim = Blueprinted();
        var port = PanelNamed(sim, "Port Button");
        var door = sim.Editor.World.Entities.First(entity => entity.Name == "Airlock Inner Door");
        port.Toolbar.Changing().Set(0, MyToolbarItemInfo.BlockAction(door.Id, "Open_On"), new List<TerminalActionParameter>()).Apply();
        Tick(sim);

        sim.Editor.Parts()[0].Fills!.Execute();
        sim.Tick();
        sim.Editor.PressBlock("Port Button");
        sim.Editor.PressChoice("is pressed");
        sim.Editor.PressPrimary();
        sim.Tick();

        Assert.Equal("Button 1 of Port Button already does something else.", sim.Editor.Question());

        sim.Editor.PressChoice(sim.Texts.ButtonLeaveAlone);
        Tick(sim);

        var cargo = sim.Editor.Program.Systems[0].Instances[1];
        Assert.Empty(cargo.Filled);
        Assert.True(port.Toolbar.TryGetItem(0, out var kept));
        Assert.Equal("Open_On", kept.ActionId);

        sim.Editor.PressPrimary();
        sim.Tick();
        sim.Editor.PressChoice(sim.Texts.ButtonTakeOver);
        Tick(sim);

        Assert.Single(cargo.Filled);
        Assert.EndsWith("/2/1", KeyIn(port));
    }

    /// <summary>Taking another blockset's button writes it for the one taking it, and the other is left needing a button.</summary>
    [Fact]
    public void TakingAnotherBlocksetsButtonRewiresIt()
    {
        var sim = Blueprinted();
        var button = PanelNamed(sim, "Airlock Button");
        Assert.EndsWith("/1/1", KeyIn(button));

        sim.Editor.Parts()[0].Fills!.Execute();
        sim.Tick();
        sim.Editor.PressBlock("Airlock Button");
        sim.Editor.PressChoice("is pressed");
        sim.Editor.PressPrimary();
        sim.Tick();

        Assert.Equal("Airlock Button already starts Sequence 1 on Installation 1 of Airlock.", sim.Editor.Question());

        sim.Editor.PressChoice(sim.Texts.ButtonTakeOver);
        Tick(sim);

        var system = sim.Editor.Program.Systems[0];
        Assert.Empty(system.Instances[0].Filled);
        Assert.Single(system.Instances[1].Filled);
        Assert.EndsWith("/2/1", KeyIn(button));
    }

    /// <summary>
    /// Each blockset answers the blueprint's trigger with its own button, and picking button 3 of a panel with four for
    /// the second writes that button and leaves the first blockset's alone.
    /// </summary>
    [Fact]
    public void EachBlocksetChoosesItsOwnButton()
    {
        var sim = Blueprinted();
        var four = sim.Machine.Panel(40, "Four Button", 4);
        Tick(sim);

        sim.Editor.Parts()[0].Fills!.Execute();
        sim.Tick();
        sim.Editor.PressBlock("Four Button");
        sim.Editor.PressChoice("button 3 is pressed");
        sim.Editor.PressPrimary();
        Tick(sim);

        Assert.EndsWith("/1/1", KeyIn(PanelNamed(sim, "Airlock Button")));
        Assert.EndsWith("/2/1", KeyIn(four, 2));
        Assert.Null(KeyIn(four));

        var system = sim.Editor.Program.Systems[0];
        var trigger = (RequiredTrigger)system.Triggers[0];
        Assert.Equal(1, system.Instances[0].Positions(trigger.Part)![0].Slot);
        Assert.Equal(3, system.Instances[1].Positions(trigger.Part)![0].Slot);
        Assert.Contains(":toolbar(1, 1, 3) ]", ProgramText.Save(sim.Editor.Program, sim.World.Values));
    }

    /// <summary>
    /// A logic system started from Airlock Button, made a blueprint, with a second blockset switched off and open on the
    /// configuration panel, and a second button on the construct.
    /// </summary>
    static Simulation Blueprinted()
    {
        var sim = Simulation.Fresh();
        sim.Machine.Panel(30, "Port Button");
        sim.Tick();

        sim.Editor.NewButtonSystem("Airlock");
        sim.Editor.GoToRoot();
        sim.Tick();

        sim.Editor.GoToRoot();
        sim.Tick();

        sim.Editor.AddInstallation(0);
        sim.Tick();

        var system = sim.Editor.Program.Systems[0];
        system.Instances[1].Enabled = false;
        Tick(sim);
        return sim;
    }

    static void FillWith(Simulation sim, string block)
    {
        sim.Editor.Parts()[0].Fills!.Execute();
        sim.Tick();
        sim.Editor.PressBlock(block);
        sim.Editor.PressChoice("is pressed");
        sim.Editor.PressPrimary();
        Tick(sim);
    }

    static void Tick(Simulation sim)
    {
        for (var i = 0; i < 5; i++)
            sim.Tick();
    }

    static Panel PanelNamed(Simulation sim, string name) => sim.Panels().Single(panel => panel.Name == name);

    /// <summary>The key Sprocket's action in one of the panel's slots carries, the first by default, or null where it holds something else.</summary>
    static string? KeyIn(Panel panel, int slot = 0)
    {
        if (!panel.Toolbar.TryGetItem(slot, out var present) || !TriggerAction.IsOurs(present))
            return null;

        var parameters = new List<TerminalActionParameter>();
        panel.Toolbar.Parameters(slot, parameters);
        return parameters.Count == 0 ? null : parameters[0].Value as string;
    }
}
