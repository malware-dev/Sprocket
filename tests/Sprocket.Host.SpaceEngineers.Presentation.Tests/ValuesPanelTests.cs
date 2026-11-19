using System.Globalization;
using Sprocket.Host.SpaceEngineers.Features.Sequences;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using Lumel.Text;
using VRage.Input;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// The values a step gives are changed in a panel hanging from the part of its line that says them, where the selector
/// would only ask them again.
/// </summary>
/// <remarks>Done or Enter keeps what the panel says, as one change, once the block takes it; Escape or a press anywhere else leaves the step as it was.</remarks>
public class ValuesPanelTests
{
    /// <summary>Pressing a step's value opens its panel on what the step gives, and Enter keeps a new value there as one change.</summary>
    [Fact]
    public void AStepsValueIsChangedInItsPanel()
    {
        var screen = Falloff();
        PressValue(screen);

        var giving = Assert.IsType<SequencePage>(screen.Editor.Current).Giving;
        Assert.NotNull(giving);
        Assert.Equal("2.0", giving.Form.Fields[0].Text);
        Type(screen, "1.5");
        screen.Scene.Key(MyKeys.Enter, false, false);
        screen.Run(2);

        Assert.Null(Page(screen).Giving);
        var act = Step(screen);
        Assert.Equal(1.5, Assert.Single(act.Arguments).Number);
        Assert.Equal("Falloff", act.ActionId);
        Assert.True(act.Sets);
        Assert.Single(screen.Editor.Program.Sequences[0].Steps);

        screen.Editor.Undo(screen.Editor.Program.Systems[0], screen.Editor.Program.Sequences[0]);
        Assert.Equal(2, Assert.Single(Step(screen).Arguments).Whole);
    }

    /// <summary>A value the block refuses keeps the panel open, saying why, and the step keeps what it gave.</summary>
    [Fact]
    public void AValueTheBlockRefusesIsNotTaken()
    {
        var screen = Falloff();
        PressValue(screen);

        Type(screen, "5");
        screen.Scene.Key(MyKeys.Enter, false, false);
        screen.Run(2);

        var giving = Page(screen).Giving;
        Assert.NotNull(giving);
        Assert.Equal("Falloff must be between 0 and 2.", giving.Why);
        Assert.Equal(2, Assert.Single(Step(screen).Arguments).Whole);
    }

    /// <summary>Escape puts the panel away and leaves the step as it was.</summary>
    [Fact]
    public void EscapeLeavesTheStepAsItWas()
    {
        var screen = Falloff();
        PressValue(screen);

        Type(screen, "1");
        screen.Scene.Key(MyKeys.Escape, false, false);
        screen.Run(2);

        Assert.Null(Page(screen).Giving);
        Assert.Equal(2, Assert.Single(Step(screen).Arguments).Whole);
    }

    /// <summary>What an if compares a number with is changed in a panel under it, as one change.</summary>
    [Fact]
    public void WhatAnIfComparesWithIsChangedInItsPanel()
    {
        var screen = Screen.Fresh();
        screen.Editor.NewButtonSystem("Airlock");
        screen.Run(2);
        screen.Editor.PressAdd(SentenceText.IfWord);
        screen.Editor.PressBlock("Airlock Light");
        screen.Editor.PressChoice("falloff");
        screen.Editor.Typed("below 1");
        screen.Run(2);

        var sequence = screen.Editor.Program.Sequences[0];
        var opens = sequence.Steps.OfType<If>().Single();
        Assert.Equal(1, opens.Compared.Value.Whole);
        screen.Editor.StepRows()[sequence.Steps.IndexOf(opens)].Slots.OfType<ArgumentsSlot>()
            .Single(part => part.Holds == ArgumentRole.Checked).Opens!.Execute();
        screen.Run(2);

        var giving = Page(screen).Giving;
        Assert.NotNull(giving);
        giving.Form.Fields[0].Text = "1.5";
        giving.Done();
        screen.Run(2);

        Assert.Null(Page(screen).Giving);
        var now = sequence.Steps.OfType<If>().Single();
        Assert.Equal(1.5, now.Compared.Value.Number);
        Assert.Equal(Relation.Below, now.Compared.Relation);
    }

