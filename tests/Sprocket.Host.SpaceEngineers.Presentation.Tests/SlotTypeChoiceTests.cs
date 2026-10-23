using Sprocket.Host.SpaceEngineers.Features.Blueprints;
using Sprocket.Host.SpaceEngineers.Features.Picking;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// A block slot's type is chosen from its page, in a picker listing only the types the slot can take: wider or narrower, down
/// to one kind of block, and each one offering what the slot is used for. Narrowing asks first.
/// </summary>
public class SlotTypeChoiceTests
{
    /// <summary>The types offered are those the slot's steps can still act through, the slot's own among them.</summary>
    [Fact]
    public void OnlyTypesOfferingWhatTheSlotDoesAreListed()
    {
        var screen = OnTheDoorsSlot(out _, out var part);
        var page = Open(screen);

        Assert.Contains("Door", page.Offered);
        Assert.Contains(page.Offered, type => type.StartsWith("Door/", StringComparison.Ordinal));
        Assert.DoesNotContain("InteriorLight", page.Offered);
        Assert.DoesNotContain("", page.Offered);
        Assert.Equal(TypeText.Of(screen.Editor.Texts, part.Expects), page.Strip.Stages[0].Entries().Single(entry => entry.IsChosen).Text);
    }

    /// <summary>Narrowed to one kind of block, the slot asks first, then takes it as one step back, and another installation's door that no longer fits is marked to fill again.</summary>
    [Fact]
    public void NarrowingAsksAndMarksWhatNoLongerFits()
    {
        var screen = OnTheDoorsSlot(out var system, out var part);
        var other = system.AddInstance();
        other.Fill(part.Id, new[] { screen.Editor.Program.Declare(screen.Editor.World, screen.Editor.World.Entities.First(e => e.Name == "Hangar Door")).Name });
        var was = part.Expects;
        var page = Open(screen);
        var exact = page.Offered.First(type => type.StartsWith("Door/", StringComparison.Ordinal));
        var at = page.Offered.IndexOf(exact);

        page.Strip.Stages[0].Entries()[at].Pick(HeldModifier.Alone, 1);
        screen.Run(2);
        screen.Editor.PressPrimary();
        screen.Run(2);
        Assert.True(screen.Editor.Screen.HasQuestion, "narrowing asked nothing");
        screen.Editor.PressAnswer(screen.Editor.Texts.SlotNarrowYes);
        screen.Run(2);

        Assert.Equal(exact, part.Expects);
        var used = new List<string>();
        system.Acted(used);
        Assert.Equal(SlotMark.Changed, SlotMarks.Of(screen.Editor.Runner.Bindings, system, other, part, used));

        screen.Editor.Screen.Undo!.Execute();
        screen.Run(2);
        Assert.Equal(was, part.Expects);
    }

    /// <summary>While its logic system may run, a slot's type is shown and cannot be pressed, its note saying why.</summary>
    [Fact]
    public void TheTypeWaitsForTheSwitch()
    {
        var screen = OnTheDoorsSlot(out var system, out _);
        system.Enabled = true;
        screen.Editor.Program.Edited();
        screen.Editor.Invalidate();
        screen.Run(2);

        var expects = screen.Editor.Slot().Expects;
        Assert.True(expects.IsVisible);
        Assert.Null(expects.Command);
        Assert.Equal(Lumel.Text.Template.Fill(screen.Editor.Texts.SystemLocked, "name", system.Name), expects.Tip!.Text);
    }

    /// <summary>A signal slot takes no type of block, so its page offers none to choose.</summary>
    [Fact]
    public void ASignalSlotHasNoTypeToChoose()
    {
        var screen = OnTheDoorsSlot(out var system, out _);
        screen.Editor.BackKeeping();
        screen.Run(2);
        screen.Editor.OpenSlot(((SignalTrigger)system.Triggers[0]).Part);
        screen.Run(2);

        Assert.False(screen.Editor.Slot().Expects.IsVisible);
    }

    /// <summary>The page of the slot of a step opening the inner door, its logic system switched off so it can be written.</summary>
    static Screen OnTheDoorsSlot(out AutomationSystem system, out Part part)
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

        system = editor.Program.Systems[0];
        var slot = editor.SlotOf("Airlock Inner Door");
        part = system.FindPart(slot)!;
        editor.GoToRoot();
        screen.Run(5);
        system.Enabled = false;
        editor.Program.Edited();
        editor.OpenBlueprintSystem(0);
        screen.Run(5);
        editor.OpenSlot(slot);
        screen.Run(5);
        return screen;
    }

    /// <summary>Presses the slot's type on its page, opening the picker of types.</summary>
    static SlotTypePage Open(Screen screen)
    {
        screen.Editor.Slot().Expects.Command!.Execute();
        screen.Run(2);
        return Assert.IsType<SlotTypePage>(screen.Editor.Current);
    }
}
