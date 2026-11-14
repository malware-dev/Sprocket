using Sprocket.Host.SpaceEngineers.Features.Selector;
using Sprocket.Host.SpaceEngineers.Features.Selector.Toolbar;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// The picker's heading is the slot the pick names: the new slot's name, given in place, or the names of the slots the picks
/// fill already, in italics; and a slot already made can be picked by its name.
/// </summary>
public class SlotHeadingTests
{
    const string Inner = "Airlock Inner Door";
    const string Outer = "Airlock Outer Door";

    /// <summary>Blocks no slot holds make a new slot, named in the heading by the next numbered name, with that name in hand to be given.</summary>
    [Fact]
    public void ANewSlotIsNamedInTheHeading()
    {
        var screen = Built();
        var editor = screen.Editor;
        editor.PressAdd("task");
        screen.Run(2);
        editor.PressBlock(Inner);
        screen.Run(2);

        var heading = Assert.Single(editor.Screen.Heading);
        Assert.Equal("block slot 1", heading.Text);
        Assert.True(heading.CanRename);
        Assert.False(heading.Emphasized);
        Assert.True(heading.NamedOnArrival);
    }

    /// <summary>A plain logic system's picker is headed by the picker's own name, with no slot to name and nothing in italics.</summary>
    [Fact]
    public void APlainSystemsPickerHasNoSlotPlate()
    {
        var screen = Built();
        var editor = screen.Editor;
        editor.Program.Systems[0].IsBlueprint = false;
        editor.PressAdd("task");
        screen.Run(2);
        var title = Assert.Single(editor.Screen.Heading).Text;
        editor.PressBlock(Inner);
        screen.Run(2);

        var heading = Assert.Single(editor.Screen.Heading);
        Assert.Equal(title, heading.Text);
        Assert.NotEqual("slot 2", heading.Text);
        Assert.False(heading.CanRename);
        Assert.False(heading.Emphasized);
    }

    /// <summary>The name given in the heading is the name of the slot the step makes.</summary>
    [Fact]
    public void TheNameGivenIsTheSlots()
    {
        var screen = Built();
        var editor = screen.Editor;
        editor.PressAdd("task");
        screen.Run(2);
        editor.PressBlock(Inner);
        editor.Name("inner");
        editor.PressChoice("open");
        editor.PressPrimary();
        screen.Run(2);

        Assert.Equal("inner", Assert.Single(Act(screen, 0).Targets));
        Assert.NotNull(editor.Program.Systems[0].FindPart("inner"));
    }

    /// <summary>A name another slot, a state or a type has, in any case, is refused in the heading.</summary>
    [Fact]
    public void ANameTakenInAnyCaseIsRefused()
    {
        var screen = Built();
        var editor = screen.Editor;
        editor.Program.Systems[0].States.Add(new State("Cycling", "", Literal.Of(false)));
        editor.PressAdd("task");
        screen.Run(2);
        editor.PressBlock(Inner);
        screen.Run(2);

        var heading = editor.Screen.Heading[0];
        Assert.False(heading.Takes("SIGNAL SLOT 1"));
        Assert.False(heading.Takes("cycling"));
        Assert.True(heading.Takes("inner"));
    }

    /// <summary>Blocks a slot holds already show that slot's name in italics, which cannot be renamed there.</summary>
    [Fact]
    public void MatchedSlotsShowInItalics()
    {
        var screen = Built();
        var editor = screen.Editor;
        AddOpen(screen, Inner);
        AddOpen(screen, Outer);
        var inner = Assert.Single(Act(screen, 0).Targets);
        var outer = Assert.Single(Act(screen, 1).Targets);

        editor.PressAdd("task");
        screen.Run(2);
        editor.PressBlock(Inner);
        editor.TickBlock(Outer);
        screen.Run(2);

        var heading = editor.Screen.Heading[0];
        Assert.True(heading.Emphasized);
        Assert.False(heading.CanRename);
        Assert.Equal($"{inner} and {outer}", heading.Text);
    }

    /// <summary>Picking something no slot holds again brings back the name as it was left.</summary>
    [Fact]
    public void TheNameComesBackAsItWasLeft()
    {
        var screen = Built();
        var editor = screen.Editor;
        AddOpen(screen, Inner);
        var inner = Assert.Single(Act(screen, 0).Targets);

        editor.PressAdd("task");
        screen.Run(2);
        editor.PressBlock(Outer);
        editor.Name("outer");
        screen.Run(2);
        editor.PressBlock(Inner);
        screen.Run(2);
        Assert.Equal(inner, editor.Screen.Heading[0].Text);

        editor.PressBlock(Outer);
        screen.Run(2);
        Assert.Equal("outer", editor.Screen.Heading[0].Text);
        Assert.False(editor.Screen.Heading[0].Emphasized);
    }

    /// <summary>Other blocks picked for a step that is all that uses its slot name that slot, which keeps its name until it is renamed there.</summary>
    [Fact]
    public void AStepsOwnSlotKeepsItsNameInTheHeading()
    {
        var screen = Built();
        var editor = screen.Editor;
        AddOpen(screen, Inner);
        var inner = Assert.Single(Act(screen, 0).Targets);

        editor.PressStepSlot(0, 1);
        screen.Run(2);
        editor.PressBlock(Outer);
        screen.Run(2);

        var heading = editor.Screen.Heading[0];
        Assert.Equal(inner, heading.Text);
        Assert.False(heading.Emphasized);
        Assert.True(heading.CanRename);
    }

