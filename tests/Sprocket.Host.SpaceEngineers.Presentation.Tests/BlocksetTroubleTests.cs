using Sprocket.Host.SpaceEngineers.Features.Blueprints;
using Sprocket.Host.SpaceEngineers.Features.LogicSystems;
using Sprocket.Host.SpaceEngineers.Features.Sequences;
using Sprocket.Sim.Machine;
using Lumel.Text;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>What is wrong with one blockset is said on its row and on its installation panel, which leads to where it is mended.</summary>
public class BlocksetTroubleTests
{
    const string Why = "Sequence 1, step 1: This affects something that isn't here any more.";

    /// <summary>The blockset's row says what is wrong in place of what it is doing.</summary>
    [Fact]
    public void ItsRowSaysWhatIsWrong()
    {
        var sim = Broken();

        var row = sim.Editor.RootRows()[0];
        Assert.True(row.Problems > 0);
        Assert.Equal(Why, row.Problem);
    }

    /// <summary>The blueprint's heading is marked broken with it, so the trouble shows from the blueprint down.</summary>
    [Fact]
    public void ItsBlueprintsHeadingIsMarkedWithIt()
    {
        var sim = Broken();

        var heading = sim.Editor.RootHeadings().Single();
        Assert.Equal(sim.Editor.RootRows()[0].Problems, heading.Problems);
        Assert.True(heading.Problems > 0);
    }

    /// <summary>The blueprint named in its heading opens the blueprint, on its blocks, and Back comes back to it.</summary>
    [Fact]
    public void TheBlueprintInItsHeadingOpensTheBlueprint()
    {
        var sim = Broken();
        var system = sim.Editor.Program.Systems.Single();

        sim.Editor.RootRows()[0].Opens!.Execute();
        sim.Tick();

        var from = sim.Editor.Screen.Heading[1];
        Assert.Equal(system.Name, from.Text);
        from.Opens!.Execute();
        sim.Tick();

        Assert.IsType<LogicSystemPage>(sim.Editor.Current);
        Assert.Same(system.Instances[0], sim.Editor.StandIn(system));

        sim.Editor.Back();
        sim.Tick();
        Assert.IsType<BlueprintPage>(sim.Editor.Current);
    }

    /// <summary>Its installation panel says the same, and opens the sequence at fault as this blockset runs it.</summary>
    [Fact]
    public void ItsPanelSaysWhatIsWrongAndLeadsToTheSequence()
    {
        var sim = Broken();
        var system = sim.Editor.Program.Systems.Single();

        sim.Editor.RootRows()[0].Opens!.Execute();
        Run(sim, 401, 500);

        Assert.IsType<BlueprintPage>(sim.Editor.Current);
        Assert.Contains(sim.Editor.Current.View!.Within().OfType<Label>(), label => label.Text == Why && label.IsVisible);

        sim.Editor.PressStart("Open Sequence 1");
        sim.Tick();

        Assert.IsType<SequencePage>(sim.Editor.Current);
        Assert.Same(system.Instances[0], sim.Editor.StandIn(system));
    }

    /// <summary>A blueprint whose one blockset has lost the door its sequence acts on.</summary>
    static Simulation Broken()
    {
        var sim = Simulation.Fresh();
        sim.Editor.NewButtonSystem("Airlock");
        sim.Editor.PressAdd("task");
        sim.Editor.PressBlock("Airlock Inner Door");
        sim.Editor.PressChoice("Open");
        sim.Editor.PressPrimary();
        sim.Editor.GoToRoot();
        sim.Tick();

        sim.Editor.GoToRoot();
        sim.Tick();

        // Off, so the fault is said as it is now rather than as why Sprocket switched it off.
        sim.Editor.Program.Systems.Single().Enabled = false;
        sim.Machine.Grind(sim.Machine.Fitted.First(fitting => fitting.Name == "Airlock Inner Door"));
        Run(sim, 1, 400);

        return sim;
    }

    /// <summary>Runs the world and the screen from tick <paramref name="from" /> to <paramref name="to" />, as a player looking at it would.</summary>
    static void Run(Simulation sim, int from, int to)
    {
        for (var tick = from; tick <= to; tick++)
        {
            sim.Tick();
            sim.Editor.Tick(tick);
        }
    }
}
