using Sprocket.Host.SpaceEngineers.Features.LogicSystems;
using Sprocket.Host.SpaceEngineers.Features.Sequences;
using Sprocket.Host.SpaceEngineers.Framework.Text;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// Each screen of a logic system takes back what was changed on it, with whatever later change on another screen leaned on
/// that, under the switch it was made under.
/// </summary>
public class UndoTests
{
    /// <summary>A step added on a sequence is taken back on the sequence, and the logic system's screen leaves it standing.</summary>
    [Fact]
    public void AStepIsTakenBackOnItsSequence()
    {
        var screen = Built();
        var editor = screen.Editor;
        Light(screen);

        Assert.Equal(Filled.With(editor.Texts.ChangeAddStep, "number", "1", "place", SequenceText.Called(editor.Texts, editor.Program.Sequences[0]),
            "thing", SentenceText.Short(editor.Reads(0))), editor.Screen.UndoText);
        editor.Screen.Undo!.Execute();
        screen.Run(2);

        Assert.Empty(editor.Program.Sequences[0].Steps);
        Assert.Single(editor.Program.Systems[0].Triggers);
        Assert.IsType<SequencePage>(editor.Current);
    }

    /// <summary>
    /// The arrows' words are put together once for a change, never while nothing changes, though they are asked for every
    /// tick, and once more when a block on the construct is renamed in the game.
    /// </summary>
    [Fact]
    public void TheArrowsWordsArePutTogetherOnlyForAChange()
    {
        var sim = Simulation.Fresh();
        var editor = sim.Editor;
        editor.NewButtonSystem("Airlock");
        editor.PressAdd("task");
        editor.PressBlock("Airlock Light");
        editor.PressChoice("power");
        editor.Pick().Arguments!.Form.Fields[0].Text = "on";
        editor.PressPrimary();
        for (var i = 0; i < 2; i++)
            sim.Tick();

        var before = editor.Worded;
        for (var i = 0; i < 600; i++)
            sim.Tick();
        Assert.Equal(0, editor.Worded - before);

        sim.Machine.Fitted.First(fitting => fitting.Name == "Airlock Light").Name = "Lock Light";
        sim.Tick();
        Assert.Equal(1, editor.Worded - before);
    }

    /// <summary>A deleted step is named on the way back by its number, its sequence and its words, and the way forward says the same.</summary>
    [Fact]
    public void ADeletedStepIsNamedByItsNumberAndWords()
    {
        var screen = Built();
        var editor = screen.Editor;
        Light(screen);
        Light(screen);
        var second = editor.Reads(1);

        editor.StepRows()[1].Deletes!.Execute();
        screen.Run(2);
        var deleted = Filled.With(editor.Texts.ChangeRemoveStep, "number", "2", "place",
            SequenceText.Called(editor.Texts, editor.Program.Sequences[0]), "thing", SentenceText.Short(second));
        Assert.Equal(deleted, editor.Screen.UndoText);

        editor.Screen.Undo!.Execute();
        screen.Run(2);
        Assert.Equal(second, editor.Reads(1));
        Assert.Equal(deleted, editor.Screen.RedoText);
    }

    /// <summary>
    /// Undo on the logic system's screen takes back the trigger that made the sequence, and the step added on the sequence
    /// since with it, saying so; redo there brings both back.
    /// </summary>
    [Fact]
    public void TheTriggerTakesTheStepAlong()
    {
        var screen = Built();
        var editor = screen.Editor;
        Light(screen);
        editor.BackKeeping();
        screen.Run(2);
        Assert.IsType<LogicSystemPage>(editor.Current);

        var also = Filled.With(editor.Texts.UndoAlso, "change", Filled.With(editor.Texts.ChangeAddTrigger, "thing", SentenceText.Short(editor.Says(0))));
        Assert.StartsWith(also.Substring(0, also.IndexOf("{screens}", StringComparison.Ordinal)), editor.Screen.UndoText);
        Assert.Contains(SequenceText.Called(editor.Texts, editor.Program.Sequences[0]), editor.Screen.UndoText);
        editor.Screen.Undo!.Execute();
        screen.Run(2);

        Assert.Empty(editor.Program.Systems[0].Triggers);
        Assert.Empty(editor.Program.Sequences);

        editor.Screen.Redo!.Execute();
        screen.Run(2);
        Assert.Single(editor.Program.Systems[0].Triggers);
        Assert.Single(editor.Program.Sequences[0].Steps);
    }

