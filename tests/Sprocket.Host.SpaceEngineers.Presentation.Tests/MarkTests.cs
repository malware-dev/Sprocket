using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Picking;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// Everything named is shown under the mark of its kind, one mark a row: the bolt for an action, the clock for one that is
/// waited for, "=?" for a value asked about, the pulse for a signal, the cube for a block, and three steps for a sequence.
/// </summary>
/// <remarks>The configuration panel's two kinds of slot carry the same marks as the moments they are filled for.</remarks>
public class MarkTests
{
    /// <summary>A moment watched on a block is marked by the cube, and a signal a block sends by the pulse.</summary>
    [Fact]
    public void TheTriggerPickerMarksSignalsAndMoments()
    {
        var screen = OnTheSystem();

        screen.Editor.PressStart("when");
        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Run(2);
        Assert.Equal(Symbols.Block, Entry(screen, "finishes closing").Mark);

        screen.Editor.PressBlock("Airlock Button");
        screen.Run(2);
        Assert.Equal(Symbols.Signal, Entry(screen, "button 1 is pressed").Mark);
    }

    /// <summary>What a trigger may do is marked as what it is: a single action by the bolt, a new sequence and each sequence by the sequence's mark.</summary>
    [Fact]
    public void WhatATriggerDoesIsMarked()
    {
        var screen = OnTheSystem();
        var texts = screen.Editor.Texts;

        screen.Editor.PressStart("when");
        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Editor.PressChoice("finishes closing");
        screen.Run(2);

        var rows = screen.Editor.Pick().Stages.Last().Entries();
        Assert.Equal(Symbols.Action, rows.Single(row => row.Text == texts.PickSingleAction).Mark);
        Assert.Equal(Symbols.Sequence, rows.Single(row => row.Text == texts.PickNewSequence).Mark);
        Assert.Equal(Symbols.Sequence, rows.Single(row => row.Text == "Sequence 1").Mark);
    }

    /// <summary>What an if asks about a block is marked "=?", every one of it.</summary>
    [Fact]
    public void AnIfMarksWhatItAsks()
    {
        var screen = OnTheSequence();

        screen.Editor.PressAdd(screen.Editor.Texts.SequenceIf);
        screen.Editor.PressBlock("Airlock Outer Door");
        screen.Run(2);

        var entries = screen.Editor.Pick().Stages.Last().Entries().Where(entry => !entry.IsHeading).ToList();
        Assert.NotEmpty(entries);
        Assert.All(entries, entry => Assert.Equal(Symbols.Value, entry.Mark));
    }

    /// <summary>Setting a value is something a block is told to do, and is marked as one.</summary>
    [Fact]
    public void SettingAValueIsMarkedAsAnAction()
    {
        var screen = OnTheSequence();

        screen.Editor.PressAdd("task");
        screen.Editor.PressBlock("Airlock Light");
        screen.Run(2);

        Assert.Equal(Symbols.Action, Entry(screen, "Set Radius").Mark);
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

    /// <summary>The configuration panel lists its block slots, marked by the cube, before its signal slots, marked by the pulse, each in order of name.</summary>
    [Fact]
    public void ThePanelListsBlockSlotsThenSignalSlotsByName()
    {
        var screen = Panel();

        var rows = screen.Editor.Parts();
        Assert.Equal(new[] { "Airlock Inner Door", "Airlock Outer Door", "Airlock Button" }, rows.Select(row => row.NameBox.Text));
        Assert.Equal(new[] { Symbols.Block, Symbols.Block, Symbols.Signal }, rows.Select(row => row.Mark));
    }

    /// <summary>A slot renamed moves to its place in the order of names, and back again as the rename is undone.</summary>
    [Fact]
    public void ARenamedSlotMovesToItsPlace()
    {
        var screen = Panel();
        var editor = screen.Editor;

        editor.Input.Focus(editor.Parts().Single(row => row.NameBox.Text == "Airlock Outer Door").NameBox);
        screen.Do("clear", "type", "Aft Door").Run(1);
        editor.Input.Focus(null);
        screen.Run(5);

        Assert.Equal(new[] { "Aft Door", "Airlock Inner Door", "Airlock Button" }, editor.Parts().Select(row => row.NameBox.Text));

        editor.Screen.Undo!.Execute();
        screen.Run(5);

        Assert.Equal(new[] { "Airlock Inner Door", "Airlock Outer Door", "Airlock Button" }, editor.Parts().Select(row => row.NameBox.Text));
    }

    /// <summary>The row of the what column offering <paramref name="text" />.</summary>
    static PickRow Entry(Screen screen, string text)
    {
        return screen.Editor.Pick().Stages.Last().Entries().Single(entry => entry.Text == text);
    }

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

    /// <summary>The configuration panel, being written, of a button system whose sequence closes both airlock doors, the outer one first.</summary>
    static Screen Panel()
    {
        var screen = OnTheSequence();
        var editor = screen.Editor;
        foreach (var door in new[] { "Airlock Outer Door", "Airlock Inner Door" })
        {
            editor.PressAdd("task");
            editor.PressBlock(door);
            editor.PressChoice("Close");
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
        editor.EditPanel();
        screen.Run(5);
        return screen;
    }
}
