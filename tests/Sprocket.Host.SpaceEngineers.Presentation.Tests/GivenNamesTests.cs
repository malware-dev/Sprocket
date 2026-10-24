using Sprocket.Host.SpaceEngineers.World;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>An installation and a sequence are given a name of their own as they come into being, and keep it.</summary>
public class GivenNamesTests
{
    /// <summary>Installations read without names are given the names they were shown as, and taking one out renames no other.</summary>
    [Fact]
    public void AnInstallationKeepsItsNameWhenAnotherGoes()
    {
        var (sim, system) = Airlock();
        for (var added = 0; added < 3 && system.Instances.Count < 3; added++)
            sim.Editor.Another(system);
        Run(sim);
        Assert.Equal(3, system.Instances.Count);

        foreach (var instance in system.Instances)
            instance.Name = null;

        Assert.True(GivenNames.Give(sim.Editor.Program, sim.Texts));
        Assert.Equal(new[] { Installation(sim, 1), Installation(sim, 2), Installation(sim, 3) }, Names(system));

        system.RemoveInstance(system.Instances[1]);
        Assert.False(GivenNames.Give(sim.Editor.Program, sim.Texts));
        Assert.Equal(new[] { Installation(sim, 1), Installation(sim, 3) }, Names(system));
    }

    /// <summary>A program read with an installation and a sequence that have no names is given them as it loads.</summary>
    [Fact]
    public void AProgramReadWithoutNamesIsGivenThemAsItLoads()
    {
        var (first, system) = Airlock();
        system.Instances[0].Name = null;
        system.Sequences[0].Name = null;
        first.Editor.Program.Edited();
        first.Tick();
        first.Unit.Written(out var program, out _);
        Assert.DoesNotContain(Installation(first, 1), program);

        var store = Store.Nowhere();
        store.SetValue(BlockStorage.ProgramKey, program);
        var sim = Simulation.On(store);
        sim.Tick();

        var loaded = sim.Editor.Program.Systems[0];
        Assert.Equal(Installation(sim, 1), loaded.Instances[0].Name);
        Assert.Equal(SequenceText.DefaultName(sim.Texts, loaded.Sequences[0].Number), loaded.Sequences[0].Name);
    }

    /// <summary>A sequence made in the editor has a name of its own from the start, the one the screens show it by.</summary>
    [Fact]
    public void ANewSequenceIsGivenItsName()
    {
        var (sim, system) = Airlock();

        sim.Editor.NewSequence(system);
        Run(sim);

        var made = system.Sequences[system.Sequences.Count - 1];
        Assert.True(made.HasName);
        Assert.Equal(SequenceText.DefaultName(sim.Texts, made.Number), made.Name);
    }

    /// <summary>A sequence read without a name is given the one it was shown as, unless another sequence already has it.</summary>
    [Fact]
    public void AnUnnamedSequenceIsGivenTheNameItWasShownAs()
    {
        var (sim, system) = Airlock();
        sim.Editor.NewSequence(system);
        Run(sim);
        var first = system.Sequences[0];
        var second = system.Sequences[1];
        second.Name = SequenceText.DefaultName(sim.Texts, first.Number);
        first.Name = null;

        GivenNames.Give(sim.Editor.Program, sim.Texts);

        Assert.True(first.HasName);
        Assert.NotEqual(second.Name, first.Name);
    }

    static (Simulation Sim, AutomationSystem System) Airlock()
    {
        var sim = Simulation.Fresh();
        sim.Machine.Panel(30, "Port Button");
        sim.Tick();
        sim.Editor.NewButtonSystem("Airlock");
        sim.Editor.GoToRoot();
        Run(sim);
        return (sim, sim.Editor.Program.Systems[0]);
    }

    static string Installation(Simulation sim, int number) => Template.Fill(sim.Texts.SystemsInstance, "number", number.ToString());

    static string[] Names(AutomationSystem system)
    {
        var names = new string[system.Instances.Count];
        for (var i = 0; i < names.Length; i++)
            names[i] = system.Instances[i].Name ?? "";
        return names;
    }

    static void Run(Simulation sim)
    {
        for (var i = 0; i < 3; i++)
            sim.Tick();
    }
}
