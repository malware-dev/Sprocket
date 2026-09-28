using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A trigger added to a blueprint is answered by each blockset with its own blocks.</summary>
public class BlocksetTriggerTests
{
    /// <summary>
    /// A button trigger made through one blockset starts it there, while another blockset has not had its button given yet.
    /// </summary>
    [Fact]
    public void ANewButtonStartsTheBlocksetItWasGivenIn()
    {
        var sim = TwoBlocksets();

        Assert.Equal("", sim.Editor.Triggers().Last().Problem);

        // The second has nothing in the button's slot, so there, and only there, nothing can start it.
        var system = sim.Editor.Program.Systems.Single();
        var sequence = system.Sequences.Last();
        Assert.True(sim.World.CanStart(sequence, system.Instances[0]));
        Assert.False(sim.World.CanStart(sequence, system.Instances[1]));
    }

    /// <summary>A blockset made with its slots empty is switched off alone as the program is checked, saying which slot.</summary>
    [Fact]
    public void ABlocksetWithAnEmptySlotIsSwitchedOffAlone()
    {
        var sim = TwoBlocksets();
        var system = sim.Editor.Program.Systems.Single();

        sim.Editor.SwitchOn();
        for (var i = 0; i < 120; i++)
            sim.Tick();

        Assert.True(system.Enabled);
        Assert.True(system.Instances[0].Enabled);
        Assert.False(system.Instances[1].Enabled);
        Assert.Equal(SequenceProblem.EmptySlot, system.Instances[1].Halt!.Problem);
        Assert.EndsWith("slot was empty.", SequenceText.Stopped(system.Instances[1].Halt, system));
    }

    /// <summary>
    /// A blueprint with two blocksets, the first given a new button trigger on its logic system screen, looked at through the
    /// first.
    /// </summary>
    static Simulation TwoBlocksets()
    {
        var sim = Simulation.Fresh();
        sim.Machine.Panel(30, "Inside Button");
        sim.Tick();

        sim.Editor.NewButtonSystem("Airlock");
        sim.Editor.PressAdd("Action…");
        sim.Editor.PressBlock("Airlock Inner Door");
        sim.Editor.PressChoice("Open");
        sim.Editor.PressPrimary();
        sim.Editor.Back();
        sim.Editor.GoToRoot();
        sim.Tick();

        sim.Editor.RootRows()[0].Offers!.Execute();
        sim.Editor.GoToRoot();
        sim.Tick();

        sim.Editor.RootRows().Single(row => row.IsAction).Opens!.Execute();
        sim.Tick();
        sim.Editor.Screen.Heading[0].Renames!("Port airlock");
        sim.Editor.GoToRoot();
        sim.Tick();

        // Through the first, which the editor shows until another is chosen.
        sim.Editor.RootRows()[0].Opens!.Execute();
        sim.Tick();
        sim.Editor.GoToRoot();
        sim.Tick();
        sim.Editor.OpenBlueprintSystem(0);
        sim.Tick();

        if (sim.Editor.Screen.SwitchIsOn)
        {
            sim.Editor.Screen.Flip!.Execute();
            sim.Editor.PressChoice("Switch them off");
            sim.Tick();
        }

        sim.Editor.PressStart("When…");
        sim.Editor.PressBlock("Inside Button");
        sim.Editor.PressChoice("is pressed");
        sim.Editor.PressChoice("A single action…");
        sim.Editor.PressBlock("Airlock Outer Door");
        sim.Editor.PressChoice("Open");
        sim.Editor.PressPrimary();
        for (var i = 0; i < 70; i++)
            sim.Tick();

        return sim;
    }
}