    /// <summary>
    /// A value chosen among a few words, a switch's on, off and toggle, is offered in a menu under its part as a state's
    /// values are, the one it holds marked; toggle makes the step the game's toggle, as one change.
    /// </summary>
    [Fact]
    public void ASwitchsValueIsChosenFromAMenuUnderIt()
    {
        var screen = Screen.Fresh();
        screen.Editor.NewButtonSystem("Airlock");
        screen.Run(2);
        screen.Editor.PressAdd("task");
        screen.Editor.PressBlock("Airlock Light");
        screen.Editor.PressChoice("power");
        screen.Run(2);
        screen.Do("press", "on");
        screen.Editor.PressPrimary();
        screen.Run(2);
        var power = Step(screen).ActionId;

        // Pressed where it stands, as a person presses it, so the menu hangs under it.
        var row = screen.Editor.StepRows()[0];
        var at = row.ChipOf(row.Slots.OfType<ArgumentsSlot>().Single(part => part.Holds == ArgumentRole.NewValue))!.Bounds.Center;
        screen.Do(at.X.ToString(CultureInfo.InvariantCulture), at.Y.ToString(CultureInfo.InvariantCulture));
        screen.Run(2);
        Assert.Null(Page(screen).Giving);
        Assert.Equal(new[] { "on", "off", "toggle" }, screen.Editor.Screen.Answers.Select(answer => answer.Text).ToArray());
        Assert.Equal("on", screen.Editor.Screen.Default!.Text);

        screen.Editor.PressAnswer("toggle");
        screen.Run(2);
        var step = Step(screen);
        Assert.Equal(ReachKind.Action, step.Form);
        Assert.Equal(power, Vocabulary.Flipped(screen.Editor.World, screen.Editor.Blocks(screen.Editor.Program.Systems[0], step.Targets), step.ActionId));
        Assert.Empty(step.Arguments);

        screen.Editor.Undo(screen.Editor.Program.Systems[0], screen.Editor.Program.Sequences[0]);
        Assert.Equal(ReachKind.Value, Step(screen).Form);
        Assert.True(Assert.Single(Step(screen).Arguments).Flag);
    }

    /// <summary>
    /// A value's mark offers a fixed value, the settings holding what it takes and a new one; a setting chosen stands in the
    /// value's place, and Enter keeps the step giving the setting, as one change.
    /// </summary>
    [Fact]
    public void AValuesMarkChoosesASettingToStandInItsPlace()
    {
        var sim = Arm(out var system, out var sequence);
        var page = Assert.IsType<SequencePage>(sim.Editor.Current);
        var form = page.Giving!.Form;
        form.MarkOf(0)!.Command!.Execute();
        sim.Tick();
        var answers = sim.Editor.Screen.Answers.Select(answer => answer.Text).ToArray();
        Assert.Equal(4, answers.Length);
        Assert.Equal(sim.Editor.Texts.ValueFixed, answers[0]);
        Assert.Equal(sim.Editor.Texts.ValueNewSetting, answers[1]);
        Assert.True(sim.Editor.Screen.Answers[2].IsRule, "the settings stand apart, after a line");
        Assert.StartsWith("Reach", answers[3]);

        sim.Editor.PressAnswer(answers[3]);
        sim.Tick();
        Assert.NotNull(page.Giving);
        Assert.Equal("Reach", form.SettingAt(0));

        page.Giving!.Done();
        sim.Tick();
        var act = Assert.IsType<Act>(Assert.Single(sequence.Steps));
        Assert.True(act.Arguments[0].IsSetting);
        Assert.Equal("Reach", act.Arguments[0].Text);

        sim.Editor.Undo(system, sequence);
        Assert.False(Assert.IsType<Act>(Assert.Single(sequence.Steps)).Arguments[0].IsSetting);
    }

