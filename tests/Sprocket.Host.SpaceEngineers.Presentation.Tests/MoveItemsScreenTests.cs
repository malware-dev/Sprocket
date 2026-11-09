using Sprocket.Host.SpaceEngineers.Blocks;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Features.Picking;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>Moving items, as a person sets it up on the selector.</summary>
public class MoveItemsScreenTests
{
    static Screen Picking(out Rig rig, params string[] slots)
    {
        rig = Rig.Demo();
        rig.Refinery(40, "Refinery 1");
        rig.Cargo(41, "Cargo 1");
        var screen = Screen.On(rig);
        screen.Editor.NewButtonSystem("Stock");
        screen.Run(2);
        foreach (var slot in slots)
            screen.Editor.Program.Systems[0].PartFor(slot).Holds = "ext.inventory";

        screen.Editor.PressAdd("task");
        screen.Editor.PressBlock("Refinery 1");
        screen.Run(2);
        screen.Editor.PressChoice("move items");
        screen.Run(2);
        return screen;
    }

    /// <summary>Selects the cargo container as where the items go, on the selector the option opens.</summary>
    static void SelectTarget(Screen screen)
    {
        ((SlotsField)screen.Editor.Pick().Arguments!.Form.Fields[0]).Select();
        screen.Run(2);
        screen.Editor.PressBlock("Cargo 1");
        screen.Run(2);
        screen.Editor.PressPrimary();
        screen.Run(2);
    }

    /// <summary>The step asks where the items go, with a button that selects them.</summary>
    [Fact]
    public void TheStepAsksWhereTheItemsGo()
    {
        var screen = Picking(out _);

        var form = screen.Editor.Pick().Arguments!.Form;
        Assert.IsType<SlotsField>(form.Fields[0]);
        Assert.Contains("Select", screen.Dump);
    }

    /// <summary>An amount left empty says in its box that all of it moves.</summary>
    [Fact]
    public void TheAmountSaysAllWhileEmpty()
    {
        var screen = Picking(out _);

        Assert.Contains("\"all\"", screen.Dump);
    }

    /// <summary>Left with no item and no amount, the line says it moves all items.</summary>
    [Fact]
    public void NoItemReadsAsAllItems()
    {
        var screen = Picking(out _);
        SelectTarget(screen);
        screen.Editor.PressPrimary();
        screen.Run(4);

        var said = string.Join(" ", System.Text.RegularExpressions.Regex.Matches(screen.Dump, "\"([^\"]*)\"").Cast<System.Text.RegularExpressions.Match>().Select(m => m.Groups[1].Value));
        Assert.Contains("move all items from", said);
    }

    /// <summary>Selecting a block makes a slot holding its inventory, named in the step's argument, with the step waited for.</summary>
    [Fact]
    public void SelectingABlockMakesTheSlotTheItemsGoTo()
    {
        var screen = Picking(out _);

        SelectTarget(screen);
        screen.Editor.PressPrimary();
        screen.Run(4);

        var system = screen.Editor.Program.Systems[0];
        var steps = system.Sequences[0].Steps;
        var at = steps.ToList().FindIndex(step => (step as Act)?.ActionId == InventoryType.MoveItems);
        Assert.True(at >= 0, screen.Dump);
        var act = (Act)steps[at];
        Assert.Equal(ValueKind.Slots, act.Arguments[0].Kind);
        var made = act.Arguments[0].Names[0];
        Assert.Equal("ext.inventory", system.FindPart(made)!.Holds);
        Assert.Equal("Cargo 1", system.First!.FilledWith(made)![0]);
        Assert.IsType<Await>(steps[at + 1]);
        Assert.Contains(made, screen.Dump);
    }

    /// <summary>Undoing the step takes the slot made for it out with it, and redoing brings both back.</summary>
    [Fact]
    public void UndoingTheStepTakesItsNewSlotOut()
    {
        var screen = Picking(out _);
        SelectTarget(screen);
        screen.Editor.PressPrimary();
        screen.Run(4);
        var system = screen.Editor.Program.Systems[0];
        var made = ((Act)system.Sequences[0].Steps.First(step => (step as Act)?.ActionId == InventoryType.MoveItems)).Arguments[0].Names[0];
        Assert.NotNull(system.FindPart(made));

        screen.Editor.Undo(system, system.Sequences[0]);
        screen.Run(2);

        Assert.Null(system.FindPart(made));
    }

    /// <summary>Several slots can be given, and the step names them all.</summary>
    [Fact]
    public void SeveralSlotsCanBeGiven()
    {
        var screen = Picking(out _, "first bin", "second bin");
        var system = screen.Editor.Program.Systems[0];

        screen.Editor.Pick().Arguments!.Form.Fields[0].Text = "first bin, second bin";
        screen.Editor.PressPrimary();
        screen.Run(4);

        var act = (Act)system.Sequences[0].Steps.First(step => (step as Act)?.ActionId == InventoryType.MoveItems);
        Assert.Equal(new[] { "first bin", "second bin" }, act.Arguments[0].Names);
    }

