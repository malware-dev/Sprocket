using Lumel.Controls;
using Sprocket.Host.SpaceEngineers.Features.Library;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Storage;
using Sprocket.Host.SpaceEngineers.World;
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
        written.Take("Hangar\nAirlock", Saved, new long[] { 118, 119 }, "sprocket 1 alpha 11\n\nsystem ...\n");
        written.Take("Lost Ship", Saved.AddHours(1), new long[] { 7 }, "p");

        var read = new Backups();
        Assert.Equal("", read.Read(written.Write()));

        Assert.True(read.Intact);
        Assert.Equal(2, read.All.Count);
        Assert.Equal("Hangar\nAirlock", read.All[0].Name);
        Assert.Equal(Saved, read.All[0].Saved);
        Assert.Equal(new long[] { 118, 119 }, read.All[0].Grids);
        Assert.Equal("sprocket 1 alpha 11\n\nsystem ...\n", read.All[0].Program);
        Assert.Equal("p", read.All[1].Program);
    }

    /// <summary>Backups that will not read are taken not at all, and say what was wrong.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("backups 2\n")]
    [InlineData("backups 1\nbackup 2026-10-02T14:30:00.0000000Z 1 7 4 1\nShip")]
    [InlineData("backups 1\nbackup tomorrow 1 7 4 1\nShipp\n")]
    public void BackupsThatWillNotReadAreNotTaken(string text)
    {
        var backups = new Backups();
        backups.Take("Kept", Saved, new long[] { 1 }, "p");

        Assert.NotEqual("", backups.Read(text));
        Assert.False(backups.Intact);
        Assert.Empty(backups.All);
    }

    /// <summary>A construct's backup is taken again, not taken twice, where any of its grids is one it had.</summary>
    [Fact]
    public void ABackupIsTakenAgainForTheSameConstruct()
    {
        var backups = new Backups();
        backups.Take("Ship", Saved, new long[] { 1, 2 }, "first");
        backups.Take("Ship renamed", Saved.AddHours(1), new long[] { 2, 3 }, "second");

        var backup = Assert.Single(backups.All);
        Assert.Equal("Ship renamed", backup.Name);
        Assert.Equal("second", backup.Program);
        Assert.Equal(new long[] { 2, 3 }, backup.Grids);
    }

    /// <summary>A lost construct whose every logic system has its equal in the library leaves no backup: the library has it all.</summary>
    [Fact]
    public void ALostConstructTheLibraryHoldsLeavesNoBackup()
    {
        var rig = Blueprinted();
        rig.Editor.Stamp(true);
        rig.Shelf.Keep(rig.Editor.Program.Systems[0]);

        rig.Unit.BackUp(rig.Backups, Saved, rig.Shelf);

        Assert.Empty(rig.Backups.All);
    }

    /// <summary>A lost construct's backup holds only the logic systems the library has no equal of, without their installations.</summary>
    [Fact]
    public void ALostConstructsBackupHoldsOnlyWhatTheLibraryLacks()
    {
        var rig = Blueprinted();
        var kept = rig.Editor.Program.Systems[0];
        rig.Editor.NewSystem("Hangar");
        rig.Editor.GoToRoot();
        rig.Tick();
        rig.Editor.Stamp(true);
        rig.Shelf.Keep(kept);

        rig.Unit.BackUp(rig.Backups, Saved, rig.Shelf);

        var backup = Assert.Single(rig.Backups.All);
        var read = new Automation();
        Assert.Equal("", ProgramText.Load(backup.Program, read, new Values()));
        var system = Assert.Single(read.Systems);
        Assert.Equal("Hangar", system.Name);
        Assert.Empty(system.Instances);
    }

    /// <summary>The library lists a lost construct whose grids are still in the world: a backup is only taken as the automation is lost.</summary>
    [Fact]
    public void TheLibraryListsALostConstructWhoseGridsRemain()
    {
        var rig = Blueprinted();
        rig.Unit.BackUp(rig.Backups, Saved, rig.Shelf);
        Assert.Single(rig.Backups.All);

        rig.Editor.GoToRoot();
        rig.Editor.OpenLibrary();
        rig.Tick();

        var page = Assert.IsType<LibraryPage>(rig.Editor.Current);
        Assert.NotNull(page.View);
        Assert.Contains(page.Rows.OfType<StatusRow>(), r => r.Problem != null && r.Problem.StartsWith("Lost "));
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
        rig.Backups.Take("Lost Ship", Saved, new long[] { 424242 }, "p");

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
        rig.Editor.GoToRoot();
        rig.Tick();
        Assert.Null(Assert.Single(rig.Editor.RootHeadings()).Offers);

        // Renamed, and changed after its blueprint, as an edit on the screen stamps it.
        system.Name = "Airlock Mk II";
        system.Changed = DateTime.UtcNow.AddMinutes(1);
        rig.Editor.Invalidate();
        rig.Tick();
        heading = Assert.Single(rig.Editor.RootHeadings());
        Assert.Equal(rig.Texts.SystemsUpdateBlueprint, heading.ButtonText);
        Assert.NotNull(heading.Offers);
    }

    /// <summary>Making a blueprint from the home screen opens the library with that blueprint chosen.</summary>
    [Fact]
    public void MakingABlueprintOpensTheLibraryAtIt()
    {
        var rig = Blueprinted();
        var copy = rig.Editor.Copy(ProgramText.Blueprint(rig.Editor.Program.Systems[0]), out var trouble);
        Assert.True(copy != null, trouble);
        rig.Editor.GoToRoot();
        rig.Tick();

        var made = rig.Editor.RootHeadings().Single(h => h.Summary == copy.Name);
        made.Offers!.Execute();
        rig.Tick();

        var page = Assert.IsType<LibraryPage>(rig.Editor.Current);
        Assert.NotNull(page.View);
        var chosen = Assert.Single(page.Rows.OfType<StatusRow>(), r => r.Chosen);
        Assert.Equal(copy.Name, chosen.Summary);
        Assert.NotEqual("Airlock", chosen.Summary);
    }

    /// <summary>Where the library's blueprint was changed after the logic system, the home screen and the library offer to update the logic system from it.</summary>
    [Fact]
    public void ANewerBlueprintIsOfferedToTheLogicSystem()
    {
        var rig = Newer();

        var heading = Assert.Single(rig.Editor.RootHeadings());
        Assert.Equal(rig.Texts.SystemsUpdateFromLibrary, heading.ButtonText);
        Assert.NotNull(heading.Offers);

        rig.Editor.OpenLibrary();
        rig.Tick();
        var page = Assert.IsType<LibraryPage>(rig.Editor.Current);
        Assert.NotNull(page.View);
        var update = Assert.Single(page.View!.Within().OfType<Button>(), b => b.Text == rig.Texts.SystemsUpdateFromLibrary);
        Assert.True(update.Command!.CanExecute);
    }

    /// <summary>
    /// Updating a logic system from the library asks first, then puts the blueprint in its place: its installations stay, with
    /// their blocks and their switches, and the update can be undone.
    /// </summary>
    [Fact]
    public void UpdatingFromTheLibraryKeepsTheInstallations()
    {
        var rig = Newer();
        var was = rig.Editor.Program.Systems[0];
        var filled = was.Instances.Select(i => string.Join(",", i.Filled.OrderBy(s => s))).ToList();
        var switched = was.Instances.Select(i => i.Enabled).ToList();

        Assert.Single(rig.Editor.RootHeadings()).Offers!.Execute();
        rig.Tick();
        Assert.Contains(rig.Texts.LibraryRenewReplaced, rig.Editor.Question());
        rig.Editor.PressChoice(rig.Texts.LibraryRenewConfirm);
        rig.Tick();

        var now = Assert.Single(rig.Editor.Program.Systems);
        Assert.Equal("Airlock Mk II", now.Name);
        Assert.Equal(was.Identity, now.Identity);
        Assert.Equal(filled, now.Instances.Select(i => string.Join(",", i.Filled.OrderBy(s => s))).ToList());
        Assert.Equal(switched, now.Instances.Select(i => i.Enabled).ToList());
        Assert.True(rig.Shelf.Holds(now));
        Assert.Null(Assert.Single(rig.Editor.RootHeadings()).Offers);

        rig.Editor.Screen.Undo!.Execute();
        rig.Tick();
        Assert.Equal("Airlock", Assert.Single(rig.Editor.Program.Systems).Name);
        Assert.Equal(rig.Texts.SystemsUpdateFromLibrary, Assert.Single(rig.Editor.RootHeadings()).ButtonText);
    }

    /// <summary>Updating a blueprint asks first, and keeping the blueprint leaves it as it was.</summary>
    [Fact]
    public void UpdatingABlueprintAsksFirst()
    {
        var rig = Blueprinted();
        var system = rig.Editor.Program.Systems[0];
        system.Changed = Saved;
        rig.Shelf.Keep(system);

        system.Name = "Airlock Mk II";
        system.Changed = Saved.AddDays(1);
        rig.Editor.Invalidate();
        rig.Tick();

        Assert.Single(rig.Editor.RootHeadings()).Offers!.Execute();
        rig.Tick();
        Assert.Contains(rig.Texts.LibraryUpdateReplaced, rig.Editor.Question());
        rig.Editor.PressChoice(rig.Texts.LibraryUpdateKeep);
        rig.Tick();
        Assert.Equal("Airlock", Assert.Single(rig.Shelf.All).Name);

        Assert.Single(rig.Editor.RootHeadings()).Offers!.Execute();
        rig.Tick();
        rig.Editor.PressChoice(rig.Texts.LibraryUpdateConfirm);
        rig.Tick();
        Assert.Equal("Airlock Mk II", Assert.Single(rig.Shelf.All).Name);
    }

    /// <summary>A library with no index makes one as it is read, saying how it is used, and lists what it keeps below that.</summary>
    [Fact]
    public void TheLibraryMakesItsIndexWhereThereIsNone()
    {
        var rig = Blueprinted();
        Assert.False(rig.Folder.Has("sprocket-bp.index"));

        Assert.Equal("", rig.Shelf.Read());
        var made = rig.Folder.Files["sprocket-bp.index"];
        Assert.StartsWith("# Blueprints for the automation library:", made);
        Assert.Empty(rig.Shelf.All);

        rig.Shelf.Keep(rig.Editor.Program.Systems[0]);
        Assert.Equal(made + "sprocket-bp-Airlock.cog\n", rig.Folder.Files["sprocket-bp.index"]);
        Assert.Single(rig.Shelf.All);
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
    /// brought up to date only when asked to.
    /// </summary>
    [Fact]
    public void ALogicSystemTakenOffUpdatesItsBlueprintOnlyWhenAsked()
    {
        var rig = Blueprinted();
        var blueprint = rig.Editor.Program.Systems[0];
        Assert.Equal(2, blueprint.Instances.Count);

        rig.Editor.TakeOff(blueprint, true);
        Assert.Single(rig.Editor.Program.Systems);
        Assert.Empty(rig.Shelf.All);

        rig.Shelf.Keep(blueprint);
        blueprint.Name = "Airlock Mk II";
        rig.Editor.TakeOff(blueprint, false);
        Assert.Empty(rig.Editor.Program.Systems);
        Assert.Equal("Airlock", Assert.Single(rig.Shelf.All).Name);

        rig.Editor.Screen.Undo!.Execute();
        rig.Tick();
        rig.Editor.TakeOff(blueprint, true);
        Assert.Equal("Airlock Mk II", Assert.Single(rig.Shelf.All).Name);
    }

    /// <summary>
    /// Taking off a logic system changed after its blueprint in the library asks whether to bring the blueprint up to date,
    /// and answering to leave it takes the logic system off with the blueprint as it was.
    /// </summary>
    [Fact]
    public void TakingOffAChangedLogicSystemAsksAboutItsBlueprint()
    {
        var rig = Blueprinted();
        var blueprint = rig.Editor.Program.Systems[0];
        blueprint.Changed = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        rig.Shelf.Keep(blueprint);

        blueprint.Name = "Airlock Mk II";
        rig.Editor.AskTakeOff(blueprint);

        Assert.Contains(rig.Texts.LibraryTakeOffChanged, rig.Editor.Question());
        Assert.True(rig.Editor.HasChoice(rig.Texts.LibraryTakeOffUpdate));
        rig.Editor.PressChoice(rig.Texts.LibraryTakeOffLeave);
        rig.Tick();

        Assert.Empty(rig.Editor.Program.Systems);
        Assert.Equal("Airlock", Assert.Single(rig.Shelf.All).Name);
    }

    /// <summary>Taking off a logic system unchanged since its blueprint asks only whether to take it off, nothing about the blueprint.</summary>
    [Fact]
    public void TakingOffAnUnchangedLogicSystemAsksNothingAboutItsBlueprint()
    {
        var rig = Blueprinted();
        var blueprint = rig.Editor.Program.Systems[0];
        rig.Editor.Stamp(true);
        rig.Shelf.Keep(blueprint);

        rig.Editor.AskTakeOff(blueprint);

        Assert.Contains(rig.Texts.LibraryTakeOffKept, rig.Editor.Question());
        Assert.False(rig.Editor.HasChoice(rig.Texts.LibraryTakeOffUpdate));
        rig.Editor.PressChoice(rig.Texts.LibraryTakeOffConfirm);
        rig.Tick();

        Assert.Empty(rig.Editor.Program.Systems);
        Assert.True(rig.Shelf.Holds(blueprint));
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

    /// <summary>
    /// A slot the library's newer blueprint has done away with loses its blocks in every installation as the logic system is
    /// updated from it, and the slots it still has keep theirs.
    /// </summary>
    [Fact]
    public void UpdatingFromTheLibraryDropsTheBlocksOfASlotItNoLongerHas()
    {
        var rig = Newer();
        var system = rig.Editor.Program.Systems[0];

        // The logic system on the construct gains a slot its blueprint in the library lacks: the inner door, which a step
        // closes, answered in every installation.
        var door = rig.Editor.Program.Declare(rig.Editor.World, rig.Editor.World.FindEntity(2)!).Name;
        system.PartFor("Inner").Expects.Add("Door");
        foreach (var instance in system.Instances)
            instance.Fill("Inner", new[] { door });

        var close = new Act("close");
        close.Targets.Add("Inner");
        system.Sequences[0].Steps.Add(close);
        rig.Editor.Program.Edited();
        system.Changed = Saved;
        rig.Editor.Stamp(true);
        system.Changed = Saved;
        rig.Editor.Invalidate();
        rig.Tick();
        Assert.All(system.Instances, i => Assert.NotNull(i.FilledWith("Inner")));
        var kept = system.Instances.Select(i => string.Join(",", i.Filled.Where(s => s != "Inner").OrderBy(s => s))).ToList();

        rig.Editor.AskRenew(system);
        rig.Editor.PressChoice(rig.Texts.LibraryRenewConfirm);
        rig.Tick();

        var now = Assert.Single(rig.Editor.Program.Systems);
        Assert.Null(now.FindPart("Inner"));
        Assert.All(now.Instances, i => Assert.Null(i.FilledWith("Inner")));
        Assert.Equal(kept, now.Instances.Select(i => string.Join(",", i.Filled.OrderBy(s => s))).ToList());
    }

    /// <summary>The same, whose blueprint in the library is a newer copy, renamed Airlock Mk II.</summary>
    static Simulation Newer()
    {
        var rig = Blueprinted();
        var system = rig.Editor.Program.Systems[0];
        system.Name = "Airlock Mk II";
        system.Changed = Saved.AddDays(1);
        rig.Shelf.Keep(system);

        system.Name = "Airlock";
        system.Changed = Saved;
        rig.Editor.Stamp(true);
        rig.Editor.Invalidate();
        rig.Tick();
        return rig;
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
