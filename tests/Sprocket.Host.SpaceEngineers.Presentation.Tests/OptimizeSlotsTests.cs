using Lumel.Controls;
using Sprocket.Host.SpaceEngineers.Features.Blueprints;
using Sprocket.Host.SpaceEngineers.Features.LogicSystems;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// Optimize slots on the logic system's installation panel tab lists the changes that give a logic system as few slots as its steps and triggers
/// can name, and makes them only once they are approved, as one step back.
/// </summary>
public class OptimizeSlotsTests
{
    const string Inner = "Airlock Inner Door";
    const string Outer = "Airlock Outer Door";
    const string Light = "Airlock Light";
    const string Cockpit = "Cockpit Light";

    /// <summary>The button is pressed while the logic system is switched off, and says why it cannot be while it is on.</summary>
    [Fact]
    public void OptimizeWaitsForTheSwitch()
    {
        var screen = Built(out var system);
        Off(screen, Light);
        var editor = screen.Editor;
        Panel(screen, system);
        Assert.NotNull(Optimize(screen).Command);

        editor.SwitchSystem(system, true);
        screen.Run(2);

        var button = Optimize(screen);
        Assert.True(button.IsVisible);
        Assert.Null(button.Command);
        Assert.Equal(editor.Texts.Codex.SystemTurnOffToEdit, button.Tip?.Text);
    }

    /// <summary>The button sits on the line of the SLOTS heading, above the slots it tidies.</summary>
    [Fact]
    public void OptimizeSitsOnTheSlotsHeading()
    {
        var screen = Built(out var system);
        Off(screen, Light);
        Panel(screen, system);

        var button = Optimize(screen).Bounds;
        var heading = screen.Text(screen.Editor.Texts.BlueprintParts.ToUpperInvariant());
        var slots = ((LogicSystemPage)screen.Editor.Current!).View.Within().OfType<PartList>().Single().Bounds;
        Assert.InRange(heading.At.Y, button.Y, button.Y + button.Height);
        Assert.True(button.Y + button.Height <= slots.Y, "the button sits above the slots");
    }

    /// <summary>Pressed, it lists each change and changes nothing, and Cancel leaves everything as it was.</summary>
    [Fact]
    public void ItListsTheChangesAndCancelChangesNothing()
    {
        var screen = Whole(out var system, out var whole);
        var before = Targets(system, 0);

        Press(screen);

        var question = screen.Editor.Question();
        Assert.StartsWith(screen.Editor.Texts.SlotOptimizeQuestion, question);
        Assert.Contains("- Steps and triggers naming " + whole + " name ", question);
        Assert.Contains("- " + Filled.With(screen.Editor.Texts.SlotOptimizeRemoves, "slot", whole), question);
        Assert.Equal(before, Targets(system, 0));
        Assert.NotNull(system.FindPart(whole));

        screen.Editor.PressAnswer(screen.Editor.Texts.SlotOptimizeCancel);
        screen.Run(2);
        Assert.Equal(before, Targets(system, 0));
        Assert.NotNull(system.FindPart(whole));
    }

    /// <summary>Approved, the changes are made, and one Undo takes every one of them back.</summary>
    [Fact]
    public void ApprovedTheChangesAreOneStepBack()
    {
        var screen = Whole(out var system, out var whole);
        var before = Targets(system, 0);
        var halves = Targets(system, 1).Concat(Targets(system, 2)).OrderBy(name => name).ToList();

        Press(screen);
        screen.Editor.PressAnswer(screen.Editor.Texts.SlotOptimizeApply);
        screen.Run(2);

        Assert.Equal(halves, Targets(system, 0).OrderBy(name => name));
        Assert.Null(system.FindPart(whole));
        Assert.EndsWith(screen.Editor.Texts.ChangeOptimizeSlots, screen.Editor.Screen.UndoText);

        screen.Editor.Screen.Undo!.Execute();
        screen.Run(2);
        Assert.Equal(before, Targets(system, 0));
        Assert.Equal(4, system.First!.FilledWith(whole)!.Count);
    }

    /// <summary>A slot the change makes is numbered on from the slots there are, holds the blocks, and the slots it replaces go.</summary>
    [Fact]
    public void ANewSlotIsNumbered()
    {
        var screen = Built(out var system);
        Off(screen, Inner, Outer);
        Off(screen, Light);
        Off(screen, Inner, Outer, Light);
        var parts = Targets(system, 2);
        Assert.Equal(2, parts.Count);
        Remove(screen, 1);
        Remove(screen, 0);
        var number = system.Parts.Count + 1;
        Panel(screen, system);

        Press(screen);
        var makes = Filled.With(screen.Editor.Texts.SlotOptimizeMakes, "slot", "slot " + number);
        Assert.Contains("- " + makes.Substring(0, makes.IndexOf("{blocks}", StringComparison.Ordinal)), screen.Editor.Question());
        screen.Editor.PressAnswer(screen.Editor.Texts.SlotOptimizeApply);
        screen.Run(2);

        var slot = Assert.Single(Targets(system, 0));
        Assert.Equal("slot " + number, slot);
        Assert.Equal(3, system.First!.FilledWith(slot)!.Count);
        Assert.All(parts, part => Assert.Null(system.FindPart(part)));
    }

