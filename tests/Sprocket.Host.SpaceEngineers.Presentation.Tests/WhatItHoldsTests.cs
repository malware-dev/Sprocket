using Sprocket.Host.SpaceEngineers.Features.LogicSystems;
using Sprocket.Host.SpaceEngineers.Features.Picking;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Features.Sequences;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// The part of a sentence holding what a step was given says what it holds: an action's options, the value a step sets, or
/// the value an if checks for.
/// </summary>
public class WhatItHoldsTests
{
    const string Program = "sprocket 1 alpha 18\n\n"
                           + "let 'Airlock Inner Door' = block 'Door/LargeBlockDoor' [ 2, [ (2 0 0) ], \"Airlock Inner Door\" ]\n\n"
                           + "system blueprint \"61da898f-e0f5-4b57-8c81-3194c3357d4e\" as Malware.'Airlock System'\nchanged \"2026-10-07T20:49:07Z\"\n{\n"
                           + "    required slot 'Inner Door' expects Door\n\n"
                           + "    sequence 1 as Cycle {\n"
                           + "        if any 'how-far-open' on [ 'Inner Door' ] == 50 then {\n"
                           + "        }\n"
                           + "    }\n"
                           + "}\n\n"
                           + "instance 1 of \"61da898f-e0f5-4b57-8c81-3194c3357d4e\" as 'Installation 1' {\n"
                           + "    'Inner Door' = [ 'Airlock Inner Door' ]\n"
                           + "}\n";

    /// <summary>A step given an action's options holds options, and its part says so.</summary>
    [Fact]
    public void AnActionsOptionsAreOptions()
    {
        var screen = Screen.On(Rig.Demo());
        screen.Editor.NewButtonSystem("Airlock");
        screen.Run(2);
        screen.Editor.PressAdd("task");
        screen.Editor.PressBlock("Airlock Vent");
        screen.Editor.PressChoice("depressurize");
        screen.Run(2);
        screen.Do("press", "move on");
        screen.Editor.PressPrimary();
        screen.Run(2);

        Assert.Equal(screen.Editor.Texts.Codex.SequenceOptionsAbout, Tip(screen.Editor.StepRows()[0]));
    }

    /// <summary>A step setting a value holds the new value, and its part says so.</summary>
    [Fact]
    public void ASetStepHoldsTheNewValue()
    {
        var screen = Screen.Fresh();
        screen.Editor.NewButtonSystem("Airlock");
        screen.Run(2);
        screen.Editor.PressAdd("task");
        screen.Editor.PressBlock("Airlock Light");
        screen.Editor.PressChoice("falloff");
        screen.Editor.Typed("2");
        screen.Run(2);

        Assert.Equal(screen.Editor.Texts.Codex.SequenceNewValueAbout, Tip(screen.Editor.StepRows()[0]));
    }

    /// <summary>An if comparing a value holds the value it checks for, and its part says so.</summary>
    [Fact]
    public void AnIfHoldsTheValueItChecksFor()
    {
        var rig = Simulation.Fresh();
        Assert.Equal("", Storage.ProgramReader.Load(Program, rig.Editor.Program, rig.World.Values));
        rig.Editor.Invalidate();
        rig.Tick();
        var system = rig.Editor.Program.Systems[0];
        rig.Editor.GoToRoot();
        rig.Tick();
        system.Enabled = false;
        rig.Tick();
        rig.Editor.Push(new SequencePage(rig.Editor, system.Sequences[0]));
        for (var i = 0; i < 70; i++)
            rig.Tick();
        rig.Editor.StepRows()[0].Slots[0].Opens!.Execute();
        rig.Tick();
        rig.Editor.Pick().Stages[1].Entries().First(entry => entry.Text == "how far open").Pick(HeldModifier.Alone, 1);
        rig.Tick();
        rig.Editor.Typed("50");
        rig.Tick();

        Assert.Equal(rig.Editor.Texts.Codex.SequenceCheckedAbout, Tip(rig.Editor.StepRows()[0]));
    }

    /// <summary>A trigger setting a value holds the new value, and its part on the logic system's screen says so.</summary>
    [Fact]
    public void ATriggerSettingAValueHoldsTheNewValue()
    {
        var rig = new Rig();
        Panel button = rig.Panel(1, "Button");
        Fitting light = rig.Light(11, "Light").Carrying("Falloff", "Falloff", ValueKind.Real, "0", "3");
        var sim = Simulation.On(rig);
        var program = sim.Editor.Program;
        var system = program.AddSystem();
        program.AddSequence(system, Pressed.On(program, system, Named(sim, button)));

        var set = new Act("Falloff") { Form = ReachKind.Value };
        set.Targets.Add(Named(sim, light));
        set.Arguments.Add(Literal.Of(2L));
        system.Triggers[0].Does = set;
        system.Enabled = false;
        sim.Editor.Push(new LogicSystemPage(sim.Editor, system));
        for (var i = 0; i < 5; i++)
            sim.Tick();

        var row = sim.Editor.Triggers()[0];
        Assert.Equal(sim.Texts.Codex.SequenceNewValueAbout, row.Within().OfType<SlotChip>().Last().Tip?.Text);
    }

    static string Named(Simulation sim, Fitting fitting) =>
        sim.Editor.Program.Declare(sim.World, sim.World.FindEntity(fitting.Id)).Name;

    /// <summary>The tooltip on the row's last part, the one holding what the step was given.</summary>
    static string? Tip(StepRow row) => row.Within().OfType<SlotChip>().Last().Tip?.Text;
}