    /// <summary>The arm's blueprint, its step moving the piston, with a real setting and a text one, its page open on the step's values panel.</summary>
    static Simulation Arm(out AutomationSystem system, out Sequence sequence)
    {
        var sim = Simulation.Fresh();
        sim.Machine.Piston(50, "Arm Piston");
        sim.Tick();
        const string program = "sprocket 2 alpha 2\n\n"
            + "let 'Arm Piston' = block 'ExtendedPistonBase/LargePistonBase' [ 50, [ (50 0 0) ], \"Arm Piston\" ]\n\n"
            + "system blueprint \"0b0c8f3e-5d5e-4a43-9a51-6f1c2d7e9a10\" as Malware.Arm\n{\n"
            + "    required slot arm expects ExtendedPistonBase\n"
            + "    optional slot Reach expects real = 5\n"
            + "    optional slot Label expects text = \"x\"\n\n"
            + "    sequence 1 {\n        call sprocket.move_to(10, 0.3) on [ arm ]\n    }\n}\n\n"
            + "instance 1 of \"0b0c8f3e-5d5e-4a43-9a51-6f1c2d7e9a10\" as 'Installation 1' {\n    arm = [ 'Arm Piston' ]\n}\n";
        Assert.Equal("", Framework.Language.Reading.ProgramReader.Load(program, sim.Editor.Program, sim.World.Values));
        system = sim.Editor.Program.Systems[0];
        sequence = system.Sequences[0];
        system.Enabled = false;
        sim.Editor.Program.Edited();
        sim.Editor.Invalidate();
        sim.Tick();
        sim.Editor.Push(new SequencePage(sim.Editor, sequence));
        for (var i = 0; i < 5; i++)
            sim.Tick();

        sim.Editor.StepRows()[0].Slots.OfType<ArgumentsSlot>().First().Opens!.Execute();
        sim.Tick();
        return sim;
    }

    /// <summary>New setting... asks for the setting in a panel of its own, which makes it, as one change, and has it give the value.</summary>
    [Fact]
    public void ANewSettingIsMadeToStandInForTheValue()
    {
        var sim = Arm(out var system, out _);
        var page = Assert.IsType<SequencePage>(sim.Editor.Current);
        var form = page.Giving!.Form;
        form.MarkOf(0)!.Command!.Execute();
        sim.Tick();
        sim.Editor.PressAnswer(sim.Editor.Texts.ValueNewSetting);
        sim.Tick();

        var making = page.Giving!.Making;
        Assert.NotNull(making);
        Assert.Equal(2, making.Form.Count);
        making.Done();
        sim.Tick();

        var made = system.FindPart(sim.Editor.Texts.SettingNumbered);
        Assert.NotNull(made);
        Assert.Equal("real", made.Setting);
        Assert.True(made.Optional);
        Assert.Equal(10, made.Default.Number);
        Assert.Equal(made.Id, form.SettingAt(0));
        Assert.NotNull(page.Giving);
    }

    /// <summary>A new setting whose default is left empty is one every installation must choose.</summary>
    [Fact]
    public void ANewSettingWithNoDefaultIsRequired()
    {
        var sim = Arm(out var system, out _);
        var page = Assert.IsType<SequencePage>(sim.Editor.Current);
        page.Giving!.Form.MarkOf(0)!.Command!.Execute();
        sim.Tick();
        sim.Editor.PressAnswer(sim.Editor.Texts.ValueNewSetting);
        sim.Tick();

        var making = page.Giving!.Making!;
        making.Form.Fields[1].Text = "";
        making.Done();
        sim.Tick();

        var made = system.FindPart(sim.Editor.Texts.SettingNumbered);
        Assert.NotNull(made);
        Assert.False(made.Optional);
        Assert.False(made.HasDefault);
    }

    /// <summary>A state's values are offered with the settings holding its type, each with its default; one chosen is set, as one change.</summary>
    [Fact]
    public void AStateIsSetToASettingOfItsType()
    {
        var sim = Simulation.Fresh();
        const string program = "sprocket 2 alpha 2\n\n"
            + "system blueprint \"0b0c8f3e-5d5e-4a43-9a51-6f1c2d7e9a10\" as Malware.Hatch\n{\n"
            + "    optional slot Home expects side = outside\n"
            + "    optional slot Glow expects real = 1.5\n\n"
            + "    type side = inside | outside\n\n"
            + "    let where = inside\n\n"
            + "    sequence 1 {\n        set where to outside\n    }\n}\n";
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

        sim.Editor.StepRows()[0].Slots.OfType<ArgumentsSlot>().Single(part => part.Holds == ArgumentRole.NewValue).Opens!.Execute();
        sim.Tick();
        var answers = sim.Editor.Screen.Answers.Select(answer => answer.Text).ToList();
        Assert.Contains(sim.Editor.Texts.ValueNewSetting, answers);
        var home = answers.Single(answer => answer.StartsWith("Home"));
        Assert.DoesNotContain(answers, answer => answer.StartsWith("Glow"));

        sim.Editor.PressAnswer(home);
        sim.Tick();
        var set = Assert.IsType<SetState>(Assert.Single(sequence.Steps));
        Assert.True(set.Value.IsSetting);
        Assert.Equal("Home", set.Value.Text);
        sim.Editor.Undo(system, sequence);
        Assert.Equal(Literal.Name("outside"), Assert.IsType<SetState>(Assert.Single(sequence.Steps)).Value);
    }

