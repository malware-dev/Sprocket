using Sprocket.Host.SpaceEngineers.Features.Blueprints;
using Sprocket.Host.SpaceEngineers.Features.LogicSystems;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A slot nothing uses is kept, warned about on the logic system's installation panel and removed there with its bin, and left off an installation's own page.</summary>
public class UnusedSlotTests
{
    const string Light = "Airlock Light";
    const string Cockpit = "Cockpit Light";

    /// <summary>The slot of a step taken away stays on the installation panel, warned about, with its blocks and a bin.</summary>
    [Fact]
    public void AnUnusedSlotIsWarnedAbout()
    {
        var screen = Unused(out var system, out var slot);

        var row = screen.Editor.SystemSlots().Single(r => r.Name == slot);
        Assert.Equal(screen.Editor.Texts.SlotUnused, row.Problem);
        Assert.Equal(NoticeLevel.Warning, row.Level);
        Assert.Single(row.Blocks);
        Assert.True(row.Removable);
        Assert.NotNull(row.Removes);
        Assert.Null(screen.Editor.SystemSlots().Single(r => r.Name == Assert.Single(Targets(system))).Problem);
    }

    /// <summary>The bin takes the slot out with every installation's blocks in it, and Undo puts both back.</summary>
    [Fact]
    public void TheBinRemovesItAndUndoPutsItBack()
    {
        var screen = Unused(out var system, out var slot);
        var port = system.AddInstance();
        port.Fill(slot, new[] { Declared(screen, Cockpit) });
        screen.Run(2);

        screen.Editor.SystemSlots().Single(r => r.Name == slot).Removes!.Execute();
        screen.Run(2);
        Assert.Null(system.FindPart(slot));
        Assert.Null(port.FilledWith(slot));
        Assert.DoesNotContain(screen.Editor.SystemSlots(), r => r.Name == slot);
        Assert.EndsWith(Filled.With(screen.Editor.Texts.ChangeRemoveSlot, "thing", slot), screen.Editor.Screen.UndoText);

        screen.Editor.Screen.Undo!.Execute();
        screen.Run(2);
        Assert.NotNull(system.FindPart(slot));
        Assert.Equal(new[] { Declared(screen, Cockpit) }, port.FilledWith(slot));
        Assert.Single(system.First!.FilledWith(slot)!);
    }

    /// <summary>While the logic system is switched on, the bin is shown but cannot be pressed, and says why.</summary>
    [Fact]
    public void TheBinWaitsForTheSwitch()
    {
        var screen = Unused(out var system, out var slot);
        screen.Editor.SwitchSystem(system, true);
        screen.Run(2);

        var row = screen.Editor.SystemSlots().Single(r => r.Name == slot);
        Assert.True(row.Removable);
        Assert.Null(row.Removes);
        Assert.Equal(screen.Editor.Texts.SlotUnused, row.Problem);
    }

    /// <summary>An installation's own page leaves out a slot nothing uses, and does not count it among the slots it has to fill.</summary>
    [Fact]
    public void AnInstallationsPageLeavesOutAnUnusedSlot()
    {
        var screen = Unused(out var system, out var slot);
        var port = system.AddInstance();
        port.Fill(Assert.Single(Targets(system)), new[] { Declared(screen, Cockpit) });
        foreach (var part in system.Parts.Where(part => part.IsSignal))
            port.Fill(part.Id, system.First!.FilledWith(part.Id)!, system.First!.Positions(part.Id)!);

        screen.Editor.Push(new BlueprintPage(screen.Editor, system));
        screen.Run(2);
        screen.Editor.Choose(system, port);
        screen.Run(2);

        Assert.DoesNotContain(screen.Editor.Parts(), r => r.Name == slot);
        Assert.Contains(screen.Editor.Parts(), r => r.Name == Assert.Single(Targets(system)));
        Assert.DoesNotContain(screen.Editor.Blueprint().View.Within().OfType<Lumel.Text.Label>(), label => label.IsVisible && label.Text == screen.Editor.Texts.BlueprintStillOne);
    }

    /// <summary>An unused slot left empty by another installation is not one it still has to fill on the logic system's screen.</summary>
    [Fact]
    public void AnUnusedSlotAsksNothingOfAnInstallation()
    {
        var screen = Unused(out var system, out var slot);
        var port = system.AddInstance();
        port.Fill(Assert.Single(Targets(system)), new[] { Declared(screen, Cockpit) });
        foreach (var part in system.Parts.Where(part => part.IsSignal))
            port.Fill(part.Id, system.First!.FilledWith(part.Id)!, system.First!.Positions(part.Id)!);

        screen.Editor.Choose(system, port);
        screen.Run(2);
        var slots = screen.Editor.SystemSlots();
        Assert.Null(slots.Single(r => r.Name == slot).Marks);
        Assert.Equal(screen.Editor.Texts.SlotUnused, slots.Single(r => r.Name == slot).Problem);
    }

    /// <summary>The warning on the logic system's screen is amber, and its bin removes the slot.</summary>
    [Fact]
    public void TheWarningIsAmberAndTheBinRemovesIt()
    {
        var screen = Unused(out var system, out var slot);

        var row = screen.Editor.SystemSlots().Single(r => r.Name == slot);
        Assert.True(row.Removable);
        var warning = row.Within().OfType<Lumel.Text.Label>().Single(label => label.Text == screen.Editor.Texts.SlotUnused);
        Assert.Equal(Features.Look.Palette.Notice, warning.Color);
        row.Removes!.Execute();
        screen.Run(2);
        Assert.Null(system.FindPart(slot));
    }

    /// <summary>A logic system with a step on one slot and a second slot whose step was taken away, on its installation panel tab.</summary>
    static Screen Unused(out AutomationSystem system, out string slot)
    {
        var screen = Screen.On(Rig.Demo());
        screen.Editor.NewButtonSystem("Airlock");
        screen.Run(2);
        system = screen.Editor.Program.Systems[0];

        Off(screen, Light);
        Off(screen, Cockpit);
        slot = Assert.Single(system.Sequences[0].Steps.OfType<Act>().ElementAt(1).Targets);
        screen.Editor.StepRows()[1].Deletes!.Execute();
        screen.Run(2);

        screen.Editor.BackKeeping();
        screen.Run(2);
        screen.Editor.Push(new LogicSystemPage(screen.Editor, system));
        screen.Run(2);
        screen.Editor.PressTab(screen.Editor.Texts.SystemInstallationPanel);
        screen.Run(2);
        return screen;
    }

    /// <summary>Adds a step switching <paramref name="block" /> off.</summary>
    static void Off(Screen screen, string block)
    {
        screen.Editor.PressAdd("task");
        screen.Run(2);
        screen.Editor.PressBlock(block);
        screen.Editor.PressChoice("power");
        screen.Editor.Pick().Arguments!.Form.Fields[0].Text = "off";
        screen.Editor.PressPrimary();
        screen.Run(2);
    }

    /// <summary>What the one act of the first sequence names.</summary>
    static List<string> Targets(AutomationSystem system) =>
        system.Sequences[0].Steps.OfType<Act>().Single().Targets.ToList();

    /// <summary>The variable the block called <paramref name="name" /> is declared as.</summary>
    static string Declared(Screen screen, string name) =>
        screen.Editor.Program.Declare(screen.Editor.World, screen.Editor.World.Entities.First(e => e.Name == name)).Name;
}
