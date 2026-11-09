using Lumel.Images;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Picking;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.World;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// Everything named is shown under the mark of its kind, one mark a row: the bolt for an action, the clock for one that is
/// waited for, "=?" for a value asked about, the pulse for a signal, the cube for a block, and three steps for a sequence.
/// </summary>
/// <remarks>The installation panel's two kinds of slot carry the same marks as the moments they are filled for.</remarks>
public class MarkTests
{
    /// <summary>Every signal is marked by the pulse, one of Sprocket's own as one sent from a toolbar slot.</summary>
    [Fact]
    public void TheTriggerPickerMarksEverySignalByThePulse()
    {
        var screen = OnTheSystem();

        screen.Editor.PressStart("when");
        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Run(2);
        Assert.Equal(Symbols.Signal, Entry(screen, "finishes closing").Mark);

        screen.Editor.PressBlock("Airlock Button");
        screen.Run(2);
        Assert.Equal(Symbols.Signal, Entry(screen, "button is pressed").Mark);
    }

    /// <summary>What a trigger may do is marked as what it is: a single action by the bolt, a new sequence and each sequence by the sequence's mark.</summary>
    [Fact]
    public void WhatATriggerDoesIsMarked()
    {
        var screen = OnTheSystem();
        var texts = screen.Editor.Texts;
        screen.Editor.DeveloperMode = true;
        screen.Run(1);

        screen.Editor.PressStart("when");
        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Editor.PressChoice("finishes closing");
        screen.Run(2);

        // The mark each row draws, from the first time the rows are filled.
        var rows = screen.Editor.Pick().Stages.Last().Entries();
        Assert.Equal(Symbols.Action, Drawn(rows.Single(row => row.Text == texts.PickSingleAction)));
        Assert.Equal(Symbols.Sequence, Drawn(rows.Single(row => row.Text == texts.PickNewSequence)));
        Assert.Equal(Symbols.Sequence, Drawn(rows.Single(row => row.Text == "Sequence 1")));
    }

    /// <summary>
    /// A mark says nothing of its own: an action a step waits for says so on its whole row, and a mark beside a member that
    /// does not wait carries no note at all.
    /// </summary>
    [Fact]
    public void WhatAMarkStandsForIsToldByTheRow()
    {
        var screen = OnTheSequence();

        screen.Editor.PressAdd("task");
        screen.Editor.PressBlock("Airlock Outer Door");
        screen.Run(2);

        var open = screen.Editor.Pick().Stages.Last().Entries().First(row => row.Mark == Symbols.Lasting);
        Assert.Equal(screen.Editor.Texts.Codex.MemberWaits, open.Tip!.Text);

        var marks = screen.Editor.Pick().Stages.Last().Entries().SelectMany(row => row.Within().OfType<Glyph>()).ToList();
        Assert.NotEmpty(marks);
        Assert.All(marks, mark => Assert.Null(mark.Tip));
    }

    /// <summary>What an if asks about a block is marked "=?", every one of it.</summary>
    [Fact]
    public void AnIfMarksWhatItAsks()
    {
        var screen = OnTheSequence();

        screen.Editor.PressAdd(SentenceText.IfWord);
        screen.Editor.PressBlock("Airlock Outer Door");
        screen.Run(2);

        var entries = screen.Editor.Pick().Stages.Last().Entries().Where(entry => !entry.IsHeading).ToList();
        Assert.NotEmpty(entries);
        Assert.All(entries, entry => Assert.Equal(Symbols.Value, entry.Mark));
    }

    /// <summary>A value carries its own mark when it is chosen to be set, as it does wherever a value is named, and so does the step that sets it.</summary>
    [Fact]
    public void SettingAValueIsMarkedAsAValue()
    {
        var screen = OnTheSequence();

        screen.Editor.PressAdd("task");
        screen.Editor.PressBlock("Airlock Light");
        screen.Run(2);

        Assert.Equal(Symbols.Value, Entry(screen, "radius").Mark);

        var light = screen.Editor.World.Entities.Single(entity => entity.Name == "Airlock Light");
        Assert.Equal(Symbols.Value, MarkText.Of(light.Kind.FindAction("Radius", ReachKind.Value)!));
        Assert.Equal(Symbols.Action, MarkText.Of(light.Kind.FindAction("OnOff_On", ReachKind.Action)!));
    }

    /// <summary>A trigger's row marks the signal that starts it and the sequence it runs.</summary>
    [Fact]
    public void ATriggerRowMarksItsSignalAndItsSequence()
    {
        var screen = OnTheSystem();

        var marks = screen.Editor.Triggers()[0].Within().OfType<MemberName>().Select(member => member.Mark).ToList();

        Assert.Contains(Symbols.Signal, marks);
        Assert.Contains(Symbols.Sequence, marks);
    }

