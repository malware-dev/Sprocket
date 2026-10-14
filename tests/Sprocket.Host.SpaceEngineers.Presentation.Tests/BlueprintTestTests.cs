using Sprocket.Host.SpaceEngineers.Features.Picking;
using Sprocket.Host.SpaceEngineers.Features.Sequences;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// A test in a blueprint names its blocks by slot, which only an installation answers: picking what it checks again gives it
/// the member of the blocks the slot stands for.
/// </summary>
public class BlueprintTestTests
{
    const string Program = "sprocket 1 alpha 15\n\n"
                           + "let 'Airlock Inner Door' = block 'Door/LargeBlockDoor' [ 2, [ (2 0 0) ], \"Airlock Inner Door\" ]\n"
                           + "let 'Airlock Outer Door' = block 'Door/LargeBlockDoor' [ 3, [ (3 0 0) ], \"Airlock Outer Door\" ]\n\n"
                           + "system blueprint \"61da898f-e0f5-4b57-8c81-3194c3357d4e\" as Malware.'Airlock System' changed \"2026-10-07T20:49:07Z\" {\n"
                           + "    required slot 'Inner Door' expects Door\n"
                           + "    required slot 'Outer Door' expects Door\n\n"
                           + "    sequence 1 as Cycle {\n"
                           + "        if any ext.'switched-on' on [ 'Inner Door' ] == on then {\n"
                           + "            call ext.close on [ 'Outer Door' ]\n"
                           + "            await\n"
                           + "        }\n"
                           + "    }\n"
                           + "}\n\n"
                           + "instance 1 of \"61da898f-e0f5-4b57-8c81-3194c3357d4e\" as 'Installation 1' {\n"
                           + "    'Inner Door' = [ 'Airlock Inner Door' ]\n"
                           + "    'Outer Door' = [ 'Airlock Outer Door' ]\n"
                           + "}\n";

    /// <summary>A state picked again for a test naming a slot tests that state's value on the slot, and the step is mended.</summary>
    [Fact]
    public void AStatePickedAgainOnASlotIsFound()
    {
        var rig = Opened(out var sequence);
        Assert.NotEmpty(rig.Editor.StepRows()[0].Problem);

        Pick(rig, "is on", null);

        var test = Assert.IsType<If>(sequence.Steps[0]).Test;
        Assert.Equal("power", test.MemberId);
        Assert.Equal(BlockKinds.Own, test.Provider);
        Assert.Equal(Literal.Of(true), test.Value);
        Assert.Equal(new[] { "Inner Door" }, test.Targets);
        for (var i = 0; i < 70; i++)
            rig.Tick();

        Assert.Empty(rig.Editor.StepRows()[0].Problem);
    }

    /// <summary>A value picked for a test naming a slot keeps the value's namespace, as it does on a block named outright.</summary>
    [Fact]
    public void AValuePickedOnASlotKeepsItsNamespace()
    {
        var rig = Opened(out var sequence);

        Pick(rig, "How far open", "50");

        var test = Assert.IsType<If>(sequence.Steps[0]).Test;
        Assert.Equal("how-far-open", test.MemberId);
        Assert.Equal(BlockKinds.Own, test.Provider);
    }

    /// <summary>
    /// Opens the test's member from its chip and picks <paramref name="text" />, compared with <paramref name="value" /> typed in
    /// where a value wants one, finishing the pick.
    /// </summary>
    static void Pick(Simulation rig, string text, string? value)
    {
        rig.Editor.StepRows()[0].Slots[0].Opens!.Execute();
        rig.Tick();
        rig.Editor.Pick().Stages[1].Entries().First(entry => entry.Text == text).Pick(Held.Alone, 1);
        rig.Tick();
        if (value != null)
            rig.Editor.Typed(value);
        else
            rig.Editor.PressPrimary();

        rig.Tick();
        Assert.Equal("Cycle", rig.Editor.Screen.Title());
    }

    /// <summary>The blueprint loaded, switched off so it can be edited, with its sequence's screen open and looked at.</summary>
    static Simulation Opened(out Sequence sequence)
    {
        var rig = Simulation.Fresh();
        Assert.Equal("", Storage.ProgramText.Load(Program, rig.Editor.Program, rig.World.Values));
        rig.Editor.Invalidate();
        rig.Tick();

        var system = rig.Editor.Program.Systems[0];
        sequence = system.Sequences[0];
        rig.Editor.GoToRoot();
        rig.Tick();
        system.Enabled = false;
        rig.Tick();
        rig.Editor.Push(new SequencePage(rig.Editor, sequence));
        for (var i = 0; i < 70; i++)
            rig.Tick();

        return rig;
    }
}
