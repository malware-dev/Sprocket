using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Features.Terminal;
using Sprocket.Sim.Machine;
using VRage.Game.ModAPI;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A toolbar a person presses is one line in the picker, its page and slot chosen on the slots that open under it.</summary>
public class ToolbarSlotTests
{
    /// <summary>A cockpit's hotbar is one line, not a line for every slot of every page.</summary>
    [Fact]
    public void AHotbarIsOneLine()
    {
        var sim = Picking("Cockpit");

        Assert.True(sim.Editor.HasChoice("hotbar slot is pressed"));
        Assert.False(sim.Editor.HasChoice("hotbar page 1, slot 1 is pressed"));
        Assert.Null(sim.Editor.Slots());
    }

    /// <summary>Chosen, it opens its slots on the first one holding nothing, and the heading names that slot.</summary>
    [Fact]
    public void ChosenItOpensOnTheFirstFreeSlot()
    {
        var sim = Picking("Cockpit", seat => seat.Toolbar.Changing()
            .Set(0, MyToolbarItemInfo.BlockAction(10, "OnOff"))
            .Set(1, MyToolbarItemInfo.BlockAction(1, "Open"))
            .Apply());

        sim.Editor.PressChoice("hotbar slot is pressed");
        sim.Tick();

        var slots = sim.Editor.Slots()!;
        Assert.Equal("Page 1, slot 3: free", slots.Line);
    }

    /// <summary>A slot holding the game's own action says which block and action, and carries that block's picture.</summary>
    [Fact]
    public void ASlotSaysWhatItHolds()
    {
        var sim = Picking("Cockpit", seat => seat.Toolbar.Changing().Set(0, MyToolbarItemInfo.BlockAction(10, "OnOff")).Apply());

        sim.Editor.PressChoice("hotbar slot is pressed");
        sim.Tick();

        var held = sim.Editor.Slots()!.HeldAt(1, 1);
        Assert.True(held.Taken);
        Assert.Equal("Landing Lights: Toggle block On/Off", held.Says);
        Assert.StartsWith("MyObjectBuilder_InteriorLight/", held.Picture);
        Assert.False(sim.Editor.Slots()!.HeldAt(1, 2).Holds);
    }

    /// <summary>A slot on another page is chosen there, and the trigger made names it.</summary>
    [Fact]
    public void ASlotOnAnotherPageIsTheOneWritten()
    {
        var sim = Picking("Cockpit");

        sim.Editor.PressChoice("hotbar slot is pressed");
        sim.Tick();
        var slots = sim.Editor.Slots()!;
        slots.Turn(2);
        slots.Press(4);
        sim.Tick();

        Assert.Equal(2, sim.Editor.Slots()!.Page);
        Assert.Equal("Page 2, slot 4: free", sim.Editor.Slots()!.Line);

        sim.Editor.PressPrimary();
        Settle(sim);
        sim.Editor.Back();
        Settle(sim);

        Assert.Equal("When slot 1 hotbar page 2, slot 4 is pressed", sim.Editor.Says(0));
    }

    /// <summary>Opened again, the trigger's picker opens on its toolbar's line with the slot it was given.</summary>
    [Fact]
    public void OpenedAgainItOpensOnItsSlot()
    {
        var sim = Picking("Cockpit");
        sim.Editor.PressChoice("hotbar page 3, slot 7 is pressed");
        sim.Tick();
        sim.Editor.PressPrimary();
        Settle(sim);
        sim.Editor.Back();
        Settle(sim);

        sim.Editor.PressDoes(0);
        sim.Tick();

        Assert.Equal(3, sim.Editor.Slots()!.Page);
        Assert.StartsWith("Page 3, slot 7: ", sim.Editor.Slots()!.Line);
    }

    /// <summary>A slot a trigger of the automation already answers is marked as answering, saying what it starts.</summary>
    [Fact]
    public void ASlotATriggerAnswersSaysWhichOne()
    {
        var sim = Picking("Cockpit");
        sim.Editor.PressChoice("hotbar page 1, slot 1 is pressed");
        sim.Tick();
        sim.Editor.PressPrimary();
        Settle(sim);
        Assert.True(Seat(sim).Toolbar.TryGetItem(0, out var written) && SignalAction.IsOurs(written), "the slot was not written");

        sim.Editor.Back();
        Settle(sim);
        sim.Editor.PressStart(SentenceText.WhenWord);
        sim.Editor.PressBlock("Cockpit");
        sim.Editor.PressChoice("hotbar slot is pressed");
        sim.Tick();

        var held = sim.Editor.Slots()!.HeldAt(1, 1);
        Assert.True(held.Answering);
        Assert.Equal("Used by Installation 1 of Airlock: Sequence 1", held.Says);
        Assert.Equal("Page 1, slot 2: free", sim.Editor.Slots()!.Line);
    }

    /// <summary>A slot answering a trigger on one block picked and holding the game's own action on another answers, and says both.</summary>
    [Fact]
    public void ASlotAnsweringOnAnyBlockPickedAnswers()
    {
        var sim = Picking("Cockpit");
        sim.Editor.PressChoice("hotbar page 1, slot 1 is pressed");
        sim.Tick();
        sim.Editor.PressPrimary();
        Settle(sim);
        sim.Editor.Back();
        Settle(sim);

        var copilot = sim.Machine.Cockpit(24, "Copilot Seat");
        copilot.Toolbar.Changing().Set(0, MyToolbarItemInfo.BlockAction(10, "OnOff")).Apply();
        Settle(sim);
        sim.Editor.PressStart(SentenceText.WhenWord);
        sim.Editor.PressBlock("Cockpit");
        sim.Editor.TickBlock("Copilot Seat");
        sim.Editor.PressChoice("hotbar slot is pressed");
        sim.Tick();

        var held = sim.Editor.Slots()!.HeldAt(1, 1);
        Assert.True(held.Answering);
        Assert.Equal(new[] { "Cockpit: Used by Installation 1 of Airlock: Sequence 1", "Copilot Seat: Landing Lights: Toggle block On/Off" },
            held.Lines);
    }

