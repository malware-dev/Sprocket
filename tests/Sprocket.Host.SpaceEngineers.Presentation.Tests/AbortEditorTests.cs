using Sprocket.Host.SpaceEngineers.Features.Sequences;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using Lumel.Controls;
using Lumel.Text;
using Sprocket.Sim.Machine;
using VRage.Input;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// An abort is added from the step menu at once, and what it says is given in a panel hanging from the part of its line
/// that says it.
/// </summary>
/// <remarks>Done or Enter keeps what the panel says, as one change; Escape or a press anywhere else leaves the abort as it was.</remarks>
public class AbortEditorTests
{
    /// <summary>The step menu adds a bare abort, which reads as ending silently, with no panel to answer.</summary>
    [Fact]
    public void AnAbortIsAddedAtOnce()
    {
        var screen = Added();

        var abort = Assert.IsType<Abort>(Assert.Single(screen.Editor.Program.Sequences[0].Steps));
        Assert.Equal("", abort.Message);
        Assert.Equal(AbortLevel.Note, abort.Level);
        Assert.Null(Page(screen).Aborting);
        Assert.True(screen.Shows("silently"));
    }

    /// <summary>Pressing the abort's part opens its panel on what it says, and Enter keeps a message and level given there, as one change.</summary>
    [Fact]
    public void WhatAnAbortSaysIsGivenInItsPanel()
    {
        var screen = Added();
        PressPart(screen);

        Type(screen, "Door jammed");
        screen.Do("press", "warning");
        screen.Scene.Key(MyKeys.Enter, false, false);
        screen.Run(2);

        Assert.Null(Page(screen).Aborting);
        var abort = Assert.IsType<Abort>(Assert.Single(screen.Editor.Program.Sequences[0].Steps));
        Assert.Equal("Door jammed", abort.Message);
        Assert.Equal(AbortLevel.Warning, abort.Level);
        Assert.True(screen.Shows("saying \"Door jammed\" as a warning"));

        screen.Editor.Undo(screen.Editor.Program.Systems[0], screen.Editor.Program.Sequences[0]);
        Assert.Equal("", abort.Message);
        Assert.Equal(AbortLevel.Note, abort.Level);
    }

    /// <summary>The panel opens on what the abort already says.</summary>
    [Fact]
    public void ThePanelOpensOnWhatTheAbortSays()
    {
        var screen = Added();
        PressPart(screen);
        Type(screen, "Hangar blocked");
        screen.Do("press", "problem");
        Done(screen).Command!.Execute();
        screen.Run(2);

        PressPart(screen);

        var form = Form(screen);
        Assert.Equal("Hangar blocked", form.Fields[0].Text);
        Assert.Equal("problem", form.Fields[1].Text);
    }

    /// <summary>Escape, or a press anywhere else, closes the panel and leaves the abort as it was.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BackingOutLeavesTheAbortAsItWas(bool byEscape)
    {
        var screen = Added();
        PressPart(screen);
        Type(screen, "Door jammed");
        if (byEscape)
            screen.Scene.Key(MyKeys.Escape, false, false);
        else
            screen.Do("800", "900");

        screen.Run(2);

        Assert.Null(Page(screen).Aborting);
        var abort = Assert.IsType<Abort>(Assert.Single(screen.Editor.Program.Sequences[0].Steps));
        Assert.Equal("", abort.Message);
    }

    /// <summary>A blueprint's abort offers its text settings under the message's mark; one chosen says the message, as one change.</summary>
    [Fact]
    public void AnAbortsMarkChoosesASettingToSayItsMessage()
    {
        var sim = Simulation.Fresh();
        const string program = "sprocket 2 alpha 2\n\n"
            + "system blueprint \"0b0c8f3e-5d5e-4a43-9a51-6f1c2d7e9a10\" as Malware.Hatch\n{\n"
            + "    optional slot Why expects text = \"Door jammed\"\n"
            + "    optional slot Glow expects real = 1.5\n\n"
            + "    sequence 1 {\n        abort saying \"Stuck\" as warning\n    }\n}\n";
        Assert.Equal("", Framework.Language.Reading.ProgramReader.Load(program, sim.Editor.Program, sim.World.Values));
        var system = sim.Editor.Program.Systems[0];
        var sequence = system.Sequences[0];
        system.Enabled = false;
        sim.Editor.Program.Edited();
        sim.Editor.Invalidate();
        sim.Tick();
        sim.Editor.Push(new SequencePage(sim.Editor, sequence));
        for (var i = 0; i < 5; i++)
            sim.Tick();

        sim.Editor.StepRows()[0].Slots.OfType<ArgumentsSlot>().Single().Opens!.Execute();
        sim.Tick();
        var form = Assert.IsType<SequencePage>(sim.Editor.Current).Aborting!;
        form.MarkOf(0)!.Command!.Execute();
        sim.Tick();
        var answers = sim.Editor.Screen.Answers.Select(answer => answer.Text).ToArray();
        Assert.Equal(4, answers.Length);
        Assert.StartsWith("Why", answers[3]);

        sim.Editor.PressAnswer(answers[3]);
        sim.Tick();
        Assert.Equal("Why", form.SettingAt(0));
        form.Parent!.Within().OfType<Button>().Single(button => button.Text == sim.Editor.Texts.ChromeDone).Command!.Execute();
        sim.Tick();

        var abort = Assert.IsType<Abort>(Assert.Single(sequence.Steps));
        Assert.Equal("Why", abort.Setting);
        Assert.Equal(AbortLevel.Warning, abort.Level);
        sim.Editor.Undo(system, sequence);
        Assert.Equal("", abort.Setting);
        Assert.Equal("Stuck", abort.Message);
    }

    static Screen Added()
    {
        var screen = Screen.Fresh();
        screen.Editor.NewButtonSystem("Airlock");
        screen.Run(2);
        screen.Editor.PressAdd("abort");
        return screen.Run(3);
    }

    static SequencePage Page(Screen screen) => Assert.IsType<SequencePage>(screen.Editor.Current);

    static FieldSet Form(Screen screen) => Page(screen).Aborting ?? throw new Xunit.Sdk.XunitException("no panel is open");

    static Button Done(Screen screen) => Form(screen).Parent!.Within().OfType<Button>().Single(button => button.Text == "Done");

    /// <summary>Types <paramref name="words" /> over the message, which holds the keyboard as the panel opens.</summary>
    static void Type(Screen screen, string words)
    {
        var box = Assert.IsType<TextBox>(Form(screen).Fields[0].Keyboard);
        Assert.True(box.IsFocused);
        box.SelectAll();
        box.Insert(words);
    }

    /// <summary>Presses the part of the abort's line saying its message and level.</summary>
    static void PressPart(Screen screen)
    {
        var row = Page(screen).View!.Within().OfType<StepRow>().Single();
        row.Slots.OfType<ArgumentsSlot>().Single().Opens!.Execute();
        screen.Run(2);
    }
}
