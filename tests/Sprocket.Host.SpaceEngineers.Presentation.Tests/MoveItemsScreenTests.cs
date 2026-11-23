using Sprocket.Host.SpaceEngineers.Features.Selector;
using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
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
            screen.Editor.Program.Systems[0].PartFor(slot).Holds = "sprocket.inventory";

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
        Assert.Equal("sprocket.inventory", system.FindPart(made)!.Holds);
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

    /// <summary>A selector that names the logic system's slots selects a target as a step does.</summary>
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
        screen.Editor.Push(new PickPage(screen.Editor, "A task on blocks", false, new List<long>(), default(MemberKey), ReachKind.Native, answer => answered = answer)
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

        Assert.DoesNotContain(system.Parts, part => part.Holds == "sprocket.inventory");
    }

    /// <summary>The target shows the blocks selected until the step is finished, when the slot is made for them.</summary>
    [Fact]
    public void TheTargetShowsItsBlocks()
    {
        var screen = Picking(out _);

        SelectTarget(screen);

        Assert.Equal("Cargo 1", screen.Editor.Pick().Arguments!.Form.Fields[0].Text);
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

    /// <summary>The target is a part of the line of its own, named as the source is, under its mark, with "to" a word between them; the source's slot takes the first number and the target's the next, in the order the line reads them.</summary>
    [Fact]
    public void TheTargetIsAPartOfItsOwn()
    {
        var target = Finished(out var screen, out var system);

        var act = (Act)system.Sequences[0].Steps.First(step => (step as Act)?.ActionId == InventoryType.MoveItems);
        Assert.Equal("inventory slot 1", act.Targets[0]);
        Assert.Equal("inventory slot 2", act.Arguments[0].Names[0]);

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
        screen.Run(1);
        screen.Editor.ChooseSource(screen.Editor.Texts.PickSourceBlocks);
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

    /// <summary>A slot whose name holds a comma stays one slot on its button, and when its step is finished.</summary>
    [Fact]
    public void ACommaInASlotsNameKeepsItOneSlot()
    {
        var screen = Picking(out _, "bay, left");
        var field = (SlotsField)screen.Editor.Pick().Arguments!.Form.Fields[0];
        field.Text = "bay, left";

        Assert.Equal("bay, left", field.Within().OfType<SlotChip>().Single().Text);

        screen.Editor.PressPrimary();
        screen.Run(4);

        Assert.Equal(new[] { "bay, left" }, Target(screen).Slots);
    }

    /// <summary>Back from the target's selector, or from the source's opened on its blocks and left with nothing changed, goes straight back to the line without asking, with the step as it was.</summary>
    [Fact]
    public void BackFromAStepsSelectorsReturnsToTheLine()
    {
        var target = Finished(out var screen, out _);

        target.Opens!.Execute();
        screen.Run(1);
        screen.Editor.ChooseSource(screen.Editor.Texts.PickSourceBlocks);
        screen.Run(3);
        screen.Editor.Back();
        screen.Run(4);

        Assert.Equal("Sequence 1", screen.Editor.Screen.Title());
        Assert.Equal(new[] { "inventory slot 2" }, Target(screen).Slots);

        screen.Editor.StepRows()[0].Slots.OfType<EntitiesSlot>().Single(slot => !slot.IsOption).Opens!.Execute();
        screen.Run(1);
        screen.Editor.ChooseSource(screen.Editor.Texts.PickSourceBlocks);
        screen.Run(3);
        screen.Editor.Back();
        screen.Run(4);

        Assert.Equal("Sequence 1", screen.Editor.Screen.Title());
        Assert.Equal(new[] { "inventory slot 2" }, Target(screen).Slots);
    }

    /// <summary>The line reads as a sentence: what moves, from where, to where, and with no item and no amount it says it moves all items.</summary>
    [Theory]
    [InlineData(null, null, "move all items from")]
    [InlineData("Ingot/Iron", "100", "move 100 kg Ingot/Iron from")]
    public void TheStepReadsAsASentence(string? item, string? amount, string phrase)
    {
        var screen = Picking(out _);
        var form = screen.Editor.Pick().Arguments!.Form;
        SelectTarget(screen);
        if (item != null)
            form.Fields[1].Text = item;
        if (amount != null)
            form.Fields[2].Text = amount;
        screen.Editor.PressPrimary();
        screen.Run(4);

        var said = string.Join(" ", System.Text.RegularExpressions.Regex.Matches(screen.Dump, "\"([^\"]*)\"").Cast<System.Text.RegularExpressions.Match>().Select(m => m.Groups[1].Value));
        Assert.Contains(phrase, said);
        Assert.Contains(" to ", said);
    }

    /// <summary>An if on a slot holding a container's inventory offers its amount, fill and mass; the container picked itself offers amount once.</summary>
    [Fact]
    public void AnInventorySlotOffersAmountFillAndMass()
    {
        var screen = Picking(out _);
        SelectTarget(screen);
        screen.Editor.PressPrimary();
        screen.Run(4);
        var editor = screen.Editor;
        var system = editor.Program.Systems[0];
        var made = ((Act)system.Sequences[0].Steps.First(step => (step as Act)?.ActionId == InventoryType.MoveItems)).Arguments[0].Names[0];

        editor.PressAdd("if", editor.Texts.PickSourceSlots);
        screen.Run(2);
        editor.PressBlock(made);
        screen.Run(2);
        var offered = editor.Members();
        Assert.True(new[] { "amount", "fill", "mass" }.All(offered.Contains), string.Join(", ", offered));

        editor.Back();
        screen.Run(2);
        editor.PressAdd("if");
        screen.Run(2);
        editor.PressBlock("Cargo 1");
        screen.Run(2);
        offered = editor.Members();
        Assert.Equal(1, offered.Count(text => text == "amount"));
    }
}
