using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>Logic systems thrown away from the home page, each after asking, the toolbar slots their triggers took let go of.</summary>
public class DeletingSystemsTests
{
    /// <summary>A logic system keeps at least one installation: the only one's bin says so, and asking to delete it does nothing.</summary>
    [Fact]
    public void TheOnlyInstallationIsKept()
    {
        var sim = Built();
        var system = sim.Editor.Program.Systems.Single();
        var row = sim.Editor.RootRows().Single();

        Assert.Null(row.Deletes);
        Assert.Equal(sim.Texts.SystemsKeepsOne, row.Locked);

        sim.Editor.DeleteInstance(system, system.Instances[0]);
        Assert.Equal("", sim.Editor.Question());
        Assert.Single(system.Instances);
    }

    /// <summary>Deleting a logic system from its heading takes its installations with it, and lets go of the button its trigger was wired to.</summary>
    [Fact]
    public void DeletingItLetsGoOfItsButton()
    {
        var sim = Built();
        Assert.True(Button(sim).Toolbar.TryGetItem(0, out _), "the trigger never took the button");

        sim.Editor.RootHeadings().Single().Deletes!.Execute();
        var asking = sim.Editor.Asking!;
        asking.Box.Insert("Airlock");
        asking.Take();
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

        // Asked as the one going, and told that the blueprint and the rest are kept.
        sim.Editor.RootRows()[1].Deletes!.Execute();
        Assert.StartsWith(sim.Texts.DeleteInstance, sim.Editor.Question());
        Assert.EndsWith("The " + system.Name + " logic system and its other installations are kept. This cannot be undone.",
            sim.Editor.Question());

        sim.Editor.PressChoice(sim.Texts.DeleteConfirm);
        sim.Tick();

        Assert.Single(system.Instances);
        Assert.Same(system, sim.Editor.Program.Systems.Single());
        Assert.True(system.IsBlueprint);
    }

    /// <summary>
    /// One of two installations goes, and the one left is kept: its bin says so, and it is still there once the program is
    /// saved and read again.
    /// </summary>
    [Fact]
    public void TheLastInstallationStays()
    {
        var sim = Blueprinted();
        sim.Editor.RootRows()[1].Deletes!.Execute();
        sim.Editor.PressChoice(sim.Texts.DeleteConfirm);
        sim.Tick();

        var system = sim.Editor.Program.Systems.Single();
        Assert.Single(system.Instances);
        Assert.Null(sim.Editor.RootRows().Single().Deletes);

        var store = Store.Nowhere();
        sim.Unit.Written(out var program, out _);
        store.SetValue(World.SprocketUnit.ProgramKey, program);
        var reread = Simulation.On(store);
        reread.Tick();

        Assert.Single(reread.Editor.Program.Systems.Single().Instances);
        Assert.Single(reread.Editor.RootRows());
    }

    /// <summary>
    /// A blueprint goes only once its name is typed, whatever its case, since every logic system made from it goes too; a
    /// press on the way on before then does nothing.
    /// </summary>
    [Fact]
    public void ABlueprintGoesOnlyOnceItsNameIsTyped()
    {
        var sim = Blueprinted();
        var system = sim.Editor.Program.Systems.Single();
        sim.Editor.OpenBlueprintSystem(0);
        sim.Tick();

        Assert.Equal(sim.Texts.SystemDeleteBlueprint, sim.Editor.Screen.DeleteAbout);
        sim.Editor.Screen.Deletes!.Execute();
        var asking = sim.Editor.Asking!;

        asking.Take();
        sim.Tick();
        Assert.Same(system, sim.Editor.Program.Systems.Single());

        asking.Box.SelectAll();
        asking.Box.Insert("Airloc");
        asking.Take();
        sim.Tick();
        Assert.Same(system, sim.Editor.Program.Systems.Single());

        asking.Box.SelectAll();
        asking.Box.Insert(system.Name.ToUpperInvariant());
        asking.Take();
        sim.Tick();

        Assert.Empty(sim.Editor.Program.Systems);
        Assert.Empty(sim.Editor.RootRows());
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
        sim.Editor.GoToRoot();
        sim.Tick();

        sim.Editor.AddInstallation(0);
        sim.Tick();

        // Something done to it, switched off, so it stays when the panel is left.
        sim.Editor.Program.Systems[0].Instances[sim.Editor.Program.Systems[0].Instances.Count - 1].Enabled = false;
        sim.Editor.GoToRoot();
        sim.Tick();

        return sim;
    }

    static Panel Button(Simulation sim) =>
        sim.Panels().Single(panel => panel.Name == "Airlock Button");
}
