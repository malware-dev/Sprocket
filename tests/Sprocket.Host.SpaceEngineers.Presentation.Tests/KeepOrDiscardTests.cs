using Lumel.Controls;
using Sprocket.Host.SpaceEngineers.Features.LogicSystems;
using Sprocket.Host.SpaceEngineers.Features.Sequences;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// Every screen with a history asks, on the way back, whether to keep what was changed on it: keeping goes back with the
/// changes, discarding takes them back first. What was changed on a screen opened from it was answered for there.
/// </summary>
public class KeepOrDiscardTests
{
    /// <summary>Slots moved on the logic system's screen are asked about on the way back, and discarding puts them back.</summary>
    [Fact]
    public void DiscardingOnTheLogicSystemTakesItsChangesBack()
    {
        var screen = Panel();
        var editor = screen.Editor;
        var system = editor.Program.Systems[0];
        Add(screen);

        editor.Back();
        screen.Run(2);
        Assert.Equal(editor.Texts.PickKeepQuestion, editor.Question());

        editor.PressChoice(editor.Texts.PickDiscardThem);
        screen.Run(5);
        Assert.Empty(system.Categories);
    }

    /// <summary>Keeping on the logic system's screen keeps its changes and goes back, asking nothing more.</summary>
    [Fact]
    public void KeepingOnTheLogicSystemGoesBack()
    {
        var screen = Panel();
        var editor = screen.Editor;
        Add(screen);

        editor.Back();
        screen.Run(2);
        editor.PressChoice(editor.Texts.PickKeepThem);
        screen.Run(2);

        Assert.Single(editor.Program.Systems[0].Categories);
        Assert.False(editor.Screen.HasQuestion, "going back asked something more");
        Assert.False(editor.Screen.CanGoBack, "keeping did not go back");
    }

    /// <summary>Done keeps what was changed without asking.</summary>
    [Fact]
    public void DoneKeepsWithoutAsking()
    {
        var screen = Panel();
        var editor = screen.Editor;
        Add(screen);

        editor.PressPrimary();
        screen.Run(2);

        Assert.NotEqual(editor.Texts.PickKeepQuestion, editor.Question());
        Assert.Single(editor.Program.Systems[0].Categories);
    }

    /// <summary>A step deleted on a sequence's screen is asked about on the way back, and discarding brings it back.</summary>
    [Fact]
    public void DiscardingOnASequenceBringsTheStepBack()
    {
        var screen = Panel();
        var editor = screen.Editor;
        var sequence = editor.Program.Systems[0].Sequences[0];
        editor.Push(new SequencePage(editor, sequence));
        screen.Run(5);

        var steps = sequence.Steps.Count;
        editor.StepRows()[0].Deletes!.Execute();
        screen.Run(5);
        Assert.Equal(steps - 1, sequence.Steps.Count);

        editor.Back();
        screen.Run(2);
        editor.PressChoice(editor.Texts.PickDiscardThem);
        screen.Run(5);

        Assert.Equal(steps, sequence.Steps.Count);
        Assert.IsType<LogicSystemPage>(editor.Current);
    }

    /// <summary>What was kept on a slot's page is not asked about again on the logic system's screen it was opened from.</summary>
    [Fact]
    public void WhatASlotsPageKeptIsNotAskedAgain()
    {
        var screen = Panel();
        var editor = screen.Editor;
        editor.OpenSlot("Airlock Outer Door");
        screen.Run(5);
        editor.Screen.Heading[0].Renames!("Aft Door");
        screen.Run(5);
        editor.BackKeeping();
        screen.Run(5);
        Assert.IsType<LogicSystemPage>(editor.Current);

        editor.Back();
        screen.Run(2);

        Assert.NotEqual(editor.Texts.PickKeepQuestion, editor.Question());
        Assert.NotNull(editor.Program.Systems[0].FindPart("Aft Door"));
    }

    /// <summary>The logic system's screen, switched off, of a button system whose sequence closes both airlock doors, on its installation panel.</summary>
    static Screen Panel()
    {
        var screen = Screen.Fresh();
        var editor = screen.Editor;
        editor.NewButtonSystem("Airlock");
        screen.Run(2);
        foreach (var door in new[] { "Airlock Outer Door", "Airlock Inner Door" })
        {
            editor.PressAdd("task");
            editor.PressBlock(door);
            editor.PressChoice("close");
            editor.PressPrimary();
            screen.Run(2);
        }

        editor.GoToRoot();
        screen.Run(5);
        editor.Program.Systems[0].Enabled = false;
        editor.Program.Edited();
        editor.OpenBlueprintSystem(0);
        screen.Run(5);
        editor.PressTab(editor.Texts.SystemInstallationPanel);
        return screen.Run(5);
    }

    /// <summary>Adds a category on the installation panel.</summary>
    static void Add(Screen screen)
    {
        var page = (LogicSystemPage)screen.Editor.Current!;
        page.View.Within().OfType<Button>().Single(button => button.Text.EndsWith(screen.Editor.Texts.CategoryAdd)).Command!.Execute();
        screen.Run(5);
    }
}
