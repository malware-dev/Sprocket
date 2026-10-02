using Sprocket.Host.SpaceEngineers.Features.Library;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Storage;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>The world keeps a backup of every construct's automation, the player keeps blueprints for every world, and the library lists both.</summary>
public class LibraryTests
{
    static readonly DateTime Saved = new DateTime(2026, 10, 2, 14, 30, 0, DateTimeKind.Utc);

    /// <summary>Backups written out read back as they were, whatever their names and programs hold.</summary>
    [Fact]
    public void BackupsReadBackWhatTheyWrite()
    {
        var written = new Backups();
        written.Take("Hangar\nAirlock", Saved, new long[] { 118, 119 }, "sprocket 1 alpha 9\n\nsystem ...\n", "");
        written.Take("Lost Ship", Saved.AddHours(1), new long[] { 7 }, "p", "switched off\n");

        var read = new Backups();
        Assert.Equal("", read.Read(written.Write()));

        Assert.True(read.Intact);
        Assert.Equal(2, read.All.Count);
        Assert.Equal("Hangar\nAirlock", read.All[0].Name);
        Assert.Equal(Saved, read.All[0].Saved);
        Assert.Equal(new long[] { 118, 119 }, read.All[0].Grids);
        Assert.Equal("sprocket 1 alpha 9\n\nsystem ...\n", read.All[0].Program);
        Assert.Equal("switched off\n", read.All[1].Switches);
    }

    /// <summary>Backups that will not read are taken not at all, and say what was wrong.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("backups 2\n")]
    [InlineData("backups 1\nbackup 2026-10-02T14:30:00.0000000Z 1 7 4 1 0\nShip")]
    [InlineData("backups 1\nbackup tomorrow 1 7 4 1 0\nShipp\n")]
    public void BackupsThatWillNotReadAreNotTaken(string text)
    {
        var backups = new Backups();
        backups.Take("Kept", Saved, new long[] { 1 }, "p", "");

        Assert.NotEqual("", backups.Read(text));
        Assert.False(backups.Intact);
        Assert.Empty(backups.All);
    }

    /// <summary>A construct's backup is taken again, not taken twice, where any of its grids is one it had.</summary>
    [Fact]
    public void ABackupIsTakenAgainForTheSameConstruct()
    {
        var backups = new Backups();
        backups.Take("Ship", Saved, new long[] { 1, 2 }, "first", "");
        backups.Take("Ship renamed", Saved.AddHours(1), new long[] { 2, 3 }, "second", "");

        var backup = Assert.Single(backups.All);
        Assert.Equal("Ship renamed", backup.Name);
        Assert.Equal("second", backup.Program);
        Assert.Equal(new long[] { 2, 3 }, backup.Grids);
    }

    /// <summary>
    /// The library lists the blueprints on this construct, then the constructs that are lost, then the blueprints kept for
    /// every world, choosing the first.
    /// </summary>
    [Fact]
    public void TheLibraryListsThisConstructThenTheLostThenTheKept()
    {
        var rig = Blueprinted();
        rig.Shelf.Keep(rig.Editor.Program.Systems[0], Saved);
        rig.Backups.Take("Lost Ship", Saved, new long[] { 424242 }, "p", "");

        rig.Editor.GoToRoot();
        rig.Editor.OpenLibrary();
        rig.Tick();

        var page = Assert.IsType<LibraryPage>(rig.Editor.Current);
        Assert.NotNull(page.View);
        var rows = page.Rows.OfType<StatusRow>().ToList();
        Assert.Equal(new[] { "Airlock", "Lost Ship", "Airlock" }, rows.Select(r => r.Summary));
        Assert.Equal("2 installations", rows[0].Status);
        Assert.StartsWith("Lost ", rows[1].Problem);
        Assert.StartsWith("Saved ", rows[2].Status);
        Assert.True(rows[0].Chosen);
    }

    /// <summary>A library written out reads back as it was.</summary>
    [Fact]
    public void TheLibraryReadsBackWhatItWrites()
    {
        var rig = Blueprinted();
        var blueprint = rig.Editor.Program.Systems[0];
        rig.Shelf.Keep(blueprint, Saved);

        var read = new Shelf();
        Assert.Equal("", read.Read(rig.Shelf.Write()));

        var volume = Assert.Single(read.All);
        Assert.Equal(blueprint.Identity, volume.Identity);
        Assert.Equal("Airlock", volume.Name);
        Assert.Equal(Saved, volume.Saved);
        Assert.Equal(rig.Shelf.All[0].Text, volume.Text);
        Assert.True(read.Holds(blueprint));
    }

    /// <summary>A library that will not read is taken not at all, and says what was wrong.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("library 2\n")]
    [InlineData("library 1\nvolume not-a-guid 2026-10-02T14:30:00.0000000Z 0 1 1\nAb\n")]
    [InlineData("library 1\nvolume 3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43 2026-10-02T14:30:00.0000000Z 0 1 5\nAb\n")]
    public void ALibraryThatWillNotReadIsNotTaken(string text)
    {
        var shelf = new Shelf();
        Assert.NotEqual("", shelf.Read(text));
        Assert.False(shelf.Intact);
        Assert.Empty(shelf.All);
    }