    /// <summary>With several blocks picked, a slot holding something on any of them is taken, said with the block it is on.</summary>
    [Fact]
    public void ASlotTakenOnAnyBlockPickedIsTaken()
    {
        var sim = Simulation.Fresh();
        var copilot = sim.Machine.Cockpit(24, "Copilot Seat");
        copilot.Toolbar.Changing().Set(0, MyToolbarItemInfo.BlockAction(10, "OnOff")).Apply();
        sim.Tick();
        sim.Editor.NewSystem("Airlock");
        sim.Editor.PressStart(SentenceText.WhenWord);
        sim.Editor.PressBlock("Cockpit");
        sim.Editor.TickBlock("Copilot Seat");
        sim.Tick();

        sim.Editor.PressChoice("hotbar slot is pressed");
        sim.Tick();

        var held = sim.Editor.Slots()!.HeldAt(1, 1);
        Assert.True(held.Taken);
        Assert.Equal("Copilot Seat: Landing Lights: Toggle block On/Off", held.Says);
        Assert.Equal("Page 1, slot 2: free", sim.Editor.Slots()!.Line);
    }

    /// <summary>
    /// A slot holding something on several blocks picked says what it holds on each, the line under the slots names the
    /// first and how many more, and it answers a trigger where it does on any of them.
    /// </summary>
    [Fact]
    public void ASlotTakenOnSeveralBlocksSaysEach()
    {
        var sim = Simulation.Fresh();
        var copilot = sim.Machine.Cockpit(24, "Copilot Seat");
        copilot.Toolbar.Changing().Set(0, MyToolbarItemInfo.BlockAction(10, "OnOff")).Apply();
        Seat(sim).Toolbar.Changing().Set(0, MyToolbarItemInfo.BlockAction(1, "Open")).Apply();
        sim.Tick();
        sim.Editor.NewSystem("Airlock");
        sim.Editor.PressStart(SentenceText.WhenWord);
        sim.Editor.PressBlock("Cockpit");
        sim.Editor.TickBlock("Copilot Seat");
        sim.Editor.PressChoice("hotbar slot is pressed");
        sim.Tick();

        var slots = sim.Editor.Slots()!;
        slots.Press(1);
        sim.Tick();

        var held = sim.Editor.Slots()!.HeldAt(1, 1);
        Assert.Equal(new[] { "Cockpit: Hangar Door: Open/Closed", "Copilot Seat: Landing Lights: Toggle block On/Off" }, held.Lines);
        Assert.Equal("Page 1, slot 1: Cockpit: Hangar Door: Open/Closed and 1 more", sim.Editor.Slots()!.Line);
    }

    /// <summary>A button panel is one line too, its buttons the slots of its one page.</summary>
    [Fact]
    public void AButtonPanelIsOneLine()
    {
        var sim = Picking("Airlock Button");

        Assert.True(sim.Editor.HasChoice("button is pressed"));
        Assert.False(sim.Editor.HasChoice("button 1 is pressed"));

        sim.Editor.PressChoice("button is pressed");
        sim.Tick();

        Assert.Equal("Slot 1: free", sim.Editor.Slots()!.Line);
    }

    /// <summary>A toolbar whose slots are things that happen keeps a line of its own words for them, with no slots under it.</summary>
    [Fact]
    public void ATimerKeepsItsOwnWords()
    {
        var sim = Picking("Hold Timer", made: rig => rig.Timer(30, "Hold Timer"));

        Assert.True(sim.Editor.HasChoice("goes off"));

        sim.Editor.PressChoice("goes off");
        sim.Tick();
        Assert.Null(sim.Editor.Slots());
    }

    /// <summary>The slots slide open over a few ticks, and go when another line is chosen.</summary>
    [Fact]
    public void TheSlotsSlideOpenAndGoWithTheirLine()
    {
        // On a screen, since the slots open as they are drawn.
        var screen = Screen.Fresh();
        screen.Editor.NewSystem("Airlock");
        screen.Editor.PressStart(SentenceText.WhenWord);
        screen.Editor.PressBlock("Cockpit");
        screen.Run(1);

        screen.Editor.PressChoice("hotbar slot is pressed");
        screen.Run(2);
        Assert.True(screen.Editor.Slots()!.Progress < 1f, "the slots were open at once");

        screen.Run(15);
        Assert.Equal(1f, screen.Editor.Slots()!.Progress);

        screen.Editor.PressChoice("becomes occupied");
        screen.Run(1);
        Assert.Null(screen.Editor.Slots());
    }

    static Simulation Picking(string block, Action<Seat>? hotbar = null, Action<Rig>? made = null)
    {
        var sim = Simulation.Fresh();
        hotbar?.Invoke(Seat(sim));
        made?.Invoke(sim.Machine);
        sim.Tick();

        sim.Editor.NewSystem("Airlock");
        sim.Editor.PressStart(SentenceText.WhenWord);
        sim.Editor.PressBlock(block);
        sim.Tick();
        return sim;
    }

    static Seat Seat(Simulation sim) => sim.Machine.Fitted.OfType<Seat>().Single(seat => seat.Name == "Cockpit");

    static void Settle(Simulation sim)
    {
        for (var i = 0; i < 15; i++)
            sim.Tick();
    }
}
