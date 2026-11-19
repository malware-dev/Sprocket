using Sprocket.Host.SpaceEngineers.Framework.Running;
using Sprocket.Host.SpaceEngineers.Framework.Text;
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
        Assert.True(sim.World.Unstartable.CanStart(sequence, system.Instances[0]));
        Assert.False(sim.World.Unstartable.CanStart(sequence, system.Instances[1]));
    }

    /// <summary>A blockset made with its slots empty is switched off alone with no reason recorded, saying which slot is empty.</summary>
    [Fact]
    public void ABlocksetWithAnEmptySlotSaysWhichSlot()
    {
        var sim = TwoBlocksets();
        var system = sim.Editor.Program.Systems.Single();

        sim.Editor.SwitchOn();
        for (var i = 0; i < 120; i++)
            sim.Tick();

        Assert.True(system.Enabled);
        Assert.True(system.Instances[0].Enabled);
        Assert.False(system.Instances[1].Enabled);
        Assert.Null(system.Instances[1].Halt);

        var sequence = SystemSummary.Failing(sim.Editor.Runner, system, system.Instances[1], out var fault);
        Assert.Equal(SequenceProblem.EmptySlot, fault.Problem);
        Assert.EndsWith("Fill it on the installation panel.", SequenceText.Failing(new Texts(), fault, sequence!));
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
        sim.Editor.PressAdd("task");
        sim.Editor.PressBlock("Airlock Inner Door");
        sim.Editor.PressChoice("open");
        sim.Editor.PressPrimary();
        sim.Editor.Back();
        sim.Editor.GoToRoot();
        sim.Tick();

        sim.Editor.GoToRoot();
        sim.Tick();

        sim.Editor.AddInstallation(0);
        sim.Tick();
        sim.Editor.Screen.Heading.Single(part => part.CanRename).Renames!("Port airlock");
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

        sim.Editor.PressStart("when");
        sim.Editor.PressBlock("Inside Button");
        sim.Editor.PressChoice("button 1 is pressed");
        sim.Editor.PressPrimary();
        sim.Tick();
        sim.Editor.PressAdd("task");
        sim.Editor.PressBlock("Airlock Outer Door");
        sim.Editor.PressChoice("open");
        sim.Editor.PressPrimary();
        sim.Editor.BackKeeping();
        for (var i = 0; i < 70; i++)
            sim.Tick();

        return sim;
    }
}
