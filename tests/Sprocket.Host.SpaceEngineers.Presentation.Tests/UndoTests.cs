using Sprocket.Host.SpaceEngineers.Features.LogicSystems;
using Sprocket.Host.SpaceEngineers.Features.Sequences;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// One way back for each logic system, whichever of its screens a change was made on, taken in the order the changes were
/// made, under the switch they were made under, and showing where each one was.
/// </summary>
public class UndoTests
{
    /// <summary>A step added on a sequence is taken back from its logic system's screen, which goes to the sequence to show it.</summary>
    [Fact]
    public void AStepIsTakenBackFromTheLogicSystemsScreen()
    {
        var screen = Built();
        var editor = screen.Editor;
        Light(screen);
        editor.Back();
        screen.Run(2);
        Assert.IsType<LogicSystemPage>(editor.Current);

        Assert.Equal("adding a step", editor.Screen.UndoText);
        editor.Screen.Undo!.Execute();
        screen.Run(2);

        Assert.Empty(editor.Program.Sequences[0].Steps);
        Assert.IsType<SequencePage>(editor.Current);
    }

    /// <summary>A change to a trigger, taken back from a sequence's screen, goes to the logic system's screen where triggers are.</summary>
    [Fact]
    public void ATriggerIsTakenBackFromASequencesScreen()
    {
        var screen = Built();
        var editor = screen.Editor;
        Assert.IsType<SequencePage>(editor.Current);

        Assert.Equal("adding a trigger", editor.Screen.UndoText);
        editor.Screen.Undo!.Execute();
        screen.Run(2);

        Assert.Empty(editor.Program.Systems[0].Triggers);
        Assert.Empty(editor.Program.Sequences);
        Assert.IsType<LogicSystemPage>(editor.Current);
    }

    /// <summary>Renaming a logic system is a step of its own, taken back whether or not it runs.</summary>
    [Fact]
    public void RenamingALogicSystemIsTakenBack()
    {
        var screen = Built();
        var editor = screen.Editor;
        var system = editor.Program.Systems[0];
        editor.Back();
        screen.Run(2);
        editor.SwitchSystem(system, true);
        screen.Run(2);

        editor.Name("Front airlock");
        screen.Run(2);
        Assert.Equal("Front airlock", system.Name);

        Assert.Equal("renaming the logic system", editor.Screen.UndoText);
        editor.Screen.Undo!.Execute();
        screen.Run(2);
        Assert.Equal("Airlock", system.Name);
    }

    /// <summary>Renaming a sequence twice is two steps, and redo after both are undone gives the last name back.</summary>
    [Fact]
    public void TwoRenamesAreTwoSteps()
    {
        var screen = Built();
        var editor = screen.Editor;
        var sequence = editor.Program.Sequences[0];
        var was = sequence.Name;

        editor.Name("Cycle");
        screen.Run(2);
        editor.Name("Cycle in");
        screen.Run(2);

        editor.Screen.Undo!.Execute();
        screen.Run(2);
        editor.Screen.Undo!.Execute();
        screen.Run(2);
        Assert.Equal(was, sequence.Name);

        editor.Screen.Redo!.Execute();
        screen.Run(2);
        editor.Screen.Redo!.Execute();
        screen.Run(2);
        Assert.Equal("Cycle in", sequence.Name);
    }

    /// <summary>A new sequence left untouched goes, and leaves no step behind to walk.</summary>
    [Fact]
    public void ANewSequenceLeftUntouchedLeavesNoStep()
    {
        var screen = Built();
        var editor = screen.Editor;
        var system = editor.Program.Systems[0];
        editor.Back();
        screen.Run(2);
        var before = editor.Screen.UndoText;

        editor.NewSequence(system);
        screen.Run(2);
        editor.Back();
        screen.Run(2);

        Assert.Single(editor.Program.Sequences);
        Assert.Equal(before, editor.Screen.UndoText);
    }