    /// <summary>A logic system lists its slots in the order they were made, a block slot marked by the cube and a signal slot by the pulse.</summary>
    [Fact]
    public void TheSlotsStandInTheOrderTheyWereMade()
    {
        var screen = Slots();

        var rows = screen.Editor.SystemSlots();
        Assert.Equal(new[] { "signal slot 1", "block slot 1", "block slot 2" }, rows.Select(row => row.Name));
        Assert.Equal(new[] { Symbols.Signal, Symbols.Block, Symbols.Block }, rows.Select(row => row.Mark));
    }

    /// <summary>A slot renamed keeps its place, and its old name comes back there as the rename is undone.</summary>
    [Fact]
    public void ARenamedSlotKeepsItsPlace()
    {
        var screen = Slots();
        var editor = screen.Editor;

        editor.OpenSlot(editor.SlotOf("Airlock Outer Door"));
        screen.Run(5);
        editor.Screen.Heading[0].Renames!("Aft Door");
        screen.Run(5);
        editor.Back();
        screen.Run(5);
        editor.PressChoice(editor.Texts.PickKeepThem);
        screen.Run(5);

        Assert.Equal(new[] { "signal slot 1", "Aft Door", "block slot 2" }, editor.SystemSlots().Select(row => row.Name));

        editor.Screen.Undo!.Execute();
        screen.Run(5);

        Assert.Equal(new[] { "signal slot 1", "block slot 1", "block slot 2" }, editor.SystemSlots().Select(row => row.Name));
    }

    /// <summary>Going back from a slot's page after changing it asks whether to keep the changes, and discarding takes them all back.</summary>
    [Fact]
    public void DiscardingASlotsChangesTakesThemBack()
    {
        var screen = Slots();
        var editor = screen.Editor;

        editor.OpenSlot(editor.SlotOf("Airlock Outer Door"));
        screen.Run(5);
        editor.Screen.Heading[0].Renames!("Aft Door");
        screen.Run(5);
        editor.Back();
        screen.Run(5);
        editor.PressChoice(editor.Texts.PickDiscardThem);
        screen.Run(5);

        Assert.Equal(new[] { "signal slot 1", "block slot 1", "block slot 2" }, editor.SystemSlots().Select(row => row.Name));
    }

    /// <summary>Done on a slot's page keeps its changes without asking.</summary>
    [Fact]
    public void DoneKeepsASlotsChanges()
    {
        var screen = Slots();
        var editor = screen.Editor;

        editor.OpenSlot(editor.SlotOf("Airlock Outer Door"));
        screen.Run(5);
        editor.Screen.Heading[0].Renames!("Aft Door");
        screen.Run(5);
        editor.PressPrimary();
        screen.Run(5);

        Assert.False(screen.Scene.IsQuestioning, "Done asked whether to keep the changes");
        Assert.Equal(new[] { "signal slot 1", "Aft Door", "block slot 2" }, editor.SystemSlots().Select(row => row.Name));
    }

    /// <summary>The row of the what column offering <paramref name="text" />.</summary>
    static PickRow Entry(Screen screen, string text)
    {
        return screen.Editor.Pick().Stages.Last().Entries().Single(entry => entry.Text == text);
    }

    /// <summary>The mark <paramref name="row" />'s name draws beside it.</summary>
    static string Drawn(PickRow row) => row.Within().OfType<MemberName>().Single().Mark;

    /// <summary>A button system's own screen.</summary>
    static Screen OnTheSystem()
    {
        var screen = Screen.Fresh();
        screen.Editor.NewButtonSystem("Airlock");
        screen.Editor.Back();
        return screen.Run(2);
    }

    /// <summary>A button system's sequence, open.</summary>
    static Screen OnTheSequence()
    {
        var screen = Screen.Fresh();
        screen.Editor.NewButtonSystem("Airlock");
        return screen.Run(2);
    }

    /// <summary>The screen, switched off to be written, of a button system whose sequence closes both airlock doors, the outer one first.</summary>
    static Screen Slots()
    {
        var screen = OnTheSequence();
        var editor = screen.Editor;
        foreach (var door in new[] { "Airlock Outer Door", "Airlock Inner Door" })
        {
            editor.PressAdd("task");
            editor.PressBlock(door);
            editor.PressChoice("close");
            editor.PressPrimary();
            screen.Run(2);
        }

        // Leaving the sequence switches the logic system back on, and its panel is set up while it is off.
        editor.GoToRoot();
        screen.Run(5);
        editor.Program.Systems[0].Enabled = false;
        editor.Program.Edited();
        editor.OpenBlueprintSystem(0);
        screen.Run(5);
        return screen;
    }
}
