using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Blueprints;
using Sprocket.Host.SpaceEngineers.Features.LogicSystems;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Features.Sequences;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>What is wrong with one blockset is said on its row and on its installation panel, which leads to where it is mended.</summary>
public class BlocksetTroubleTests
{
    const string Why = "Sequence 1, step 1: Airlock Inner Door was lost.";

    /// <summary>A stopped installation whose lost block is still gone says it was lost.</summary>
    [Fact]
    public void AStopWhoseBlockIsStillGoneSaysItWasLost()
    {
        var sim = Stopped();
        sim.Machine.Grind(sim.Machine.Fitted.First(fitting => fitting.Name == "Airlock Inner Door"));
        Run(sim, 1, 400);

        Assert.Equal("Stopped at step 1 of Sequence 1: Airlock Inner Door was lost.", sim.Editor.RootRows()[0].Problem);
    }

    /// <summary>A stopped installation whose lost block is back says so on its panel, where a button switches it on.</summary>
    [Fact]
    public void ARestoredStopIsSwitchedOnFromItsPanel()
    {
        var sim = Stopped();
        var installation = sim.Editor.Program.Systems.Single().First!;
        Run(sim, 1, 400);

        sim.Editor.RootRows()[0].Opens!.Execute();
        Run(sim, 401, 500);

        const string restored = "Stopped at step 1 of Sequence 1: Airlock Inner Door was lost but is now restored.";
        Assert.Contains(sim.Editor.Current.View!.Within().OfType<Label>(), label => label.Text == restored && label.IsVisible);

        sim.Editor.PressStart("Switch on");
        Run(sim, 501, 520);

        Assert.True(installation.Enabled);
        Assert.Null(installation.Halt);
    }

    /// <summary>A lost block a trigger watches is named where the trouble is said.</summary>
    [Fact]
    public void ALostWatchedBlockIsNamed()
    {
        var sim = Simulation.Fresh();
        sim.Editor.NewSystem("Doors");
        sim.Editor.PressStart(SentenceText.WhenWord);
        sim.Editor.PressBlock("Airlock Inner Door");
        sim.Editor.PressChoice("finishes closing");
        sim.Editor.PressPrimary();
        sim.Editor.PressAdd("task");
        sim.Editor.PressBlock("Airlock Outer Door");
        sim.Editor.PressChoice("close");
        sim.Editor.PressPrimary();
        sim.Editor.SwitchOn();
        sim.Tick();

        sim.Machine.Grind(sim.Machine.Fitted.Single(fitting => fitting.Name == "Airlock Inner Door"));
        for (var i = 0; i < 120; i++)
            sim.Tick();
        sim.Editor.GoToRoot();
        sim.Tick();

        Assert.Contains("Airlock Inner Door, which this trigger watches, was lost.", sim.Editor.RootRows()[0].Problem);
    }

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

        var from = sim.Editor.Screen.Heading.Single(part => part.Opens != null);
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

    /// <summary>A slot that has lost one of its blocks says so on the installation panel, though another block still fills it.</summary>
    [Fact]
    public void ASlotThatLostOneOfItsBlocksSaysSo()
    {
        var sim = Simulation.Fresh();
        sim.Machine.Panel(30, "Port Button");
        sim.Tick();
        sim.Editor.NewSystem("Airlock");
        sim.Editor.PressStart(SentenceText.WhenWord);
        sim.Editor.PressBlock("Airlock Button");
        sim.Editor.TickBlock("Port Button");
        sim.Editor.PressChoice("button 1 is pressed");
        sim.Editor.PressPrimary();
        sim.Editor.GoToRoot();
        sim.Tick();
        sim.Editor.GoToRoot();
        sim.Tick();

        sim.Editor.Program.Systems.Single().Enabled = false;
        sim.Machine.Grind(sim.Machine.Fitted.First(fitting => fitting.Name == "Port Button"));
        Run(sim, 1, 400);

        sim.Editor.RootRows()[0].Opens!.Execute();
        Run(sim, 401, 500);

        var slot = Assert.Single(sim.Editor.Parts());
        Assert.Equal("Port Button was lost.", slot.Problem);
        Assert.NotNull(slot.Fills);
    }

    /// <summary>A blueprint whose one blockset has lost the door its sequence acts on.</summary>
    static Simulation Broken()
    {
        var sim = Simulation.Fresh();
        sim.Editor.NewButtonSystem("Airlock");
        sim.Editor.PressAdd("task");
        sim.Editor.PressBlock("Airlock Inner Door");
        sim.Editor.PressChoice("open");
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

    /// <summary>An installation Sprocket switched off for losing the door its sequence closes, with the door there.</summary>
    static Simulation Stopped()
    {
        var sim = Simulation.Fresh();
        sim.Editor.NewButtonSystem("Airlock");
        sim.Editor.PressAdd("task");
        sim.Editor.PressBlock("Airlock Inner Door");
        sim.Editor.PressChoice("close");
        sim.Editor.PressPrimary();
        sim.Editor.GoToRoot();
        sim.Tick();

        var system = sim.Editor.Program.Systems.Single();
        system.Enabled = true;
        system.First!.Enabled = false;
        system.First.Halt = new Halt(SequenceProblem.MissingTarget, system.Sequences[0].Number, 0, BlockTrouble.None, "Airlock Inner Door");
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