    /// <summary>A blueprint put on a construct from the library arrives as it was kept, with no installation, and only once.</summary>
    [Fact]
    public void ABlueprintFromTheLibraryArrivesWithNoInstallation()
    {
        var made = Blueprinted();
        var blueprint = made.Editor.Program.Systems[0];
        made.Shelf.Keep(blueprint, Saved);

        var rig = Simulation.Fresh();
        rig.Shelf.Read(made.Shelf.Write());
        var volume = rig.Shelf.All[0];

        Assert.Equal("", rig.Editor.PutOn(volume));

        var arrived = Assert.Single(rig.Editor.Program.Systems);
        Assert.True(arrived.IsBlueprint);
        Assert.Equal(blueprint.Identity, arrived.Identity);
        Assert.Empty(arrived.Instances);
        Assert.Equal(blueprint.Parts.Select(p => p.Id), arrived.Parts.Select(p => p.Id));
        Assert.Equal(blueprint.Sequences.Count, arrived.Sequences.Count);

        Assert.NotEqual("", rig.Editor.PutOn(volume));
        Assert.Single(rig.Editor.Program.Systems);
    }

    /// <summary>
    /// A blueprint comes off the construct only once nothing there uses it, and only where storage has a copy, which is brought
    /// up to date as it goes.
    /// </summary>
    [Fact]
    public void ABlueprintTakenOffBringsItsStoredCopyUpToDate()
    {
        var rig = Blueprinted();
        var blueprint = rig.Editor.Program.Systems[0];
        Unused(rig, blueprint);

        rig.Editor.TakeOff(blueprint);
        Assert.Single(rig.Editor.Program.Systems);
        Assert.Empty(rig.Shelf.All);

        rig.Shelf.Keep(blueprint, Saved);
        blueprint.Name = "Airlock Mk II";
        rig.Editor.TakeOff(blueprint);

        Assert.Empty(rig.Editor.Program.Systems);
        Assert.Equal("Airlock Mk II", Assert.Single(rig.Shelf.All).Name);
    }

    /// <summary>
    /// The home screen offers a blueprint nothing uses a minus where storage keeps a copy, which asks before taking it off,
    /// and a bin that deletes it, by its typed name, where this is the only copy.
    /// </summary>
    [Fact]
    public void TheHomeScreenOffersAMinusOnlyWhereStorageKeepsACopy()
    {
        var rig = Blueprinted();
        var blueprint = rig.Editor.Program.Systems[0];
        Unused(rig, blueprint);

        var heading = Assert.Single(rig.Editor.RootHeadings());
        Assert.Null(heading.BinMark);
        Assert.Equal(rig.Texts.LibraryDeletes, heading.DeleteText);
        heading.Deletes!.Execute();
        Assert.NotNull(rig.Editor.Asking);
        rig.Editor.Unask();

        rig.Shelf.Keep(blueprint, Saved);
        rig.Editor.Invalidate();
        rig.Tick();

        heading = Assert.Single(rig.Editor.RootHeadings());
        Assert.Equal(Symbols.Minus, heading.BinMark);
        Assert.Equal(rig.Texts.LibraryTakeOff, heading.DeleteText);
        heading.Deletes!.Execute();
        Assert.StartsWith(rig.Texts.LibraryTakeOffQuestion, rig.Editor.Question());
        rig.Editor.PressChoice(rig.Texts.LibraryTakeOffKeep);
        rig.Tick();
        Assert.Single(rig.Editor.Program.Systems);

        heading = Assert.Single(rig.Editor.RootHeadings());
        heading.Deletes!.Execute();
        rig.Editor.PressChoice(rig.Texts.LibraryTakeOffConfirm);
        rig.Tick();

        Assert.Empty(rig.Editor.Program.Systems);
    }

    /// <summary>A copy of a blueprint is a blueprint of its own: a new identity, the next free name, and no installation.</summary>
    [Fact]
    public void ACopyOfABlueprintIsABlueprintOfItsOwn()
    {
        var rig = Blueprinted();
        var blueprint = rig.Editor.Program.Systems[0];
        rig.Shelf.Keep(blueprint, Saved);

        var copy = rig.Editor.Copy(ProgramText.Blueprint(blueprint), out var trouble);

        Assert.Equal("", trouble);
        Assert.NotNull(copy);
        Assert.NotEqual(blueprint.Identity, copy.Identity);
        Assert.Equal("Airlock 2", copy.Name);
        Assert.True(copy.IsBlueprint);
        Assert.Empty(copy.Instances);
        Assert.Equal(2, blueprint.Instances.Count);
        Assert.Equal("Airlock 3", rig.Editor.Copy(ProgramText.Blueprint(blueprint), out trouble)!.Name);
    }

    /// <summary>A construct with no automation leaves no backup.</summary>
    [Fact]
    public void AConstructWithNoAutomationTakesNoBackup()
    {
        var rig = Simulation.Fresh();
        rig.BackUp(Saved);

        Assert.Empty(rig.Backups.All);
    }

    /// <summary>The last programmable block ground off, the automation it held is backed up as it goes.</summary>
    [Fact]
    public void LosingTheLastProgrammableBlockBacksTheAutomationUp()
    {
        var rig = Simulation.Fresh();
        rig.Editor.NewButtonSystem("Airlock");
        rig.Tick();

        rig.Machine.Grind(rig.Machine.Sprocket);
        rig.Tick();

        Assert.True(rig.Unit.Lost);
        var backup = Assert.Single(rig.Backups.All);
        Assert.Contains("Airlock", backup.Program);
    }

    /// <summary>Takes every installation of <paramref name="blueprint" /> out, leaving the home screen showing it unused.</summary>
    static void Unused(Simulation rig, AutomationSystem blueprint)
    {
        while (blueprint.Instances.Count > 0)
            rig.Editor.TakeOut(blueprint, blueprint.Instances[0]);

        rig.Editor.GoToRoot();
        rig.Tick();
    }

    /// <summary>A logic system with a button, made a blueprint, with a second installation made from it.</summary>
    static Simulation Blueprinted()
    {
        var sim = Simulation.Fresh();
        sim.Editor.NewButtonSystem("Airlock");
        sim.Editor.GoToRoot();
        sim.Tick();

        sim.Editor.RootRows()[0].Offers!.Execute();
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
}
