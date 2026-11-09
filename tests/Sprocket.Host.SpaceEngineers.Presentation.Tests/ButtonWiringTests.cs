using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Storage;
using Sprocket.Host.SpaceEngineers.Features.Terminal;
using Sprocket.Host.SpaceEngineers.World;
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

    /// <summary>A button picked is written on the pass after the program's wiring changes.</summary>
    [Fact]
    public void AButtonPickedIsWrittenWhenTheWiringChanges()
    {
        var sim = Blueprinted();
        var port = PanelNamed(sim, "Port Button");
        FillWith(sim, "Port Button");
        port.Toolbar.Changing().Clear(0).Apply();
        Tick(sim);

        sim.Editor.Wire!(port.Id);
        Tick(sim);
        Assert.False(port.Toolbar.TryGetItem(0, out _));

        sim.Editor.Program.Rewire();
        Tick(sim);
        Assert.EndsWith("/2/1", KeyIn(port));
    }

    /// <summary>A pick backed out of leaves its button unwritten, so a slot cleared by hand stays clear when the wiring next changes.</summary>
    [Fact]
    public void APickBackedOutOfWritesNothing()
    {
        var sim = Blueprinted();
        var port = PanelNamed(sim, "Port Button");
        FillWith(sim, "Port Button");
        port.Toolbar.Changing().Clear(0).Apply();
        Tick(sim);

        sim.Editor.Wire!(port.Id);
        sim.Editor.Back();
        sim.Editor.Program.Rewire();
        Tick(sim);

        Assert.False(port.Toolbar.TryGetItem(0, out _));
    }

    /// <summary>A button picked for a signal slot is named after the slot, as the game names a button after what its slot holds.</summary>
    [Fact]
    public void AButtonPickedIsNamedAfterItsSlot()
    {
        var sim = Blueprinted();
        var port = PanelNamed(sim, "Port Button");

        FillWith(sim, "Port Button");

        Assert.Equal(sim.Editor.Parts()[0].Name, port.Label(0));
    }

    /// <summary>
    /// A signal slot's short name, where it has one, names the button and is the caption under the toolbar slot's icon; a
    /// change of it writes the slot again.
    /// </summary>
    [Fact]
    public void AButtonIsNamedAfterItsSlotsShortName()
    {
        var sim = Blueprinted();
        var port = PanelNamed(sim, "Port Button");
        FillWith(sim, "Port Button");
        var part = sim.Editor.Program.Systems[0].FindPart(sim.Editor.Parts()[0].Name)!;
        Assert.True(port.Toolbar.TryGetItem(0, out var item));
        Assert.Equal(part.Id, item.Label);

        part.Short = "Call";
        sim.Editor.Program.Rewire();
        Tick(sim);

        Assert.Equal("Call", port.Label(0));
        Assert.True(port.Toolbar.TryGetItem(0, out item));
        Assert.Equal("Call", item.Label);
        Assert.Equal(port.Id, item.BlockEntityId);
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
        sim.Editor.PressChoice("button 1 is pressed");
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

    /// <summary>A button taken over from the game's own action is given its action back by undo, and taken again by redo.</summary>
    [Fact]
    public void AButtonTakenOverIsGivenBackByUndo()
    {
        var sim = Blueprinted();
        var port = PanelNamed(sim, "Port Button");
        var door = sim.Editor.World.Entities.First(entity => entity.Name == "Airlock Inner Door");
        port.Toolbar.Changing().Set(0, MyToolbarItemInfo.BlockAction(door.Id, "Open_On"), new List<TerminalActionParameter>()).Apply();
        Tick(sim);

        sim.Editor.Parts()[0].Fills!.Execute();
        sim.Tick();
        sim.Editor.PressBlock("Port Button");
        sim.Editor.PressChoice("button 1 is pressed");
        sim.Editor.PressPrimary();
        sim.Tick();
        sim.Editor.PressChoice(sim.Texts.ButtonTakeOver);
        Tick(sim);
        Assert.EndsWith("/2/1", KeyIn(port));

        Assert.Equal(Filled.With(sim.Texts.ChangeFillSlot, "thing", "signal slot 1", "place", "Installation 2", "now", "Port Button"), sim.Editor.Screen.UndoText);
        sim.Editor.Screen.Undo!.Execute();
        Tick(sim);

        var cargo = sim.Editor.Program.Systems[0].Instances[1];
        Assert.Empty(cargo.Filled);
        Assert.True(port.Toolbar.TryGetItem(0, out var given));
        Assert.Equal("Open_On", given.ActionId);

        sim.Editor.Screen.Redo!.Execute();
        Tick(sim);
        Assert.EndsWith("/2/1", KeyIn(port));
    }

    /// <summary>An installation that may run keeps its blocks: its panel offers no fill, and says why.</summary>
    [Fact]
    public void ARunningInstallationIsNotFilled()
    {
        var sim = Blueprinted();
        var system = sim.Editor.Program.Systems[0];
        var first = system.Instances[0];
        sim.Editor.Choose(system, first);
        sim.Editor.Push(new Features.Blueprints.BlueprintPage(sim.Editor, system));
        system.Enabled = true;
        first.Enabled = true;
        Tick(sim);

        Assert.Null(sim.Editor.Parts()[0].Fills);

        first.Enabled = false;
        Tick(sim);
        Assert.NotNull(sim.Editor.Parts()[0].Fills);
    }

    /// <summary>A button a toolbar signal written in place already answers is asked about, as one a named signal answers is.</summary>
    [Fact]
    public void AButtonAWrittenSignalAnswersIsAskedAbout()
    {
        var sim = Blueprinted();
        var port = sim.Editor.World.Entities.First(entity => entity.Name == "Port Button");
        var other = sim.Editor.Program.AddSequence(sim.Editor.Program.AddSystem("Bell"));
        var written = SignalTrigger.Written(BlockKinds.Own, ScriptFormat.Toolbar);
        written.At = new ToolbarSlot(1, 1, 1);
        written.Sources.Add(sim.Editor.Program.Declare(sim.Editor.World, port).Name);
        written.Does = new Run(other.Number);
        other.System.AddTrigger(written);
        sim.Editor.Program.Edited();
        Tick(sim);
        Assert.StartsWith(other.System.Identity.ToString("D"), KeyIn(PanelNamed(sim, "Port Button")));

        sim.Editor.Parts()[0].Fills!.Execute();
        sim.Tick();
        sim.Editor.PressBlock("Port Button");
        sim.Editor.PressChoice("button 1 is pressed");
        sim.Editor.PressPrimary();
        sim.Tick();

        Assert.StartsWith("Port Button already starts", sim.Editor.Question());
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
        sim.Editor.PressChoice("button 1 is pressed");
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
        var trigger = (SignalTrigger)system.Triggers[0];
        Assert.Equal(1, system.Instances[0].Positions(trigger.Part)![0].Slot);
        Assert.Equal(3, system.Instances[1].Positions(trigger.Part)![0].Slot);
        Assert.Contains("ext.toolbar(1, 1, 3) on [ ", ProgramWriter.Save(sim.Editor.Program, sim.World.Values));
    }

    /// <summary>
    /// A logic system started from Airlock Button, made a blueprint, with a second blockset switched off and open on the
    /// installation panel, and a second button on the construct.
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
        sim.Editor.PressChoice("button 1 is pressed");
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
        if (!panel.Toolbar.TryGetItem(slot, out var present) || !SignalAction.IsOurs(present))
            return null;

        var parameters = new List<TerminalActionParameter>();
        panel.Toolbar.Parameters(slot, parameters);
        return parameters.Count == 0 ? null : parameters[0].Value as string;
    }
}