    /// <summary>A change that loses what another installation filled is still offered, naming that installation.</summary>
    [Fact]
    public void AChangeThatLosesAnotherInstallationsBlocksNamesIt()
    {
        var screen = Built(out var system);
        Off(screen, Inner, Outer, Light, Cockpit);
        var whole = Assert.Single(Targets(system, 0));
        system.AddInstance().Fill(whole, new[] { Declared(screen, "Hangar Door") });
        Off(screen, Inner, Outer);
        Off(screen, Light, Cockpit);
        Panel(screen, system);

        Press(screen);

        var lines = screen.Editor.Question().Split('\n');
        var renamed = lines.Single(line => line.StartsWith("- Steps and triggers naming " + whole + " name ", StringComparison.Ordinal));
        var loses = Filled.With(screen.Editor.Texts.SlotOptimizeLoses, "installations", "Installation 2");
        Assert.EndsWith(" " + loses, renamed);
        Assert.Contains("- " + Filled.With(screen.Editor.Texts.SlotOptimizeRemoves, "slot", whole) + " " + loses, lines);
        Assert.True(screen.Editor.HasAnswer(screen.Editor.Texts.SlotOptimizeApply));
    }

    /// <summary>Where the slots are as few as they can be, it says so, and offers nothing to approve.</summary>
    [Fact]
    public void WithNothingToChangeItSaysSo()
    {
        var screen = Built(out var system);
        Off(screen, Light);
        Panel(screen, system);

        Press(screen);

        Assert.Equal(screen.Editor.Texts.SlotOptimizeNothing, screen.Editor.Question());
        Assert.False(screen.Editor.HasAnswer(screen.Editor.Texts.SlotOptimizeApply));
        screen.Editor.PressAnswer(screen.Editor.Texts.SlotOptimizeOk);
        screen.Run(2);
        Assert.Equal("", screen.Editor.Question());
    }

    /// <summary>A logic system with one step on four blocks, and two on its halves, on its installation panel.</summary>
    static Screen Whole(out AutomationSystem system, out string whole)
    {
        var screen = Built(out system);
        Off(screen, Inner, Outer, Light, Cockpit);
        whole = Assert.Single(Targets(system, 0));
        Off(screen, Inner, Outer);
        Off(screen, Light, Cockpit);
        Panel(screen, system);
        return screen;
    }


    /// <summary>A logic system with a sequence, for steps to be added to.</summary>
    static Screen Built(out AutomationSystem system)
    {
        var screen = Screen.On(Rig.Demo());
        screen.Editor.NewButtonSystem("Airlock");
        screen.Run(2);
        system = screen.Editor.Program.Systems[0];
        return screen;
    }

    /// <summary>Adds a step switching <paramref name="blocks" /> off, picked together.</summary>
    static void Off(Screen screen, params string[] blocks)
    {
        screen.Editor.PressAdd("task");
        screen.Run(2);
        screen.Editor.PressBlock(blocks[0]);
        for (var i = 1; i < blocks.Length; i++)
            screen.Editor.TickBlock(blocks[i]);

        screen.Editor.PressChoice("power");
        screen.Editor.Pick().Arguments!.Form.Fields[0].Text = "off";
        screen.Editor.PressPrimary();
        screen.Run(2);
    }

    /// <summary>Takes out the step at <paramref name="row" />.</summary>
    static void Remove(Screen screen, int row)
    {
        screen.Editor.StepRows()[row].Deletes!.Execute();
        screen.Run(2);
    }

    /// <summary>Leaves the sequence, keeping what was made, and opens the installation panel tab of <paramref name="system" />.</summary>
    static void Panel(Screen screen, AutomationSystem system)
    {
        screen.Editor.BackKeeping();
        screen.Run(2);
        screen.Editor.Push(new LogicSystemPage(screen.Editor, system));
        screen.Run(2);
        screen.Editor.PressTab(screen.Editor.Texts.SystemInstallationPanel);
        screen.Run(2);
    }

    /// <summary>The Optimize slots button on the logic system's installation panel tab, found by what it says.</summary>
    static Button Optimize(Screen screen) =>
        ((LogicSystemPage)screen.Editor.Current!).View.Within().OfType<Button>().Single(button => button.Text == screen.Editor.Texts.SlotOptimize);

    /// <summary>Presses Optimize slots.</summary>
    static void Press(Screen screen)
    {
        Optimize(screen).Command!.Execute();
        screen.Run(2);
    }

    /// <summary>What the act at <paramref name="index" /> of the first sequence, counting acts only, names.</summary>
    static List<string> Targets(AutomationSystem system, int index) =>
        system.Sequences[0].Steps.OfType<Act>().ElementAt(index).Targets.ToList();

    /// <summary>The variable the block called <paramref name="name" /> is declared as.</summary>
    static string Declared(Screen screen, string name) =>
        screen.Editor.Program.Declare(screen.Editor.World, screen.Editor.World.Entities.First(e => e.Name == name)).Name;
}
