using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>Logic systems thrown away from the home page, at once and back with undo, the toolbar slots their triggers took let go of.</summary>
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
        Assert.Equal("", sim.Editor.Question());
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

        // Gone at once, without a question: undo is the way back.
        sim.Editor.RootRows()[1].Deletes!.Execute();
        Assert.Equal("", sim.Editor.Question());
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
    /// A logic system's own screen has a bin too, which deletes it at once and leaves for the home screen; undo there puts it
    /// back whole, installations and all, switched off, and redo takes it out again.
    /// </summary>
    [Fact]
    public void ADeletedLogicSystemComesBackWithUndo()
    {
        var sim = Blueprinted();
        var system = sim.Editor.Program.Systems.Single();
        sim.Editor.OpenBlueprintSystem(0);
        sim.Tick();

        Assert.Equal(sim.Texts.SystemDelete, sim.Editor.Screen.DeleteAbout);
        sim.Editor.Screen.Deletes!.Execute();
        sim.Tick();

        Assert.Empty(sim.Editor.Program.Systems);
        Assert.Equal(sim.Texts.ChangeDeleteSystem, sim.Editor.Screen.UndoText);

        sim.Editor.Screen.Undo!.Execute();
        sim.Tick();

        Assert.Same(system, sim.Editor.Program.Systems.Single());
        Assert.False(system.Enabled, "what came back came back switched on");
        Assert.Equal(2, system.Instances.Count);
        Assert.Single(sim.Editor.Program.Sequences);
        Assert.True(Button(sim).Toolbar.TryGetItem(0, out _), "the button was not wired again");

        sim.Editor.Screen.Redo!.Execute();
        sim.Tick();
        Assert.Empty(sim.Editor.Program.Systems);
    }

    /// <summary>A deleted installation comes back with undo, with the blocks it had.</summary>
    [Fact]
    public void ADeletedInstallationComesBackWithUndo()
    {
        var sim = Blueprinted();
        var system = sim.Editor.Program.Systems.Single();
        var second = system.Instances[1];

        sim.Editor.RootRows()[1].Deletes!.Execute();
        sim.Tick();
        Assert.Single(system.Instances);

        sim.Editor.Screen.Undo!.Execute();
        sim.Tick();
        Assert.Equal(2, system.Instances.Count);
        Assert.Same(second, system.Instances[1]);
    }

    /// <summary>A logic system just added goes with undo, and comes back with redo.</summary>
    [Fact]
    public void AnAddedLogicSystemGoesWithUndo()
    {
        var sim = Built();
        sim.Editor.NewSystem("Hangar");
        sim.Tick();
        sim.Editor.PressPrimary();
        sim.Editor.GoToRoot();
        sim.Tick();
        Assert.Equal(2, sim.Editor.Program.Systems.Count);

        Assert.Equal(sim.Texts.ChangeAddSystem, sim.Editor.Screen.UndoText);
        sim.Editor.Screen.Undo!.Execute();
        sim.Tick();
        Assert.Single(sim.Editor.Program.Systems);

        sim.Editor.Screen.Redo!.Execute();
        sim.Tick();
        Assert.Equal(2, sim.Editor.Program.Systems.Count);
    }

    /// <summary>Undo stands down once the deleted logic system's button has been taken by another, so the button is never claimed twice.</summary>
    [Fact]
    public void UndoStandsDownWhereItsButtonWasTaken()
    {
        var sim = Built();
        sim.Editor.RootHeadings().Single().Deletes!.Execute();
        sim.Tick();
        Assert.True(sim.Editor.Screen.Undo != null && sim.Editor.CanUndoConstruct());

        sim.Editor.NewButtonSystem("Hangar");
        sim.Editor.GoToRoot();
        sim.Tick();

        Assert.NotEqual(sim.Texts.ChangeDeleteSystem, sim.Editor.Screen.UndoText);
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