    /// <summary>A trigger's own task selects its target as a step does, on a selector that names the logic system's slots.</summary>
    [Fact]
    public void AnySelectorNamingSlotsSelectsATarget()
    {
        var rig = Rig.Demo();
        rig.Refinery(40, "Refinery 1");
        rig.Cargo(41, "Cargo 1");
        var screen = Screen.On(rig);
        screen.Editor.NewButtonSystem("Stock");
        screen.Run(2);
        var system = screen.Editor.Program.Systems[0];
        PickAnswer? answered = null;
        screen.Editor.Push(new PickPage(screen.Editor, "A task on blocks", false, new List<long>(), "", ReachKind.Native, answer => answered = answer)
        {
            Slotted = system
        });
        screen.Run(2);
        screen.Editor.PressBlock("Refinery 1");
        screen.Run(2);
        screen.Editor.PressChoice("move items");
        screen.Run(2);

        SelectTarget(screen);
        screen.Editor.PressPrimary();
        screen.Run(2);

        Assert.NotNull(answered);
        Assert.Equal(ValueKind.Slots, answered!.Arguments[0].Kind);
        Assert.StartsWith("inventory slot", answered.Arguments[0].Names[0]);
    }

    /// <summary>Selecting a target and then backing out of the step leaves no slot behind.</summary>
    [Fact]
    public void BackingOutLeavesNoSlot()
    {
        var screen = Picking(out _);
        var system = screen.Editor.Program.Systems[0];
        SelectTarget(screen);

        screen.Editor.Pop();
        screen.Run(2);
        screen.Editor.PressAdd("task");
        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Run(2);
        screen.Editor.PressChoice("open");
        screen.Editor.PressPrimary();
        screen.Run(4);

        Assert.DoesNotContain(system.Parts, part => part.Holds == "ext.inventory");
    }

    /// <summary>The target shows the blocks selected until the step is finished, when the slot is made for them.</summary>
    [Fact]
    public void TheTargetShowsItsBlocks()
    {
        var screen = Picking(out _);

        SelectTarget(screen);

        Assert.Equal("Cargo 1", screen.Editor.Pick().Arguments!.Form.Fields[0].Text);
    }

    /// <summary>The source's slot takes the first number and the target's the next, in the order the line reads them.</summary>
    [Fact]
    public void TheSlotsNumberInTheOrderTheLineReads()
    {
        var screen = Picking(out _);
        SelectTarget(screen);
        screen.Editor.PressPrimary();
        screen.Run(4);

        var system = screen.Editor.Program.Systems[0];
        var act = (Act)system.Sequences[0].Steps.First(step => (step as Act)?.ActionId == InventoryType.MoveItems);
        Assert.Equal("inventory slot 1", act.Targets[0]);
        Assert.Equal("inventory slot 2", act.Arguments[0].Names[0]);
    }

    /// <summary>A finished step and its target part, with a second container to change it to.</summary>
    static EntitiesSlot Finished(out Screen screen, out AutomationSystem system)
    {
        screen = Picking(out var rig);
        rig.Cargo(42, "Cargo 2");
        screen.Run(2);
        SelectTarget(screen);
        screen.Editor.PressPrimary();
        screen.Run(4);
        system = screen.Editor.Program.Systems[0];
        return Target(screen);
    }

    static EntitiesSlot Target(Screen screen) => screen.Editor.StepRows()[0].Slots.OfType<EntitiesSlot>().Single(slot => slot.IsOption);

    /// <summary>The target is a part of the line of its own, named as the source is, under its mark, with "to" a word between them.</summary>
    [Fact]
    public void TheTargetIsAPartOfItsOwn()
    {
        var target = Finished(out var screen, out _);

        var source = screen.Editor.StepRows()[0].Slots.OfType<EntitiesSlot>().Single(slot => !slot.IsOption);
        Assert.Equal(new[] { "inventory slot 2" }, target.Slots);
        Assert.Equal(new[] { Symbols.Inventory }, target.Marks);
        Assert.Equal(new[] { Symbols.Inventory }, source.Marks);
        Assert.Equal("from", source.Joined);
        Assert.Equal("to", target.Joined);
    }

    /// <summary>The selector of the target is headed by the slot it makes, under the mark of what it will hold and numbered after the source.</summary>
    [Fact]
    public void TheTargetSelectorIsHeadedByTheSlotItMakes()
    {
        var screen = Picking(out _);
        ((SlotsField)screen.Editor.Pick().Arguments!.Form.Fields[0]).Select();
        screen.Run(2);
        screen.Editor.PressBlock("Cargo 1");
        screen.Run(2);

        var heading = screen.Editor.Screen.Heading;
        Assert.Equal(Symbols.Inventory, heading[heading.Count - 1].Mark);
        Assert.Equal("inventory slot 2", screen.Editor.Screen.Title());
    }