    /// <summary>A move whose speed is in total says so after the speed; one at each piston's speed says nothing more.</summary>
    [Fact]
    public void AMoveInTotalSaysSo()
    {
        var sim = Simulation.Fresh();
        sim.Machine.Piston(50, "Arm Piston");
        sim.Tick();
        const string program = "sprocket 1 alpha 27\n\n"
            + "let 'Arm Piston' = block 'ExtendedPistonBase/LargePistonBase' [ 50, [ (50 0 0) ], \"Arm Piston\" ]\n\n"
            + "system \"0b0c8f3e-5d5e-4a43-9a51-6f1c2d7e9a10\" as Malware.Arm\n{\n"
            + "    sequence 1 {\n"
            + "        call sprocket.move_to(10, 0.3, total) on [ 'Arm Piston' ]\n"
            + "        call sprocket.move_to(10, 0.3) on [ 'Arm Piston' ]\n"
            + "    }\n"
            + "}\n";
        Assert.Equal("", Framework.Language.Reading.ProgramReader.Load(program, sim.Editor.Program, sim.World.Values));
        var system = sim.Editor.Program.Systems[0];
        system.Enabled = false;
        sim.Editor.Program.Edited();
        sim.Editor.Invalidate();
        sim.Tick();
        sim.Editor.Push(new SequencePage(sim.Editor, system.Sequences[0]));
        for (var i = 0; i < 5; i++)
            sim.Tick();

        Assert.EndsWith("at 0.3 m/s in total", sim.Editor.Reads(0));
        Assert.EndsWith("at 0.3 m/s", sim.Editor.Reads(1));
    }

    /// <summary>Slots, chosen on a selector, and text written over several lines are given on a screen of their own, never in a panel.</summary>
    [Fact]
    public void ValuesChosenOnAScreenOfTheirOwnAreNotPanelled()
    {
        Assert.False(ValuesQuestion.Chooses(new[] { Field.Called("speed") }));
        Assert.True(ValuesQuestion.Chooses(new[] { Field.Called("speed"), Field.TakingSlots("doors", null!) }));
        Assert.True(ValuesQuestion.Chooses(new[] { new Field { Name = "text", Edits = FieldEditor.Lines } }));
    }

    /// <summary>A sequence whose one step sets the airlock light's falloff to 2, shown on its page.</summary>
    static Screen Falloff()
    {
        var screen = Screen.Fresh();
        screen.Editor.NewButtonSystem("Airlock");
        screen.Run(2);
        screen.Editor.PressAdd("task");
        screen.Editor.PressBlock("Airlock Light");
        screen.Editor.PressChoice("falloff");
        screen.Editor.Typed("2");
        return screen.Run(2);
    }

    static SequencePage Page(Screen screen) => Assert.IsType<SequencePage>(screen.Editor.Current);

    static Act Step(Screen screen) => Assert.IsType<Act>(Assert.Single(screen.Editor.Program.Sequences[0].Steps));

    /// <summary>Presses the part of the step's line saying its value.</summary>
    static void PressValue(Screen screen)
    {
        screen.Editor.StepRows()[0].Slots.OfType<ArgumentsSlot>().Single().Opens!.Execute();
        screen.Run(2);
    }

    /// <summary>Types <paramref name="words" /> over the value, which holds the keyboard as the panel opens.</summary>
    static void Type(Screen screen, string words)
    {
        var box = Assert.IsAssignableFrom<TextBox>(Page(screen).Giving!.Form.Fields[0].Keyboard);
        Assert.True(box.IsFocused);
        box.SelectAll();
        box.Insert(words);
    }
}
