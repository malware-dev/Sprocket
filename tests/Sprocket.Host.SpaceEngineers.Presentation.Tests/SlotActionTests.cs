using Sprocket.Host.SpaceEngineers.Features.Blueprints;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// What a line does comes from the type of its slot, so a line keeps its action whatever the installation shown fills the slot
/// with, nothing included.
/// </summary>
public class SlotActionTests
{
    /// <summary>A new installation, shown with its slots still empty, leaves a step saying what it does.</summary>
    [Fact]
    public void AStepKeepsItsActionInAnEmptyInstallation()
    {
        var screen = Screen.On(Rig.Demo());
        var editor = screen.Editor;
        editor.NewButtonSystem("Airlock");
        screen.Run(2);
        editor.PressAdd("task");
        screen.Run(2);
        editor.PressBlock("Airlock Inner Door");
        editor.PressChoice("open");
        editor.PressPrimary();
        screen.Run(2);
        Assert.Equal("open", Action(screen));

        var system = editor.Program.Systems[0];
        editor.Another(system);
        screen.Run(2);

        Assert.Same(system.Instances[1], editor.StandIn(system));
        Assert.Equal("open", Action(screen));
    }

    /// <summary>A line's slot that the installation shown leaves empty is named in its tooltip, which says that nothing fills it there.</summary>
    [Fact]
    public void AnEmptySlotsTooltipSaysSo()
    {
        var screen = Screen.On(Rig.Demo());
        var editor = screen.Editor;
        editor.NewButtonSystem("Airlock");
        screen.Run(2);
        editor.PressAdd("task");
        screen.Run(2);
        editor.PressBlock("Airlock Inner Door");
        editor.PressChoice("open");
        editor.PressPrimary();
        screen.Run(2);
        var system = editor.Program.Systems[0];
        var slot = Assert.Single(editor.Program.Sequences[0].Steps.OfType<Sequences.Act>().First().Targets);

        editor.Another(system);
        screen.Run(2);

        var part = editor.StepRows()[0].Slots.OfType<EntitiesSlot>().Single();
        var tip = editor.StepRows()[0].ChipOf(part)!.Tip!.Text;
        Assert.Contains(slot, tip);
        Assert.Contains(editor.Texts.Codex.PartSlotEmpty, tip);
    }

    /// <summary>A step's one slot is renamed in the picker's heading, wherever its system names it, and the pick goes on with the new name.</summary>
    [Fact]
    public void TheOneSlotAPickComesToIsRenamedInTheHeading()
    {
        var screen = Screen.On(Rig.Demo());
        var editor = screen.Editor;
        editor.NewButtonSystem("Airlock");
        screen.Run(2);
        editor.PressAdd("task");
        screen.Run(2);
        editor.PressBlock("Airlock Inner Door");
        editor.PressChoice("open");
        editor.PressPrimary();
        screen.Run(2);
        var act = editor.Program.Sequences[0].Steps.OfType<Sequences.Act>().First();

        editor.StepRows()[0].Slots.OfType<EntitiesSlot>().Single().Opens!.Execute();
        screen.Run(3);
        editor.Screen.Heading[0].Renames!("Inner door");
        screen.Run(3);

        Assert.Equal("Inner door", editor.Screen.Heading[0].Text);
        Assert.Equal("Inner door", Assert.Single(act.Targets));

        editor.PressPrimary();
        screen.Run(3);
        var after = editor.Program.Sequences[0].Steps.OfType<Sequences.Act>().First();
        Assert.Equal("Inner door", Assert.Single(after.Targets));
        Assert.Equal(new[] { "slot 1", "Inner door" }, editor.Program.Systems[0].Parts.Select(part => part.Id).ToArray());
    }

    /// <summary>A type of block takes a slot only where it offers what the slot's steps do with it, which is named where it does not.</summary>
    [Fact]
    public void ATypeMustOfferWhatTheSlotIsUsedFor()
    {
        var screen = Screen.On(Rig.Demo());
        var editor = screen.Editor;
        editor.NewButtonSystem("Airlock");
        screen.Run(2);
        editor.PressAdd("task");
        screen.Run(2);
        editor.PressBlock("Airlock Inner Door");
        editor.PressChoice("open");
        editor.PressPrimary();
        screen.Run(2);

        var system = editor.Program.Systems[0];
        var slot = system.FindPart(editor.SlotOf("Airlock Inner Door"))!;
        var uses = new SlotUses(editor);

        Assert.Null(uses.Missing(system, slot, "Door"));
        Assert.Equal("open", uses.Missing(system, slot, "InteriorLight"));
        Assert.Equal("open", uses.Missing(system, slot, ""));
    }

    /// <summary>The words the first step's action part says.</summary>
    static string Action(Screen screen)
    {
        var row = screen.Editor.StepRows()[0];
        return SentenceText.InStep(screen.Editor.World, row.Slots.OfType<ActionSlot>().Single());
    }
}
