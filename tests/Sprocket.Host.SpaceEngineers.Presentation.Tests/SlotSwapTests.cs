using Sprocket.Host.SpaceEngineers.Features.Slots;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// Trading a step's blocks changes its slot where nothing else uses it: the slot keeps its name, its description and what the
/// other installations filled it with, and its type follows the blocks. Where something else uses it, the person is asked
/// whether to change it for everyone or give the step a slot of its own; and blocks another slot holds name that slot.
/// </summary>
public class SlotSwapTests
{
    /// <summary>The inner door traded for the outer one leaves the slot as it was named and described, holding the outer door.</summary>
    [Fact]
    public void ADoorTradedForADoorKeepsItsSlot()
    {
        var screen = OpeningTheInnerDoor(out var system, out var slot);
        slot.Annotation = "The door on the ship's side.";
        var other = system.AddInstance();
        other.Fill(slot.Id, new[] { "elsewhere" });

        Repick(screen, "Airlock Outer Door");

        var act = Assert.IsType<Act>(screen.Editor.Program.Sequences[0].Steps[0]);
        Assert.Equal(slot.Id, Assert.Single(act.Targets));
        Assert.Same(slot, system.FindPart(slot.Id));
        Assert.Equal("The door on the ship's side.", slot.Annotation);
        Assert.Equal(new[] { "elsewhere" }, other.FilledWith(slot.Id));
        Assert.Equal(new[] { "Airlock Outer Door" }, screen.Editor.Seen(system, act.Targets).Select(Shown(screen)));
    }

    /// <summary>Taking the trade back gives the slot its old block back.</summary>
    [Fact]
    public void UndoingTheTradeGivesTheSlotItsBlockBack()
    {
        var screen = OpeningTheInnerDoor(out var system, out var slot);
        var act = Assert.IsType<Act>(screen.Editor.Program.Sequences[0].Steps[0]);
        Repick(screen, "Airlock Outer Door");

        screen.Editor.Screen.Undo!.Execute();
        screen.Run(2);

        Assert.Equal(slot.Id, Assert.Single(act.Targets));
        Assert.Equal(new[] { "Airlock Inner Door" }, screen.Editor.Seen(system, act.Targets).Select(Shown(screen)));
    }

    /// <summary>A block of another kind changes the slot's type to follow it once asked, as the type no longer takes what it took, and another installation whose block no longer fits is marked to fill it again.</summary>
    [Fact]
    public void ABlockOfAnotherKindChangesTheSlotsType()
    {
        var screen = OpeningTheInnerDoor(out var system, out var slot);
        var was = slot.Expects;
        var other = system.AddInstance();
        other.Fill(slot.Id, new[] { Declared(screen, "Airlock Outer Door") });

        Repick(screen, "Hangar Door");
        Assert.True(screen.Editor.Screen.HasQuestion, "a type that takes less was not asked about");
        screen.Editor.PressAnswer(screen.Editor.Texts.SlotNarrowYes);
        screen.Run(2);

        var act = Assert.IsType<Act>(screen.Editor.Program.Sequences[0].Steps[0]);
        Assert.Equal(slot.Id, Assert.Single(act.Targets));
        Assert.NotEqual(was, slot.Expects);
        var used = new List<string>();
        system.Acted(used);
        Assert.Equal(SlotMark.Changed, SlotMarks.Of(screen.Editor.Runner.Bindings, other, slot));
    }

    /// <summary>The same block picked again names the slot it fills, and makes none.</summary>
    [Fact]
    public void TheSameBlockAgainNamesItsSlot()
    {
        var screen = OpeningTheInnerDoor(out var system, out var slot);
        var parts = system.Parts.Count;

        Repick(screen, "Airlock Inner Door");

        var act = Assert.IsType<Act>(screen.Editor.Program.Sequences[0].Steps[0]);
        Assert.Equal(slot.Id, Assert.Single(act.Targets));
        Assert.Equal(parts, system.Parts.Count);
    }

    /// <summary>Cancelled, the question about a type that takes less leaves the slot and the step as they were, and the picker open.</summary>
    [Fact]
    public void CancellingANarrowingLeavesTheSlot()
    {
        var screen = OpeningTheInnerDoor(out _, out var slot);
        var was = slot.Expects;

        Repick(screen, "Hangar Door");
        screen.Editor.PressAnswer(screen.Editor.Texts.SlotNarrowCancel);
        screen.Run(2);

        Assert.Equal(was, slot.Expects);
        Assert.IsType<Features.Selector.PickPage>(screen.Editor.Current);
        var act = Assert.IsType<Act>(screen.Editor.Program.Sequences[0].Steps[0]);
        Assert.Equal(new[] { "Airlock Inner Door" }, screen.Editor.Seen(screen.Editor.Program.Systems[0], act.Targets).Select(Shown(screen)));
    }

    /// <summary>Blocks another slot holds name that slot, and the slot the step had is kept.</summary>
    [Fact]
    public void BlocksAnotherSlotHoldsNameIt()
    {
        var screen = OpeningTheInnerDoor(out var system, out var slot);
        AddOpen(screen, "Airlock Outer Door");
        var outer = Assert.Single(screen.Editor.Program.Sequences[0].Steps.OfType<Act>().ElementAt(1).Targets);

        Repick(screen, "Airlock Outer Door");

        var act = screen.Editor.Program.Sequences[0].Steps.OfType<Act>().First();
        Assert.Equal(outer, Assert.Single(act.Targets));
        Assert.NotNull(system.FindPart(slot.Id));
        Assert.Equal(new[] { "Airlock Inner Door" }, screen.Editor.Seen(system, new[] { slot.Id }).Select(Shown(screen)));
    }