    /// <summary>A copied sequence is a step its logic system's undo takes back.</summary>
    [Fact]
    public void ACopyIsTakenBack()
    {
        var screen = Built();
        var editor = screen.Editor;
        Light(screen);
        editor.Back();
        screen.Run(2);

        editor.Rows()[0].Copies!.Execute();
        screen.Run(2);
        Assert.Equal(2, editor.Program.Sequences.Count);
        Assert.Equal("copying a sequence", editor.Screen.UndoText);

        editor.Screen.Undo!.Execute();
        screen.Run(2);
        Assert.Single(editor.Program.Sequences);
    }

    /// <summary>Making a logic system is not taken back from home while it runs; switched off, it is.</summary>
    [Fact]
    public void HomeWaitsForARunningLogicSystem()
    {
        var screen = Screen.Fresh();
        var editor = screen.Editor;
        editor.PressStart("logic system");
        screen.Run(2);
        editor.Name("Hangar");
        screen.Run(2);
        var system = editor.Program.Systems[0];
        editor.GoToRoot();
        screen.Run(2);
        Assert.Equal("adding a logic system", editor.Screen.UndoText);

        system.Enabled = true;
        screen.Run(2);
        Assert.Null(editor.Screen.Undo);

        system.Enabled = false;
        screen.Run(2);
        editor.Screen.Undo!.Execute();
        screen.Run(2);
        Assert.Empty(editor.Program.Systems);
    }

    /// <summary>Renaming the construct is a step on home's way back.</summary>
    [Fact]
    public void RenamingTheConstructIsTakenBack()
    {
        var screen = Screen.Fresh();
        var editor = screen.Editor;
        var was = editor.World.Name;

        editor.Name("Ferry");
        screen.Run(2);
        Assert.Equal("Ferry", editor.World.Name);

        Assert.Equal("renaming the construct", editor.Screen.UndoText);
        editor.Screen.Undo!.Execute();
        screen.Run(2);
        Assert.Equal(was, editor.World.Name);
    }

    /// <summary>A deleted logic system brought back by undo keeps its own way back.</summary>
    [Fact]
    public void ADeletedLogicSystemKeepsItsWayBack()
    {
        var screen = Built();
        var editor = screen.Editor;
        var system = editor.Program.Systems[0];
        Light(screen);
        editor.GoToRoot();
        screen.Run(2);
        system.Enabled = false;

        editor.RootHeadings()[0].Deletes!.Execute();
        screen.Run(2);
        editor.Screen.Undo!.Execute();
        screen.Run(2);

        Assert.Same(system, editor.Program.Systems[0]);
        Assert.False(system.Enabled);
        editor.OpenBlueprintSystem(0);
        screen.Run(2);
        Assert.Equal("adding a step", editor.Screen.UndoText);
    }

    /// <summary>Another program read in place of the one there starts every way back again.</summary>
    [Fact]
    public void ApplyingAScriptStartsAgain()
    {
        var screen = Built();
        var editor = screen.Editor;
        Light(screen);
        var text = Storage.ProgramText.Save(editor.Program, editor.World.Values);

        Assert.Equal("", editor.Apply(text));
        editor.GoToRoot();
        screen.Run(2);
        Assert.Null(editor.Screen.Undo);

        editor.OpenBlueprintSystem(0);
        screen.Run(2);
        Assert.Null(editor.Screen.Undo);
    }

    /// <summary>A logic system with a button, on its sequence's screen.</summary>
    static Screen Built()
    {
        var screen = Screen.Fresh();
        screen.Editor.NewButtonSystem("Airlock");
        return screen.Run(2);
    }

    /// <summary>Adds a step to the sequence open, switching the airlock light on.</summary>
    static void Light(Screen screen)
    {
        screen.Editor.PressAdd("task");
        screen.Run(2);
        screen.Editor.PressBlock("Airlock Light");
        screen.Editor.PressChoice("power");
        screen.Editor.Pick().Arguments!.Form.Fields[0].Text = "on";
        screen.Editor.PressPrimary();
        screen.Run(2);
    }
}