    /// <summary>Pressing the target opens its selector on what it holds, as pressing the source does; nothing changed, nothing is made.</summary>
    [Fact]
    public void PressingTheTargetOpensItsSelector()
    {
        var target = Finished(out var screen, out var system);
        var slots = system.Parts.Count;

        target.Opens!.Execute();
        screen.Run(3);

        Assert.Equal("inventory slot 2", screen.Editor.Screen.Title());
        Assert.Contains(screen.Editor.Pick().Stages[0].Entries(), entry => entry.Text == "Cargo 1" && entry.IsChosen);

        screen.Editor.PressPrimary();
        screen.Run(4);

        Assert.Equal("Sequence 1", screen.Editor.Screen.Title());
        Assert.Equal(slots, system.Parts.Count);
        Assert.Equal(new[] { "inventory slot 2" }, Target(screen).Slots);
    }

    /// <summary>Selecting other blocks on the target changes them in one press of Done, as on the source.</summary>
    [Fact]
    public void TheTargetChangesInOnePress()
    {
        var target = Finished(out var screen, out var system);

        target.Opens!.Execute();
        screen.Run(3);
        screen.Editor.PressBlock("Cargo 1");
        screen.Editor.PressBlock("Cargo 2");
        screen.Run(2);
        screen.Editor.PressPrimary();
        screen.Run(4);

        Assert.Equal("Sequence 1", screen.Editor.Screen.Title());
        Assert.Equal("Cargo 2", system.First!.FilledWith(Target(screen).Slots[0])![0]);
    }

    /// <summary>A slot whose name holds a comma stays one slot when its step is opened and finished.</summary>
    [Fact]
    public void ACommaInASlotsNameKeepsItOneSlot()
    {
        var screen = Picking(out _, "bay, left");
        screen.Editor.Pick().Arguments!.Form.Fields[0].Text = "bay, left";

        screen.Editor.PressPrimary();
        screen.Run(4);

        Assert.Equal(new[] { "bay, left" }, Target(screen).Slots);
    }

    /// <summary>Back from the target's selector goes straight back to the line, with the step as it was.</summary>
    [Fact]
    public void BackFromTheTargetsSelectorReturnsToTheLine()
    {
        var target = Finished(out var screen, out _);

        target.Opens!.Execute();
        screen.Run(3);
        screen.Editor.Back();
        screen.Run(4);

        Assert.Equal("Sequence 1", screen.Editor.Screen.Title());
        Assert.Equal(new[] { "inventory slot 2" }, Target(screen).Slots);
    }

    /// <summary>A trigger's task names its target the same way, and pressing it opens the same selector.</summary>
    [Fact]
    public void ATriggersTaskNamesItsTargetTheSameWay()
    {
        var rig = Rig.Demo();
        rig.Refinery(40, "Refinery 1");
        rig.Cargo(41, "Cargo 1");
        var screen = Screen.On(rig);
                screen.Editor.NewButtonSystem("Stock");
        screen.Editor.Back();
        screen.Run(2);
        screen.Editor.PressStart("every");
        screen.Editor.DeveloperMode = true;
        screen.Run(1);
        screen.Editor.PressChoice("A task on blocks…");
        screen.Editor.PressBlock("Refinery 1");
        screen.Run(2);
        screen.Editor.PressChoice("move items");
        screen.Run(2);
        SelectTarget(screen);
        screen.Editor.PressPrimary();
        screen.Run(4);

        var row = screen.Editor.Triggers().Last();
        var target = row.Does.OfType<EntitiesSlot>().Single(slot => slot.IsOption);
        Assert.Equal("to", target.Joined);
        Assert.Equal(new[] { Symbols.Inventory }, target.Marks);

        target.Opens!.Execute();
        screen.Run(3);
        Assert.Equal(target.Slots[0], screen.Editor.Screen.Title());

        screen.Editor.Back();
        screen.Run(4);
        Assert.Equal("Stock", screen.Editor.Screen.Title());
    }

    /// <summary>The line reads as a sentence: what moves, from where, to where.</summary>
    [Fact]
    public void TheStepReadsAsASentence()
    {
        var screen = Picking(out _);
        var form = screen.Editor.Pick().Arguments!.Form;
        SelectTarget(screen);
        form.Fields[1].Text = "Ingot/Iron";
        form.Fields[2].Text = "100";
        screen.Editor.PressPrimary();
        screen.Run(4);

        var said = string.Join(" ", System.Text.RegularExpressions.Regex.Matches(screen.Dump, "\"([^\"]*)\"").Cast<System.Text.RegularExpressions.Match>().Select(m => m.Groups[1].Value));
        Assert.Contains("move 100 kg Ingot/Iron from", said);
        Assert.Contains(" to ", said);
    }
}