    /// <summary>A slot another step also names is changed only once asked: given a slot of its own, the step names a new one and the other keeps the slot.</summary>
    [Fact]
    public void ASharedSlotAsksAndCanBeLeftToTheOthers()
    {
        var screen = OpeningTheInnerDoor(out var system, out var slot);
        AddOpen(screen, "Airlock Inner Door");

        Repick(screen, "Airlock Outer Door");
        Assert.True(screen.Editor.Screen.HasQuestion, "nothing asked about a slot another step uses");
        screen.Editor.PressAnswer(screen.Editor.Texts.SlotSharedOwn);
        screen.Run(2);

        var steps = screen.Editor.Program.Sequences[0].Steps.OfType<Act>().ToList();
        Assert.NotEqual(slot.Id, Assert.Single(steps[0].Targets));
        Assert.Equal(new[] { "Airlock Outer Door" }, screen.Editor.Seen(system, steps[0].Targets).Select(Shown(screen)));
        Assert.Equal(slot.Id, Assert.Single(steps[1].Targets));
        Assert.Equal(new[] { "Airlock Inner Door" }, screen.Editor.Seen(system, new[] { slot.Id }).Select(Shown(screen)));
    }

    /// <summary>The question about a shared slot put away unanswered changes nothing, and leaves the picker open.</summary>
    [Fact]
    public void ASharedSlotQuestionPutAwayChangesNothing()
    {
        var screen = OpeningTheInnerDoor(out var system, out var slot);
        AddOpen(screen, "Airlock Inner Door");
        var parts = system.Parts.Count;

        Repick(screen, "Airlock Outer Door");
        screen.Do("escape");
        screen.Run(2);

        Assert.False(screen.Editor.Screen.HasQuestion, "the question is still up");
        Assert.IsType<Features.Selector.PickPage>(screen.Editor.Current);
        Assert.Equal(parts, system.Parts.Count);
        var steps = screen.Editor.Program.Sequences[0].Steps.OfType<Act>().ToList();
        Assert.Equal(slot.Id, Assert.Single(steps[0].Targets));
        Assert.Equal(new[] { "Airlock Inner Door" }, screen.Editor.Seen(system, new[] { slot.Id }).Select(Shown(screen)));
    }

    /// <summary>Answered for everyone, the shared slot takes the new block for every step that names it.</summary>
    [Fact]
    public void ASharedSlotCanBeChangedForEveryone()
    {
        var screen = OpeningTheInnerDoor(out var system, out var slot);
        AddOpen(screen, "Airlock Inner Door");

        Repick(screen, "Airlock Outer Door");
        screen.Editor.PressAnswer(screen.Editor.Texts.SlotSharedEveryone);
        screen.Run(2);

        var steps = screen.Editor.Program.Sequences[0].Steps.OfType<Act>().ToList();
        Assert.Equal(slot.Id, Assert.Single(steps[0].Targets));
        Assert.Equal(slot.Id, Assert.Single(steps[1].Targets));
        Assert.Equal(new[] { "Airlock Outer Door" }, screen.Editor.Seen(system, new[] { slot.Id }).Select(Shown(screen)));
    }

    /// <summary>A logic system whose one step opens the inner door, with that step's slot.</summary>
    static Screen OpeningTheInnerDoor(out AutomationSystem system, out Part slot)
    {
        var screen = Screen.On(Rig.Demo());
        screen.Editor.NewButtonSystem("Airlock");
        screen.Run(2);
        AddOpen(screen, "Airlock Inner Door");

        system = screen.Editor.Program.Systems[0];
        var act = Assert.IsType<Act>(screen.Editor.Program.Sequences[0].Steps[0]);
        slot = system.FindPart(Assert.Single(act.Targets))!;
        Assert.NotNull(slot);
        return screen;
    }

    static void AddOpen(Screen screen, string door)
    {
        screen.Editor.PressAdd("task");
        screen.Run(2);
        screen.Editor.PressBlock(door);
        screen.Editor.PressChoice("open");
        screen.Editor.PressPrimary();
        screen.Run(2);
    }

    /// <summary>Opens the first step's blocks and picks <paramref name="door" /> alone in their place.</summary>
    static void Repick(Screen screen, string door)
    {
        screen.Editor.PressStepSlot(0, 1);
        screen.Run(2);
        screen.Editor.PressBlock(door);
        screen.Editor.PressPrimary();
        screen.Run(2);
    }

    /// <summary>The variable the block called <paramref name="name" /> is declared as.</summary>
    static string Declared(Screen screen, string name) =>
        screen.Editor.Program.Declare(screen.Editor.World, screen.Editor.World.Entities.First(e => e.Name == name)).Name;

    /// <summary>What a declared block is called on the construct.</summary>
    static Func<string, string> Shown(Screen screen) =>
        declared => screen.Editor.World.Entities.First(e => screen.Editor.Program.Declare(screen.Editor.World, e).Name == declared).Name;
}
