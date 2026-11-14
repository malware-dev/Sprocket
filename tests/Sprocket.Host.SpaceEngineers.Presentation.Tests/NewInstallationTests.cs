using Sprocket.Host.SpaceEngineers.Framework.Running;
using Sprocket.Host.SpaceEngineers.Framework.Text;
using Lumel.Text;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>An installation added to a switched-on blueprint is made switched off to be filled in, and is switched on from its panel.</summary>
public class NewInstallationTests
{
    /// <summary>A new installation is switched off with no reason, says which slot is empty, and offers its switch once every slot is filled.</summary>
    [Fact]
    public void ANewInstallationIsSwitchedOnOnceItsSlotsAreFilled()
    {
        var sim = Simulation.Fresh();
        sim.Machine.Panel(30, "Port Button");
        sim.Tick();

        sim.Editor.NewButtonSystem("Airlock");
        sim.Editor.GoToRoot();
        sim.Tick();
        sim.Editor.GoToRoot();
        sim.Tick();
        sim.Editor.OpenBlueprintSystem(0);
        sim.Tick();
        sim.Editor.SwitchOn();
        Run(sim);
        sim.Editor.GoToRoot();
        sim.Tick();

        var system = sim.Editor.Program.Systems[0];
        sim.Editor.AddInstallation(0);
        Run(sim);
        var port = system.Instances[1];

        Assert.False(port.Enabled);
        Assert.Null(port.Halt);
        SystemSummary.Failing(sim.Editor.Runner, system, port, out var waiting);
        Assert.Equal(SequenceProblem.EmptySlot, waiting.Problem);
        Assert.Contains(Labels(sim), label => label.Text == "Sequence 1: The slot signal slot 1 is empty. Fill it on the installation panel.");

        sim.Editor.Parts()[0].Fills!.Execute();
        sim.Tick();
        sim.Editor.PressBlock("Port Button");
        sim.Editor.PressChoice("button 1 is pressed");
        sim.Editor.PressPrimary();
        Run(sim);

        Assert.Null(SystemSummary.Failing(sim.Editor.Runner, system, port, out _));
        Assert.Contains(Labels(sim), label => label.Text == sim.Texts.BlueprintFilled);

        sim.Editor.Screen.Flip!.Execute();
        Run(sim);

        Assert.True(port.Enabled);
        Assert.Null(port.Halt);
        Assert.DoesNotContain(Labels(sim), label => label.Text == "Every slot is filled.");
    }

    /// <summary>The labels on the page the editor is on that can be seen.</summary>
    static List<Label> Labels(Simulation sim) =>
        sim.Editor.Current.View!.Within().OfType<Label>().Where(label => label.IsVisible).ToList();

    static void Run(Simulation sim)
    {
        for (var i = 0; i < 120; i++)
            sim.Tick();
    }
}
