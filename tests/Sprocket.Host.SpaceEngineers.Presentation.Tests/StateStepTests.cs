using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Features.Sequences;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sandbox.Common.ObjectBuilders;
using VRage.ObjectBuilders;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A step setting a state of its blocks reads with its verb first, enable or disable, which is pressed to change it.</summary>
public class StateStepTests
{
    const string Program = "sprocket 1 alpha 18\n\n"
                           + "let 'Tank' = block 'OxygenTank/SmallHydrogenTank' [ 40, [ (40 0 0) ], \"Tank\" ]\n\n"
                           + "system blueprint \"0b0c8f3e-5d5e-4a43-9a51-6f1c2d7e9a10\" as Malware.'Fueling'\nchanged \"2026-10-16T10:00:00Z\"\n{\n"
                           + "    required slot 'Tank' expects OxygenTank\n\n"
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
        var sim = Simulation.Fresh();
        // A definition of its own: the demo's hydrogen tank is a plain block, and a kind is described once per definition.
        sim.Machine.Weld(new Tank(40, "Tank", new MyObjectBuilderType(typeof(MyObjectBuilder_OxygenTank)), "SmallHydrogenTank",
            "Small Hydrogen Tank"));
        sim.Tick();
        Assert.Equal("", Storage.ProgramReader.Load(Program, sim.Editor.Program, sim.World.Values));
        sim.Editor.Invalidate();
        sim.Tick();
        var sequence = sim.Editor.Program.Systems[0].Sequences[0];
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
}
