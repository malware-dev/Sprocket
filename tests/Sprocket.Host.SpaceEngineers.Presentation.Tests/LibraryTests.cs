using Sprocket.Host.SpaceEngineers.Features.Library;
using Sprocket.Host.SpaceEngineers.Features.Shell;
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
        written.Take("Hangar\nAirlock", Saved, new long[] { 118, 119 }, "sprocket 1 alpha 10\n\nsystem ...\n", "");
        written.Take("Lost Ship", Saved.AddHours(1), new long[] { 7 }, "p", "switched off\n");

        var read = new Backups();
        Assert.Equal("", read.Read(written.Write()));

        Assert.True(read.Intact);
        Assert.Equal(2, read.All.Count);
        Assert.Equal("Hangar\nAirlock", read.All[0].Name);
        Assert.Equal(Saved, read.All[0].Saved);
        Assert.Equal(new long[] { 118, 119 }, read.All[0].Grids);
        Assert.Equal("sprocket 1 alpha 10\n\nsystem ...\n", read.All[0].Program);
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
        rig.Editor.Program.Systems[0].Changed = Saved;
        rig.Shelf.Keep(rig.Editor.Program.Systems[0]);
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
        Assert.StartsWith("Changed ", rows[2].Status);
        Assert.True(rows[0].Chosen);
    }

    /// <summary>
    /// A logic system's heading offers to make its blueprint, offers nothing while the library's copy is the same, and offers
    /// to update it once the logic system has changed.
    /// </summary>
    [Fact]
    public void TheHeadingMakesAndUpdatesTheBlueprint()
    {
        var rig = Blueprinted();
        var system = rig.Editor.Program.Systems[0];
        var heading = Assert.Single(rig.Editor.RootHeadings());
        Assert.Equal(rig.Texts.SystemsMakeBlueprint, heading.ButtonText);

        heading.Offers!.Execute();
        rig.Tick();
        Assert.True(rig.Shelf.Holds(system));
        Assert.Null(Assert.Single(rig.Editor.RootHeadings()).Offers);

        system.Name = "Airlock Mk II";
        rig.Editor.Invalidate();
        rig.Tick();
        heading = Assert.Single(rig.Editor.RootHeadings());
        Assert.Equal(rig.Texts.SystemsUpdateBlueprint, heading.ButtonText);
        Assert.NotNull(heading.Offers);
    }

    /// <summary>A blueprint put in the library is a file of its own holding the blueprint as written, named in the index, and reads back.</summary>
    [Fact]
    public void TheLibraryReadsBackWhatItWrites()
    {
        var rig = Blueprinted();
        var blueprint = rig.Editor.Program.Systems[0];
        blueprint.Changed = Saved;
        rig.Shelf.Keep(blueprint);

        Assert.Equal("sprocket-bp-Airlock.cog\n", rig.Folder.Files["sprocket-bp.index"]);
        Assert.Equal(ProgramText.Blueprint(blueprint), rig.Folder.Files["sprocket-bp-Airlock.cog"]);

        var read = new Shelf(rig.Folder, delegate { });
        Assert.Equal("", read.Read());

        var volume = Assert.Single(read.All);
        Assert.Equal(VolumeState.Read, volume.State);
        Assert.Equal(blueprint.Identity, volume.Identity);
        Assert.Equal("Airlock", volume.Name);
        Assert.Equal(Saved, volume.Changed);
        Assert.True(read.Holds(blueprint));
    }

    /// <summary>A line of the index that gives no blueprint is listed with why, and the lines after it are read all the same.</summary>
    [Theory]
    [InlineData("gone.cog", VolumeState.Missing)]
    [InlineData("Blueprints/given.cog", VolumeState.Path)]
    [InlineData("..\\given.cog", VolumeState.Path)]
    [InlineData("C:given.cog", VolumeState.Path)]
    [InlineData("junk.doot", VolumeState.Unreadable)]
    [InlineData("sprocket-bp-Airlock.cog", VolumeState.Twice)]
    public void ALineThatGivesNoBlueprintSaysWhy(string line, VolumeState state)
    {
        var rig = Blueprinted();
        rig.Shelf.Keep(rig.Editor.Program.Systems[0]);
        rig.Folder.Write("junk.doot", "not a blueprint");
        rig.Folder.Write("sprocket-bp.index", "sprocket-bp-Airlock.cog\n" + line + "\n");

        var read = new Shelf(rig.Folder, delegate { });
        Assert.NotEqual("", read.Read());

        Assert.True(read.Intact);
        Assert.Equal(new[] { VolumeState.Read, state }, read.All.Select(v => v.State));
        Assert.Equal(line, read.All[1].File);
    }

    /// <summary>The library lists a line that gave no blueprint by its file's name, saying why.</summary>
    [Fact]
    public void TheLibraryListsALineThatGaveNoBlueprintByItsFile()
    {
        var rig = Blueprinted();
        rig.Folder.Write("sprocket-bp.index", "gone.cog\n");
        rig.Shelf.Read();

        rig.Editor.GoToRoot();
        rig.Editor.OpenLibrary();
        rig.Tick();

        var page = Assert.IsType<LibraryPage>(rig.Editor.Current);
        Assert.NotNull(page.View);
        var row = page.Rows.OfType<StatusRow>().Last();
        Assert.Equal("gone.cog", row.Summary);
        Assert.Equal(rig.Texts.LibraryMissing, row.Problem);
    }

    /// <summary>A file a player named and listed reads, keeps its name when updated, and leaves their comments where they were.</summary>
    [Fact]
    public void APlayersOwnFileKeepsItsNameAndTheirCommentsStay()
    {
        var rig = Blueprinted();
        var blueprint = rig.Editor.Program.Systems[0];
        rig.Folder.Write("stoopid.doot", ProgramText.Blueprint(blueprint));
        rig.Folder.Write("sprocket-bp.index", "# from a friend\r\n\r\n  stoopid.doot  \r\n");
        Assert.Equal("", rig.Shelf.Read());

        blueprint.Name = "Airlock Mk II";
        rig.Shelf.Keep(blueprint);

        Assert.Equal("# from a friend\r\n\r\n  stoopid.doot  \r\n", rig.Folder.Files["sprocket-bp.index"]);
        Assert.Contains("Airlock Mk II", rig.Folder.Files["stoopid.doot"]);
        Assert.Equal("Airlock Mk II", Assert.Single(rig.Shelf.All).Name);
    }

    /// <summary>A file the library named after a blueprint is renamed with it.</summary>
    [Fact]
    public void ALibraryFileIsRenamedWithItsBlueprint()
    {
        var rig = Blueprinted();
        var blueprint = rig.Editor.Program.Systems[0];
        rig.Shelf.Keep(blueprint);

        blueprint.Name = "Airlock Mk II";
        rig.Shelf.Keep(blueprint);

        Assert.False(rig.Folder.Has("sprocket-bp-Airlock.cog"));
        Assert.True(rig.Folder.Has("sprocket-bp-Airlock Mk II.cog"));
        Assert.Equal("sprocket-bp-Airlock Mk II.cog\n", rig.Folder.Files["sprocket-bp.index"]);
    }

    /// <summary>A new file's name holds nothing a file name cannot, and is numbered where the name is taken.</summary>
    [Fact]
    public void ANewFileNameIsCleanedAndNumbered()
    {
        var rig = Blueprinted();
        var blueprint = rig.Editor.Program.Systems[0];
        blueprint.Name = "Air: lock?";
        rig.Folder.Write("sprocket-bp-Air_ lock_.cog", "somebody else's");

        rig.Shelf.Keep(blueprint);

        Assert.Equal("sprocket-bp-Air_ lock_ 2.cog", Assert.Single(rig.Shelf.All).File);
        Assert.Equal("somebody else's", rig.Folder.Files["sprocket-bp-Air_ lock_.cog"]);
    }

    /// <summary>Forgetting a blueprint deletes its file and its line; forgetting a line that gave none leaves the file it names alone.</summary>
    [Fact]
    public void ForgettingDeletesTheFileOnlyWhereItHeldABlueprint()
    {
        var rig = Blueprinted();
        rig.Shelf.Keep(rig.Editor.Program.Systems[0]);
        rig.Folder.Write("DefenseShields.cfg", "[shields]");
        rig.Folder.Write("sprocket-bp.index", "# mine\nsprocket-bp-Airlock.cog\nDefenseShields.cfg\n");
        rig.Shelf.Read();

        rig.Shelf.Forget(rig.Shelf.All[1]);
        rig.Shelf.Forget(rig.Shelf.All[0]);

        Assert.Equal("# mine\n", rig.Folder.Files["sprocket-bp.index"]);
        Assert.False(rig.Folder.Has("sprocket-bp-Airlock.cog"));
        Assert.Equal("[shields]", rig.Folder.Files["DefenseShields.cfg"]);
    }

    /// <summary>A blueprint put on a construct from the library arrives as it was kept, as a logic system with its first installation, and only once.</summary>
    [Fact]
    public void ABlueprintFromTheLibraryArrivesWithItsFirstInstallation()
    {
        var made = Blueprinted();
        var blueprint = made.Editor.Program.Systems[0];
        made.Shelf.Keep(blueprint);

        var rig = Simulation.Fresh();
        foreach (var file in made.Folder.Files)
            rig.Folder.Write(file.Key, file.Value);

        rig.Shelf.Read();
        var volume = rig.Shelf.All[0];

        Assert.Equal("", rig.Editor.PutOn(volume));

        var arrived = Assert.Single(rig.Editor.Program.Systems);
        Assert.True(arrived.IsBlueprint);
        Assert.Equal(blueprint.Identity, arrived.Identity);
        Assert.Equal("Installation 1", Assert.Single(arrived.Instances).Name);
        Assert.Equal(blueprint.Parts.Select(p => p.Id), arrived.Parts.Select(p => p.Id));
        Assert.Equal(blueprint.Sequences.Count, arrived.Sequences.Count);

        Assert.NotEqual("", rig.Editor.PutOn(volume));
        Assert.Single(rig.Editor.Program.Systems);
    }

    /// <summary>
    /// A logic system comes off the construct, installations and all, only where the library has its blueprint, which is
    /// brought up to date as it goes.
    /// </summary>
    [Fact]
    public void ALogicSystemTakenOffBringsItsBlueprintUpToDate()
    {
        var rig = Blueprinted();
        var blueprint = rig.Editor.Program.Systems[0];
        Assert.Equal(2, blueprint.Instances.Count);

        rig.Editor.TakeOff(blueprint);
        Assert.Single(rig.Editor.Program.Systems);
        Assert.Empty(rig.Shelf.All);

        rig.Shelf.Keep(blueprint);
        blueprint.Name = "Airlock Mk II";
        rig.Editor.TakeOff(blueprint);

        Assert.Empty(rig.Editor.Program.Systems);
        Assert.Equal("Airlock Mk II", Assert.Single(rig.Shelf.All).Name);
    }

    /// <summary>
    /// The home screen offers a logic system a minus where the library has its blueprint, which asks before taking it off with
    /// its installations, and a bin that deletes it, by its typed name, where this is the only copy.
    /// </summary>
    [Fact]
    public void TheHomeScreenOffersAMinusOnlyWhereTheLibraryHasItsBlueprint()
    {
        var rig = Blueprinted();
        var blueprint = rig.Editor.Program.Systems[0];

        var heading = Assert.Single(rig.Editor.RootHeadings());
        Assert.Null(heading.BinMark);
        Assert.Equal(rig.Texts.LibraryDeletes, heading.DeleteText);
        heading.Deletes!.Execute();
        Assert.StartsWith(rig.Texts.DeleteSystem, rig.Editor.Question());
        rig.Editor.Unask();

        rig.Shelf.Keep(blueprint);
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

        // Taken off, it comes back with undo; the library's copy stays either way.
        Assert.Equal(rig.Texts.ChangeTakeOffSystem, rig.Editor.Screen.UndoText);
        rig.Editor.Screen.Undo!.Execute();
        rig.Tick();
        Assert.Same(blueprint, Assert.Single(rig.Editor.Program.Systems));
        Assert.True(rig.Shelf.Holds(blueprint));
    }

    /// <summary>A copy is a logic system of its own: a new identity, the next free name, and its first installation.</summary>
    [Fact]
    public void ACopyIsALogicSystemOfItsOwn()
    {
        var rig = Blueprinted();
        var blueprint = rig.Editor.Program.Systems[0];
        rig.Shelf.Keep(blueprint);

        var copy = rig.Editor.Copy(ProgramText.Blueprint(blueprint), out var trouble);

        Assert.Equal("", trouble);
        Assert.NotNull(copy);
        Assert.NotEqual(blueprint.Identity, copy.Identity);
        Assert.Equal("Airlock 2", copy.Name);
        Assert.True(copy.IsBlueprint);
        Assert.Equal("Installation 1", Assert.Single(copy.Instances).Name);
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

    /// <summary>A logic system with a button, made a blueprint, with a second installation made from it.</summary>
    static Simulation Blueprinted()
    {
        var sim = Simulation.Fresh();
        sim.Editor.NewButtonSystem("Airlock");
        sim.Editor.GoToRoot();
        sim.Tick();

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
