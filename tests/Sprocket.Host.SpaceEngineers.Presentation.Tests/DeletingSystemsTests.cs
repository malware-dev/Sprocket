using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>Logic systems thrown away from the home page, each after asking, the toolbar slots their triggers took let go of.</summary>
public class DeletingSystemsTests
{
    /// <summary>A logic system's bin asks first, and keeping it keeps everything.</summary>
    [Fact]
    public void KeepingItKeepsIt()
    {
        var sim = Built();

        sim.Editor.RootRows()[0].Deletes!.Execute();
        Assert.Equal(sim.Texts.DeleteSystem, sim.Editor.Screen.Title());

        sim.Editor.PressChoice(sim.Texts.DeleteKeep);
        sim.Tick();

        Assert.Single(sim.Editor.Program.Systems);
        Assert.Single(sim.Editor.RootRows());
    }

    /// <summary>Deleting a logic system takes it out with its sequences, and lets go of the button its trigger was wired to.</summary>
    [Fact]
    public void DeletingItLetsGoOfItsButton()
    {
        var sim = Built();
        Assert.True(Button(sim).Toolbar.TryGetItem(0, out _), "the trigger never took the button");

        sim.Editor.RootRows()[0].Deletes!.Execute();
        sim.Editor.PressChoice(sim.Texts.DeleteConfirm);
        for (var i = 0; i < 5; i++)
            sim.Tick();

        Assert.Empty(sim.Editor.Program.Systems);
        Assert.Empty(sim.Editor.Program.Sequences);
        Assert.Empty(sim.Editor.RootRows());
        Assert.False(Button(sim).Toolbar.TryGetItem(0, out _), "the button is still wired to what was deleted");
    }

    /// <summary>One of several made from a blueprint goes alone, and the blueprint stays with the rest.</summary>
    [Fact]
    public void OneMadeFromABlueprintGoesAlone()
    {
        var sim = Blueprinted();
        var system = sim.Editor.Program.Systems.Single();
        Assert.Equal(2, system.Instances.Count);

        sim.Editor.RootRows()[1].Deletes!.Execute();
        Assert.Equal(sim.Texts.DeleteFinal, Warning(sim));

        sim.Editor.PressChoice(sim.Texts.DeleteConfirm);
        sim.Tick();

        Assert.Single(system.Instances);
        Assert.Same(system, sim.Editor.Program.Systems.Single());
        Assert.True(system.IsBlueprint);
    }

    /// <summary>
    /// The blueprint stays when the last one made from it goes: still a blueprint, still offering another, and still
    /// one once the program is saved and read again.
    /// </summary>
    [Fact]
    public void TheBlueprintStaysWithNoneMadeFromIt()
    {
        var sim = Blueprinted();
        for (var i = 0; i < 2; i++)
        {
            sim.Editor.RootRows()[0].Deletes!.Execute();
            sim.Editor.PressChoice(sim.Texts.DeleteConfirm);
            sim.Tick();
        }

        var system = sim.Editor.Program.Systems.Single();
        Assert.Empty(system.Instances);
        Assert.True(system.IsBlueprint);
        Assert.True(sim.Editor.RootRows().Single().IsAction, "the way to add another is gone");

        var store = Store.Nowhere();
        sim.Unit.Written(out var program, out _);
        store.SetValue(World.SprocketUnit.ProgramKey, program);
        var reread = Simulation.On(store);
        reread.Tick();

        Assert.True(reread.Editor.Program.Systems.Single().IsBlueprint);
        Assert.True(reread.Editor.RootRows().Single().IsAction, "the blueprint came back as something else");
    }

    /// <summary>A lent editor, which may only look, offers no bin.</summary>
    [Fact]
    public void AnEditorThatMayOnlyLookOffersNoBin()
    {
        var sim = Built();

        var store = Store.Nowhere();
        sim.Unit.Written(out var program, out _);
        store.SetValue(World.SprocketUnit.ProgramKey, program);
        var lent = Simulation.On(store);
        lent.Editor.Holder = "Bert";
        lent.Tick();

        Assert.Null(lent.Editor.RootRows().Single().Deletes);
    }

    /// <summary>A logic system started by the airlock's button, and the home page shown again.</summary>
    static Simulation Built()
    {
        var sim = Simulation.Fresh();
        sim.Editor.NewButtonSystem("Airlock");
        sim.Editor.GoToRoot();
        for (var i = 0; i < 5; i++)
            sim.Tick();

        return sim;
    }

    /// <summary>The same made a blueprint, with a second logic system made from it.</summary>
    static Simulation Blueprinted()
    {
        var sim = Built();
        sim.Editor.RootRows()[0].Offers!.Execute();
        sim.Editor.GoToRoot();
        sim.Tick();

        var add = sim.Editor.RootRows().Single(row => row.IsAction);
        add.Opens!.Execute();
        sim.Editor.GoToRoot();
        sim.Tick();

        return sim;
    }

    static string Warning(Simulation sim) =>
        sim.Editor.Choices().Last().Caution;

    static Panel Button(Simulation sim) =>
        sim.Panels().Single(panel => panel.Name == "Airlock Button");
}