    /// <summary>A slot already made is picked by its name, shows in italics, and is the slot the step names.</summary>
    [Fact]
    public void AnExistingSlotIsPickedByName()
    {
        var screen = Built();
        var editor = screen.Editor;
        AddOpen(screen, Inner);
        var inner = Assert.Single(Act(screen, 0).Targets);
        var parts = editor.Program.Systems[0].Parts.Count;

        editor.PressAdd("task");
        screen.Run(2);
        ToSlots(screen);
        editor.PressBlock(inner);
        screen.Run(2);
        Assert.True(editor.Screen.Heading[0].Emphasized);
        Assert.Equal(inner, editor.Screen.Heading[0].Text);
        var held = Assert.Single(Target.Within(editor.Pick().Stages[0]).OfType<SlotHeldRow>());
        Assert.Contains(Target.Within(held).OfType<SlotChip>(), chip => chip.Text == Inner);

        editor.PressChoice("close");
        editor.PressPrimary();
        screen.Run(2);

        Assert.Equal(inner, Assert.Single(Act(screen, 1).Targets));
        Assert.Equal(parts, editor.Program.Systems[0].Parts.Count);
        Assert.Equal("close", SentenceText.InStep(editor.World, editor.StepRows()[2].Slots.OfType<ActionSlot>().Single()));
    }

    /// <summary>A slot named by a line says how it is read whatever it holds: any in, or all in.</summary>
    [Fact]
    public void ALineSaysHowItReadsItsSlot()
    {
        var screen = Built();
        var editor = screen.Editor;
        editor.PressAdd("if");
        screen.Run(2);
        editor.PressBlock(Inner);
        screen.Run(2);
        editor.Pick().Stages[1].Entries().First(entry => entry.Text == "open" && !entry.IsHeading).Pick(HeldModifier.Alone, 1);
        screen.Run(2);
        editor.Typed("is");
        screen.Run(2);

        var row = editor.StepRows()[0];
        var slot = Assert.Single(((If)editor.Program.Sequences[0].Steps[0]).Compared.Targets);
        Assert.Equal(new[] { "any in", slot, "is", "open" }, row.Slots.Select(part => SentenceText.InStep(editor.World, part)));
    }

    /// <summary>A name given in the heading stays as the picks go on changing, and is the name of the slot the step makes.</summary>
    [Fact]
    public void ANameGivenStaysAsThePicksChange()
    {
        var screen = Built();
        var editor = screen.Editor;
        editor.PressAdd("task");
        screen.Run(2);
        editor.PressBlock(Inner);
        editor.Name("doors");
        editor.TickBlock(Outer);
        screen.Run(2);
        Assert.Equal("doors", editor.Screen.Heading[0].Text);

        editor.PressChoice("open");
        editor.PressPrimary();
        screen.Run(2);

        var system = editor.Program.Systems[0];
        Assert.Equal("doors", Assert.Single(Act(screen, 0).Targets));
        Assert.Equal(2, system.First!.FilledWith("doors")!.Count);
    }

    /// <summary>A slot of a type no block on the construct is, empty in the installation shown, can be picked by name, but offers nothing to do with it, so Done waits.</summary>
    [Fact]
    public void AnEmptySlotOfATypeNoBlockIsOffersNothing()
    {
        var screen = Built();
        var editor = screen.Editor;
        editor.Program.Systems[0].PartFor("lonely").Expects = "MotorStator";
        editor.Program.Edited();
        screen.Run(2);

        editor.PressAdd("task");
        screen.Run(2);
        ToSlots(screen);
        editor.PressBlock("lonely");
        screen.Run(2);

        Assert.Equal("lonely", editor.Screen.Heading[0].Text);
        Assert.Single(editor.Pick().Stages);
        Assert.False(editor.Current!.CanPrimary, "Done offered with nothing to do");
    }

    /// <summary>A button system, on its sequence, for steps to be added to.</summary>
    static Screen Built()
    {
        var screen = Screen.On(Rig.Demo());
        screen.Editor.NewButtonSystem("Airlock");
        screen.Run(2);
        return screen;
    }

    /// <summary>Adds a step opening <paramref name="door" />.</summary>
    static void AddOpen(Screen screen, string door)
    {
        screen.Editor.PressAdd("task");
        screen.Run(2);
        screen.Editor.PressBlock(door);
        screen.Editor.PressChoice("open");
        screen.Editor.PressPrimary();
        screen.Run(2);
    }

    /// <summary>The act at <paramref name="index" />, counting acts only.</summary>
    static Act Act(Screen screen, int index) =>
        screen.Editor.Program.Sequences[0].Steps.OfType<Act>().ElementAt(index);

    /// <summary>A signal slot picked by name is the whole of what starts a trigger: nothing more is asked of it, and Done makes the trigger.</summary>
    [Fact]
    public void ASignalSlotPickedByNameStartsATrigger()
    {
        var screen = Built();
        var editor = screen.Editor;
        var system = editor.Program.Systems[0];
        var slot = ((SignalTrigger)system.Triggers[0]).Part;
        editor.BackKeeping();
        screen.Run(2);

        editor.PressStart(SentenceText.WhenWord);
        screen.Run(2);
        ToSlots(screen);
        editor.PressBlock(slot);
        screen.Run(2);

        Assert.DoesNotContain(editor.Pick().Stages, stage => stage.Title == editor.Texts.PickWhatHappens);
        Assert.True(editor.Current!.CanPrimary, "Done waits for more than the signal slot");
        editor.PressPrimary();
        screen.Run(2);

        Assert.Equal(2, system.Triggers.Count);
        Assert.Equal(slot, ((SignalTrigger)system.Triggers[1]).Part);
    }

    /// <summary>Switches the selector showing, on blocks, to the slots already made, through its Source box.</summary>
    static void ToSlots(Screen screen)
    {
        screen.Editor.ChooseSource(screen.Editor.Texts.PickSourceSlots);
        screen.Run(1);
        Assert.True(((PickPage)screen.Editor.Current!).ListsSlots);
    }
}