    /// <summary>A slot's description is taken back on its logic system's screen, and leaves the step on the sequence standing.</summary>
    [Fact]
    public void ASlotsDescriptionIsTakenBackOnItsScreen()
    {
        var screen = Built();
        var editor = screen.Editor;
        var system = editor.Program.Systems[0];
        Light(screen);
        editor.BackKeeping();
        screen.Run(2);
        system.Enabled = false;
        var slot = editor.SlotOf("Airlock Light");
        editor.Invalidate();
        screen.Run(2);
        var part = system.FindPart(slot)!;
        editor.PressTab(editor.Texts.SystemInstallationPanel);
        screen.Run(2);
        editor.OpenSlot(slot);
        screen.Run(2);
        var panel = editor.Asked();
        editor.Input.Focus(panel.About().Box);
        screen.Do("type", "Over the inner door.").Run(1);
        panel.Done();
        screen.Run(2);
        Assert.Equal("Over the inner door.", part.Annotation);
        var said = $"before undo: {editor.Program.Sequences.Count} sequences, undo says '{editor.Screen.UndoText}', on {editor.Screen.Title()}";

        editor.Screen.Undo!.Execute();
        screen.Run(2);

        Assert.Equal("", part.Annotation);
        Assert.True(editor.Program.Sequences.Count > 0, said + $"; after: {editor.Program.Sequences.Count}, on {editor.Screen.Title()}");
        Assert.Single(editor.Program.Sequences[0].Steps);
    }

    /// <summary>A trigger that made a sequence is taken back on that sequence's screen too, which goes to the logic system's screen.</summary>
    [Fact]
    public void ATriggerIsTakenBackFromASequencesScreen()
    {
        var screen = Built();
        var editor = screen.Editor;
        Assert.IsType<SequencePage>(editor.Current);

        var adding = editor.Texts.ChangeAddTrigger;
        Assert.StartsWith(adding.Substring(0, adding.IndexOf("{thing}", StringComparison.Ordinal)), editor.Screen.UndoText);
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
        editor.BackKeeping();
        screen.Run(2);
        editor.SwitchSystem(system, true);
        screen.Run(2);

        editor.Name("Front airlock");
        screen.Run(2);
        Assert.Equal("Front airlock", system.Name);

        Assert.Equal(Filled.With(editor.Texts.ChangeRenameSystem, "was", "Airlock", "now", "Front airlock"), editor.Screen.UndoText);
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
        editor.BackKeeping();
        screen.Run(2);
        var before = editor.Screen.UndoText;

        editor.NewSequence(system);
        screen.Run(2);
        editor.BackKeeping();
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
        editor.BackKeeping();
        screen.Run(2);

        editor.Rows()[0].Copies!.Execute();
        screen.Run(2);
        Assert.Equal(2, editor.Program.Sequences.Count);
        Assert.Equal(Filled.With(editor.Texts.ChangeCopySequence, "thing", SequenceText.Called(editor.Texts, editor.Program.Sequences[1])),
            editor.Screen.UndoText);

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
        Assert.Equal(Filled.With(editor.Texts.ChangeAddSystem, "thing", "Hangar"), editor.Screen.UndoText);

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

        Assert.Equal(Filled.With(editor.Texts.ChangeRenameConstruct, "was", was, "now", "Ferry"), editor.Screen.UndoText);
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
        Assert.StartsWith(Filled.With(editor.Texts.ChangeAddTrigger, "thing", SentenceText.Short(editor.Says(0))), editor.Screen.UndoText);
    }

    /// <summary>Another program read in place of the one there starts every way back again.</summary>
    [Fact]
    public void ApplyingAScriptStartsAgain()
    {
        var screen = Built();
        var editor = screen.Editor;
        Light(screen);
        var text = Framework.Language.Writing.ProgramWriter.Save(editor.Program, editor.World.Values);

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
