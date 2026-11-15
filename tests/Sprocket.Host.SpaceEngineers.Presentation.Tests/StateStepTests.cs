using Sprocket.Host.SpaceEngineers.Features.LogicSystems;
using Sprocket.Host.SpaceEngineers.Features.Sequences;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using Sandbox.Common.ObjectBuilders;
using VRage.ObjectBuilders;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A step setting a state of its blocks reads with its verb first, enable or disable, which a menu under it changes.</summary>
public class StateStepTests
{
    const string Program = "sprocket 1 alpha 18\n\n"
                           + "let 'Tank' = block 'OxygenTank/SmallHydrogenTank' [ 40, [ (40 0 0) ], \"Tank\" ]\n\n"
                           + "system blueprint \"0b0c8f3e-5d5e-4a43-9a51-6f1c2d7e9a10\" as Malware.'Fueling'\nchanged \"2026-10-16T10:00:00Z\"\n{\n"
                           + "    required slot 'Tank' expects OxygenTank\n\n"
                           + "    let filling = off\n\n"
                           + "    when filling == on then set Stockpile on [ 'Tank' ] to on\n\n"
                           + "    sequence 1 as Fill {\n"
                           + "        set Stockpile on [ 'Tank' ] to on\n"
                           + "        set Stockpile on [ 'Tank' ] to off\n"
                           + "    }\n"
                           + "}\n\n"
                           + "instance 1 of \"0b0c8f3e-5d5e-4a43-9a51-6f1c2d7e9a10\" as 'Installation 1' {\n"
                           + "    'Tank' = [ 'Tank' ]\n"
                           + "}\n";

    /// <summary>A tank's stockpile switch set on reads "enable stockpiling on Tank", the verb first and "on" before the blocks; set off, "disable".</summary>
    [Fact]
    public void AStateStepReadsWithItsVerbFirst()
    {
        var sim = Fueling(out var sequence);
        sim.Editor.Push(new SequencePage(sim.Editor, sequence));
        for (var i = 0; i < 5; i++)
            sim.Tick();

        var on = sim.Editor.StepRows()[0];
        Assert.Equal(StepKind.Enable, on.Kind);
        Assert.Equal("enable", Assert.IsType<ArgumentsSlot>(on.Slots[0]).Filled[0]);
        Assert.IsType<ActionSlot>(on.Slots[1]);
        Assert.IsType<EntitiesSlot>(on.Slots[2]);
        Assert.Equal(3, on.Slots.Count);
        Assert.Equal("on", SentenceText.Joining(on.Slots, 2));
        Assert.Equal("", SentenceText.Lead(on.Kind));

        Assert.Equal("disable", Assert.IsType<ArgumentsSlot>(sim.Editor.StepRows()[1].Slots[0]).Filled[0]);
    }

    /// <summary>The verb is chosen from a menu under it, the one the step does marked, as one edit.</summary>
    [Fact]
    public void TheVerbIsChosenFromAMenuUnderIt()
    {
        var sim = Fueling(out var sequence);
        sim.Editor.Push(new SequencePage(sim.Editor, sequence));
        sim.Tick();

        sim.Editor.StepRows()[0].Slots[0].Opens!.Execute();
        sim.Tick();
        Assert.Equal(new[] { "enable", "disable", "toggle" }, sim.Editor.Screen.Answers.Select(answer => answer.Text).ToArray());
        Assert.Equal("enable", sim.Editor.Screen.Default!.Text);

        sim.Editor.PressAnswer("disable");
        sim.Tick();
        var act = Assert.IsType<Act>(sequence.Steps[0]);
        Assert.False(act.Arguments[0].Flag);
        Assert.Equal("Stockpile", act.ActionId);
        Assert.Equal(new[] { "Tank" }, act.Targets);
        Assert.Equal(2, sequence.Steps.Count);

        sim.Editor.Screen.Undo!.Execute();
        sim.Tick();
        Assert.True(((Act)sequence.Steps[0]).Arguments[0].Flag);

        sim.Editor.StepRows()[0].Slots[0].Opens!.Execute();
        sim.Tick();
        sim.Editor.PressAnswer("toggle");
        sim.Tick();
        var toggled = Assert.IsType<Act>(sequence.Steps[0]);
        Assert.Equal(ReachKind.Action, toggled.Form);
        Assert.Empty(toggled.Arguments);
        Assert.Equal("Stockpile", Vocabulary.Flipped(sim.World, sim.Editor.Blocks(sequence.System, toggled.Targets), toggled.ActionId));
    }

    /// <summary>The verb of what a trigger does is chosen from a menu under it, as one edit.</summary>
    [Fact]
    public void ATriggersVerbIsChosenFromAMenuUnderIt()
    {
        var sim = Fueling(out var sequence);
        var system = sequence.System;
        sim.Editor.Push(new LogicSystemPage(sim.Editor, system));
        sim.Tick();

        sim.Editor.Triggers()[0].Does[0].Opens!.Execute();
        sim.Tick();
        Assert.Equal(new[] { "enable", "disable", "toggle" }, sim.Editor.Screen.Answers.Select(answer => answer.Text).ToArray());
        sim.Editor.PressAnswer("disable");
        sim.Tick();
        var act = Assert.IsType<Act>(system.Triggers[0].Does);
        Assert.False(act.Arguments[0].Flag);
        Assert.Equal("Stockpile", act.ActionId);
        Assert.Equal(new[] { "Tank" }, act.Targets);
        Assert.Equal("disable", Assert.IsType<ArgumentsSlot>(sim.Editor.Triggers()[0].Does[0]).Filled[0]);

        sim.Editor.Screen.Undo!.Execute();
        sim.Tick();
        Assert.True(((Act)system.Triggers[0].Does).Arguments[0].Flag);
    }

    /// <summary>The fueling program in a world with its tank, read in, its system switched off so it can be changed.</summary>
    static Simulation Fueling(out Sequence sequence)
    {
        var sim = Simulation.Fresh();
        // A definition of its own: the demo's hydrogen tank is a plain block, and a kind is described once per definition.
        sim.Machine.Weld(new Tank(40, "Tank", new MyObjectBuilderType(typeof(MyObjectBuilder_OxygenTank)), "SmallHydrogenTank",
            "Small Hydrogen Tank"));
        sim.Tick();
        Assert.Equal("", Framework.Language.Reading.ProgramReader.Load(Program, sim.Editor.Program, sim.World.Values));
        var system = sim.Editor.Program.Systems[0];
        system.Enabled = false;
        sim.Editor.Program.Edited();
        sim.Editor.Invalidate();
        sim.Tick();
        sequence = system.Sequences[0];
        return sim;
    }
}
