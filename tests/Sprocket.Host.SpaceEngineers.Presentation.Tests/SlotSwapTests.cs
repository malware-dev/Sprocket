using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// Trading a step's one block for another of the same kind keeps the slot: its name, its description and what the other
/// installations filled it with. Anything else picked makes a slot of its own, as picking always has.
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

    /// <summary>A block of another kind than the slot expects is a slot of its own.</summary>
    [Fact]
    public void ABlockOfAnotherKindMakesASlotOfItsOwn()
    {
        var screen = OpeningTheInnerDoor(out _, out var slot);

        screen.Editor.PressStepSlot(0, 1);
        screen.Run(2);
        screen.Editor.PressBlock("Hangar Door");
        screen.Editor.PressPrimary();
        screen.Run(2);

        var act = Assert.IsType<Act>(screen.Editor.Program.Sequences[0].Steps[0]);
        Assert.NotEqual(slot.Id, Assert.Single(act.Targets));
    }

    /// <summary>A slot another step still names keeps its block, and the new block gets a slot of its own.</summary>
    [Fact]
    public void ASlotStillNamedElsewhereKeepsItsBlock()
    {
        var screen = OpeningTheInnerDoor(out var system, out var slot);
        AddOpen(screen, "Airlock Inner Door");

        Repick(screen, "Airlock Outer Door");

        var steps = screen.Editor.Program.Sequences[0].Steps.OfType<Act>().ToList();
        Assert.NotEqual(slot.Id, Assert.Single(steps[0].Targets));
        Assert.Equal(new[] { "Airlock Inner Door" }, screen.Editor.Seen(system, new[] { slot.Id }).Select(Shown(screen)));
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

    /// <summary>What a declared block is called on the construct.</summary>
    static Func<string, string> Shown(Screen screen) =>
        declared => screen.Editor.World.Entities.First(e => screen.Editor.Program.Declare(screen.Editor.World, e).Name == declared).Name;
}
