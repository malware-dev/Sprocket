using Sprocket.Host.SpaceEngineers.Features.Home;
using Sprocket.Host.SpaceEngineers.Features.Library;
using Sprocket.Host.SpaceEngineers.Framework.Language.Reading;
using Sprocket.Host.SpaceEngineers.Framework.Language.Writing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Storage;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Tips;
using Lumel.Text;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>The world keeps a backup of every construct's automation, the player keeps blueprints for every world, and the library lists both.</summary>
public class LibraryTests
{
    static readonly DateTime Saved = new DateTime(2026, 10, 2, 14, 30, 0, DateTimeKind.Utc);

    /// <summary>Backups read back from their index as they were taken, whatever their names hold, each blueprint from its own file.</summary>
    [Fact]
    public void BackupsReadBackWhatTheyWrite()
    {
        var folder = new Folder();
        var written = new Backups(folder);
        Assert.Equal("", written.Read());
        var hangar = Other();
        var airlock = Blueprinted().Editor.Program.Systems[0];
        written.Take("Hangar [Airlock] ; = 2", Saved, new[] { hangar }, new Values());
        written.Take("Lost Ship", Saved.AddHours(1), new[] { airlock }, new Values());

        var read = new Backups(folder);
        Assert.Equal("", read.Read());

        Assert.True(read.Intact);
        Assert.Equal(2, read.All.Count);
        var first = read.All[0];
        Assert.Equal("backup-1.cog", first.File);
        Assert.Equal(hangar.Identity, first.Identity);
        Assert.Equal("Hangar", first.Name);
        Assert.Equal("Hangar [Airlock] ; = 2", first.Construct);
        Assert.Equal(Saved, first.Lost);
        Assert.Equal("backup-2.cog", read.All[1].File);

        Assert.Equal("", read.Open(read.All[1], new Values(), out var opened));
        Assert.Equal(airlock.Identity, opened!.Identity);
        Assert.Equal(ProgramWriter.Blueprint(airlock), folder.Files["backup-2.cog"]);
    }

    /// <summary>An index that will not read gives no backups, says what was wrong, and is never written over.</summary>
    [Theory]
    [InlineData("[backup-1.cog\nname=A\n")]
    [InlineData("[backup-1.cog]\nname=A\n")]
    [InlineData("[backup-1.cog]\nsystem=nope\nname=A\nconstruct=B\nlost=2026-10-02T14:30:00.0000000Z\n")]
    [InlineData("[backup-1.cog]\nsystem=6f1c0f0e-3c2b-4e8a-9d0f-1a2b3c4d5e6f\nname=A\nconstruct=B\nlost=tomorrow\n")]
    [InlineData("[backup-1.cog]\nsystem=6f1c0f0e-3c2b-4e8a-9d0f-1a2b3c4d5e6f\nname=A\nlost=2026-10-02T14:30:00.0000000Z\n")]
    public void BackupsThatWillNotReadAreNotTaken(string text)
    {
        var folder = new Folder();
        folder.Write("backups.ini", text);
        var backups = new Backups(folder);

        Assert.NotEqual("", backups.Read());
        Assert.False(backups.Intact);
        Assert.Empty(backups.All);

        backups.Take("Kept", Saved, new[] { Other() }, new Values());
        Assert.Equal(text, folder.Files["backups.ini"]);
        Assert.False(folder.Has("backup-1.cog"));
    }

    /// <summary>A world with no index yet has no backups, and its first backup makes the index.</summary>
    [Fact]
    public void TheFirstBackupMakesTheIndex()
    {
        var folder = new Folder();
        var backups = new Backups(folder);

        Assert.Equal("", backups.Read());
        Assert.Empty(backups.All);
        Assert.Empty(folder.Files);

        backups.Take("Lost Ship", Saved, new[] { Other() }, new Values());
        Assert.True(folder.Has("backups.ini"));
        Assert.True(folder.Has("backup-1.cog"));
    }

    /// <summary>Backups taken before the index was read are kept for the session alone, which is how a machine that never reads them keeps them.</summary>
    [Fact]
    public void BackupsAreWrittenOnlyOnceTheIndexIsRead()
    {
        var folder = new Folder();
        var backups = new Backups(folder);

        backups.Take("Lost Ship", Saved, new[] { Other() }, new Values());

        Assert.Single(backups.All);
        Assert.Empty(folder.Files);
    }

    /// <summary>Each loss of a changed logic system is kept on its own: lost again after a change, it is kept again, in a file of its own, beside the earlier backup.</summary>
    [Fact]
    public void EachLossIsKeptOnItsOwn()
    {
        var rig = Hangared();
        var airlock = rig.Editor.Program.Systems[0];
        var hangar = rig.Editor.Program.Systems[1];

        // Last changed well before now, so the rename below, which the backup stamps with the time now, is a later version.
        airlock.Changed = Saved;
        rig.Unit.BackUp(rig.Backups, Saved, rig.Shelf);

        rig.Editor.Program.RemoveSystem(hangar);
        airlock.Name = "Airlock Mk II";
        rig.Unit.BackUp(rig.Backups, Saved.AddHours(1), rig.Shelf);

        Assert.Equal(new[] { "Airlock", "Hangar", "Airlock Mk II" }, rig.Backups.All.Select(b => b.Name));
        Assert.Equal(new[] { Saved, Saved, Saved.AddHours(1) }, rig.Backups.All.Select(b => b.Lost));
        Assert.Equal(3, rig.Backups.All.Select(b => b.File).Distinct().Count());
        Assert.Contains("Airlock Mk II", rig.WorldFolder.Files[rig.Backups.All[2].File]);
        Assert.DoesNotContain("Airlock Mk II", rig.WorldFolder.Files[rig.Backups.All[0].File]);
    }

    /// <summary>A logic system lost again unchanged, the same identity and the same date, is not backed up twice.</summary>
    [Fact]
    public void AnUnchangedLogicSystemIsNotBackedUpTwice()
    {
        var rig = Hangared();
        rig.Unit.BackUp(rig.Backups, Saved, rig.Shelf);
        Assert.Equal(2, rig.Backups.All.Count);

        rig.Unit.BackUp(rig.Backups, Saved.AddHours(1), rig.Shelf);

        Assert.Equal(2, rig.Backups.All.Count);
        Assert.All(rig.Backups.All, b => Assert.Equal(Saved, b.Lost));
    }

    /// <summary>Every backup under one construct's name is listed under that name once, the latest first, each with when it was lost.</summary>
    [Fact]
    public void AConstructsBackupsAreListedTogether()
    {
        var rig = Hangared();
        rig.Unit.BackUp(rig.Backups, Saved, rig.Shelf);
        rig.Editor.Program.RemoveSystem(rig.Editor.Program.Systems[1]);

        // Changed after the first backup, as an edit on the screen stamps it, so it is a version of its own.
        var airlock = rig.Editor.Program.Systems[0];
        airlock.Changed = (airlock.Changed ?? Saved).AddDays(1);
        rig.Unit.BackUp(rig.Backups, Saved.AddHours(1), rig.Shelf);

        rig.Editor.GoToRoot();
        rig.Editor.OpenLibrary();
        rig.Tick();

        var page = Assert.IsType<LibraryPage>(rig.Editor.Current);
        Assert.NotNull(page.View);
        var lines = page.Rows.Select(row => row is RichText heading ? heading.Markdown : (row as Label)?.Text).Where(text => text != null).ToList();
        Assert.Single(lines.Skip(lines.IndexOf(CodexPages.Term(rig.Texts.LibraryBackups.ToUpperInvariant(), CodexPages.Backup)) + 1), l => l == "Test Rig");

        var backed = page.Rows.OfType<StatusRow>().Where(r => r.Status != null && r.Status.StartsWith("Lost ")).ToList();
        Assert.Equal(new[] { "Airlock", "Airlock", "Hangar" }, backed.Select(r => r.Summary));
        Assert.NotEqual(backed[0].Status, backed[1].Status);
        Assert.Equal(backed[1].Status, backed[2].Status);
    }

    /// <summary>The list is drawn from the index alone: a backup's blueprint is read as it is chosen, and one that will not read says so then.</summary>
    [Fact]
    public void ABackupsBlueprintIsReadOnlyAsItIsChosen()
    {
        var rig = WithLost(out _, out var page, (sim, _) =>
        {
            sim.WorldFolder.Write(sim.Backups.All[0].File, "not a blueprint");
        });

        var row = Assert.Single(page.Rows.OfType<StatusRow>());
        Assert.Equal(0, row.Problems);

        ChooseLost(rig, page);
        Assert.False(Live(ToConstruct(page)));
    }

    /// <summary>What a chosen row opens is held by its spine from its top to its last line.</summary>
    [Fact]
    public void TheSpineHoldsAllAChosenRowOpens()
    {
        var screen = Screen.Fresh("press", "+ logic system", "type", "Airlock", "enter", "press", "Done", "press", "Library", "tick", "5");
        var page = (LibraryPage)screen.Editor.Current;
        var row = page.Rows.OfType<StatusRow>().First();
        var opened = ((Lumel.Panel)row.Parent).Children.OfType<Gutter>().Single();

        Assert.True(opened.Children.OfType<Lumel.Controls.TreeLine>().Single().TurnsAtBottom);
    }

    /// <summary>Installations are this construct's, so the left side lists them and storage never does.</summary>
    [Fact]
    public void OnlyTheConstructsSideListsInstallations()
    {
        var script = new[] { "press", "+ logic system", "type", "Airlock", "enter", "press", "Done", "press", "Library", "tick", "5" };
        var here = Screen.Fresh(script);
        Assert.True(here.Shows("INSTALLATIONS"), "the construct's side did not list its installations");

        var kept = Screen.Fresh(script.Concat(new[] { "720", "520", "tick", "5", "press", "Make blueprint", "tick", "10" }).ToArray());
        Assert.True(kept.Shows("IN STORAGE"));
        Assert.False(kept.Shows("INSTALLATIONS"), "storage listed the construct's installations");
    }

    /// <summary>A blueprint renamed in the library is kept under its new name, by the same identity.</summary>
    [Fact]
    public void ABlueprintIsRenamedInTheLibrary()
    {
        var rig = Blueprinted();
        rig.Editor.Stamp(true);
        var volume = rig.Shelf.Keep(rig.Editor.Program.Systems[0]);
        var identity = volume.Identity;
        var read = new Automation();
        Assert.Equal("", ProgramReader.Load(volume.Text, read, new Values()));

        rig.Editor.Holdings.Rename(read.Systems[0], volume, "Side Airlock");

        Assert.Single(rig.Shelf.All);
        Assert.Equal("Side Airlock", rig.Shelf.Find(identity)!.Name);
        Assert.Contains("Side Airlock", rig.Shelf.Find(identity)!.Text);
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
        Assert.Equal("Hangar", backup.Name);
        Assert.Equal("", rig.Backups.Open(backup, new Values(), out var system));
        Assert.Equal("Hangar", system!.Name);
        Assert.Empty(system.Instances);
    }

    /// <summary>
    /// Backups are listed by the name of the construct they came from, the latest loss first, and none is listed as the
    /// construct the library is opened on.
    /// </summary>
    [Fact]
    public void BackupsAreListedByNameTheLatestFirst()
    {
        var rig = Blueprinted();
        rig.Backups.Take("Lost Ship", Saved.AddHours(1), new[] { Other() }, new Values());
        rig.Unit.BackUp(rig.Backups, Saved, rig.Shelf);
        rig.Backups.Take("Lost Ship", Saved.AddHours(-1), new[] { Other() }, new Values());
        Assert.Equal(3, rig.Backups.All.Count);

        rig.Editor.GoToRoot();
        rig.Editor.OpenLibrary();
        rig.Tick();

        var page = Assert.IsType<LibraryPage>(rig.Editor.Current);
        Assert.NotNull(page.View);
        var lines = page.Rows.Select(row => row is RichText heading ? heading.Markdown : (row as Label)?.Text).Where(text => text != null).ToList();
        var at = lines.IndexOf(CodexPages.Term(rig.Texts.LibraryBackups.ToUpperInvariant(), CodexPages.Backup));
        Assert.True(at >= 0, "there is no backups heading");
        Assert.Equal(new[] { "Lost Ship", "Test Rig" }, lines.Skip(at + 1).Take(2));
        Assert.Equal(new[] { "Airlock", "Hangar", "Hangar", "Airlock" }, page.Rows.OfType<StatusRow>().Select(r => r.Summary));
    }

    /// <summary>A backup's blueprints are listed under its construct's name, each with when it was lost, to be chosen as a blueprint is.</summary>
    [Fact]
    public void ABackupsBlueprintsAreListedUnderItsConstruct()
    {
        var rig = WithLost(out _, out var page);

        Assert.Contains(page.Rows.OfType<Label>(), l => l.Text == "Lost Ship");
        var row = Assert.Single(page.Rows.OfType<StatusRow>());
        Assert.Equal("Airlock", row.Summary);
        Assert.Equal(1, row.Depth);
        Assert.Equal(Symbols.Blueprint, row.Mark);
        Assert.StartsWith("Lost ", row.Status);
        Assert.NotNull(row.Deletes);

        ChooseLost(rig, page);
        Assert.True(row.Chosen);
        Assert.True(Live(ToConstruct(page)));
    }

    /// <summary>A backed up blueprint's bin forgets it alone, after asking, deleting its file, and the rest are kept.</summary>
    [Fact]
    public void ForgettingABackedUpBlueprintKeepsTheRest()
    {
        var rig = WithLost(out var lost, out var page, (sim, _) =>
        {
            sim.Backups.Take("Lost Ship", Saved, new[] { Other() }, new Values());
        });
        var file = rig.Backups.All.Single(b => b.Identity == lost.Identity).File;

        page.Rows.OfType<StatusRow>().Single(r => r.Summary == "Airlock").Deletes!.Execute();
        Assert.StartsWith(rig.Texts.LibraryForgetBackupQuestion, rig.Editor.Question());
        rig.Editor.PressChoice(rig.Texts.DeleteConfirm);
        rig.Tick();

        Assert.Equal("Hangar", Assert.Single(rig.Backups.All).Name);
        Assert.False(rig.WorldFolder.Has(file));
        Assert.DoesNotContain(file, rig.WorldFolder.Files["backups.ini"]);
    }

    /// <summary>
    /// A lost construct's logic system put on this construct arrives as it was kept, with one installation whose slots are
    /// empty, switched off until they are filled.
    /// </summary>
    [Fact]
    public void ALostConstructsLogicSystemIsPutOnWithOneEmptyInstallation()
    {
        var rig = WithLost(out var lost, out var page);
        ChooseLost(rig, page);

        ToConstruct(page).Command!.Execute();
        rig.Tick();

        var arrived = Assert.Single(rig.Editor.Program.Systems);
        Assert.Equal(lost.Identity, arrived.Identity);
        Assert.Equal(lost.Parts.Select(p => p.Id), arrived.Parts.Select(p => p.Id));
        var installation = Assert.Single(arrived.Instances);
        Assert.Empty(installation.Filled);
        Assert.False(installation.Enabled);
    }

    /// <summary>
    /// The strip's arrow onto this construct puts a chosen backup on it, and is dim once the logic system it keeps is on this
    /// construct.
    /// </summary>
    [Fact]
    public void TheArrowOntoThisConstructPutsABackupOn()
    {
        var rig = WithLost(out var lost, out var page);
        ChooseLost(rig, page);
        var add = ToConstruct(page);
        Assert.True(Live(add));
        Assert.Equal(rig.Texts.Codex.LibraryPutOn, Says(add));

        add.Command!.Execute();
        rig.Tick();

        Assert.Equal(lost.Identity, Assert.Single(rig.Editor.Program.Systems).Identity);
        page.Rows.OfType<StatusRow>().Last(r => r.Depth == 1 && r.Summary == "Airlock").Opens!.Execute();
        rig.Tick();
        Assert.False(Live(add), "the arrow puts it on while it is on this construct");
        Assert.Equal(rig.Texts.Codex.LibraryAlreadyHere, Says(add));
    }

    /// <summary>A backup is a blueprint, so it offers no copy: the arrow onto this construct puts it on.</summary>
    [Fact]
    public void ABackupOffersNoCopy()
    {
        var rig = WithLost(out _, out var page);
        ChooseLost(rig, page);

        Assert.False(Live(Copies(page)));
    }

    /// <summary>
    /// The arrow into the library promotes a chosen backup to a blueprint in storage: the backup is gone, the blueprint is
    /// chosen, and nothing is put on this construct.
    /// </summary>
    [Fact]
    public void ABackupIsPromotedToABlueprint()
    {
        var rig = WithLost(out var lost, out var page);
        ChooseLost(rig, page);

        var keep = ToLibrary(page);
        Assert.True(Live(keep));
        Assert.Equal(rig.Texts.Codex.LibraryPromoteAbout, Says(keep));
        keep.Command!.Execute();
        rig.Tick();
        rig.Editor.PressChoice(rig.Texts.LibraryMakeConfirm);
        rig.Tick();

        var volume = Assert.Single(rig.Shelf.All);
        Assert.NotEqual(lost.Identity, volume.Identity);
        Assert.Equal(ProgramWriter.Blueprint(lost, volume.Identity), volume.Text);
        Assert.Empty(rig.Editor.Program.Systems);
        Assert.Empty(rig.Backups.All);
        var chosen = Assert.Single(page.Rows.OfType<StatusRow>(), r => r.Chosen);
        Assert.Equal("Airlock", chosen.Summary);
        Assert.StartsWith("Changed ", chosen.Status);
    }

    /// <summary>A backup whose blueprint storage keeps otherwise replaces it through the arrow into the library, after asking, and is promoted.</summary>
    [Fact]
    public void ALostConstructsLogicSystemUpdatesItsBlueprintAfterAsking()
    {
        var rig = WithLost(out var lost, out var page, Linked);
        ChooseLost(rig, page);

        ToLibrary(page).Command!.Execute();
        rig.Tick();
        Assert.Contains(rig.Texts.LibraryKeepBackupLinked, rig.Editor.Question());
        Assert.Equal("Airlock Mk 0", Assert.Single(rig.Shelf.All).Name);

        rig.Editor.PressChoice(rig.Texts.LibraryKeepBackupUpdate);
        rig.Tick();

        var volume = Assert.Single(rig.Shelf.All);
        Assert.Equal("Airlock", volume.Name);
        Assert.Equal(lost.Changed, volume.Changed);
        Assert.Empty(rig.Backups.All);
    }

    /// <summary>A backup linked to a blueprint in the library is kept beside it as a new blueprint, when asked to.</summary>
    [Fact]
    public void ALostConstructsLogicSystemIsKeptAsANewBlueprint()
    {
        var rig = WithLost(out var lost, out var page, Linked);
        ChooseLost(rig, page);

        ToLibrary(page).Command!.Execute();
        rig.Tick();
        rig.Editor.PressChoice(rig.Texts.LibraryKeepBackupNew);
        rig.Tick();

        Assert.Equal(2, rig.Shelf.All.Count);
        Assert.Equal("Airlock Mk 0", rig.Shelf.Find(lost.Origin)!.Name);
        Assert.NotEqual(lost.Origin, Assert.Single(rig.Shelf.All, v => v.Name == "Airlock").Identity);
    }

    /// <summary>A backup with no link, of a logic system sharing a stored blueprint's identity, is linked to that blueprint as it is chosen.</summary>
    [Fact]
    public void ABackupSharingABlueprintsIdentityIsLinkedToIt()
    {
        var rig = WithLost(out var lost, out var page, (sim, system) =>
        {
            sim.Folder.Write("sprocket-bp-Airlock.cog", ProgramWriter.Blueprint(system));
            sim.Folder.Write("sprocket-bp.index", "sprocket-bp-Airlock.cog\n");
            Assert.Equal("", sim.Shelf.Read());
        });
        Assert.False(lost.HasOrigin);

        // The backups are listed after storage, which holds its own row of the same name.
        page.Rows.OfType<StatusRow>().Last(r => r.Depth == 1 && r.Summary == "Airlock").Opens!.Execute();
        rig.Tick();

        var keep = ToLibrary(page);
        Assert.False(Live(keep), "the library offers to keep a backup it holds already");
        Assert.Equal(rig.Texts.Codex.LibrarySame, Says(keep));
        Assert.Single(rig.Shelf.All);
    }

    /// <summary>A lost construct's logic system put back on keeps the blueprint it is linked to.</summary>
    [Fact]
    public void ALostConstructsLogicSystemPutBackOnKeepsItsLink()
    {
        var rig = WithLost(out var lost, out var page, Linked);
        ChooseLost(rig, page);

        ToConstruct(page).Command!.Execute();
        rig.Tick();

        var arrived = Assert.Single(rig.Editor.Program.Systems);
        Assert.Equal(lost.Identity, arrived.Identity);
        Assert.Equal(lost.Origin, arrived.Origin);
        Assert.True(rig.Editor.Holdings.Stored(arrived));
    }

    /// <summary>
    /// A backup put back on and unchanged since is promoted with it: the arrow into the library from this construct makes the
    /// blueprint, and the backup, which kept the same logic system as it is, is gone.
    /// </summary>
    [Fact]
    public void ARestoredUnchangedLogicSystemPromotesItsBackup()
    {
        var rig = WithLost(out _, out var page);
        ChooseLost(rig, page);
        ToConstruct(page).Command!.Execute();
        rig.Tick();

        Choose(rig, page, "Airlock");
        Assert.True(Live(ToLibrary(page)));
        ToLibrary(page).Command!.Execute();
        rig.Tick();
        rig.Editor.PressChoice(rig.Texts.LibraryMakeConfirm);
        rig.Tick();

        Assert.Single(rig.Shelf.All);
        Assert.Empty(rig.Backups.All);
    }

    /// <summary>A backup put back on and changed after is another version: making its blueprint leaves the backup.</summary>
    [Fact]
    public void ARestoredChangedLogicSystemLeavesItsBackup()
    {
        var rig = WithLost(out _, out var page);
        ChooseLost(rig, page);
        ToConstruct(page).Command!.Execute();
        rig.Tick();

        // Renamed, which the screen stamps with the time now, after the backup's date.
        var here = Assert.Single(rig.Editor.Program.Systems);
        here.Name = "Airlock Mk II";
        page = Reopened(rig);
        Assert.True(here.Changed > Saved);

        Choose(rig, page, "Airlock Mk II");
        ToLibrary(page).Command!.Execute();
        rig.Tick();
        rig.Editor.PressChoice(rig.Texts.LibraryMakeConfirm);
        rig.Tick();

        Assert.Single(rig.Shelf.All);
        Assert.Single(rig.Backups.All);
    }

    /// <summary>The arrow into the library, while a blueprint is chosen whose logic system is not on this construct, says the blueprint is in the library already and cannot be pressed.</summary>
    [Fact]
    public void TheArrowIntoTheLibrarySaysABlueprintIsThereAlready()
    {
        var rig = Blueprinted();
        var elsewhere = Simulation.Fresh();
        elsewhere.Editor.NewButtonSystem("Hangar");
        rig.Shelf.Keep(elsewhere.Editor.Program.Systems[0]);
        var page = Reopened(rig);

        Choose(rig, page, "Hangar");
        var keep = ToLibrary(page);
        Assert.False(Live(keep), "the arrow keeps a blueprint the library holds");
        Assert.Equal(rig.Texts.Codex.LibraryInLibrary, Says(keep));
    }

    /// <summary>The arrow onto this construct, while a logic system on it is chosen that is the same as its blueprint, says it is already here.</summary>
    [Fact]
    public void TheArrowOntoThisConstructSaysALogicSystemIsAlreadyHere()
    {
        var rig = Blueprinted();
        rig.Shelf.Keep(rig.Editor.Program.Systems[0]);
        var page = Reopened(rig);

        Choose(rig, page, "Airlock");
        var add = ToConstruct(page);

        Assert.False(Live(add));
        Assert.Equal(rig.Texts.Codex.LibraryAlreadyHere, Says(add));
    }

    /// <summary>Copy is offered for a logic system on this construct alone: a blueprint in storage goes on with the arrow instead.</summary>
    [Fact]
    public void CopyIsOfferedOnlyForThisConstruct()
    {
        var rig = Blueprinted();
        rig.Shelf.Keep(rig.Editor.Program.Systems[0]);
        var page = Reopened(rig);

        Choose(rig, page, "Airlock");
        Assert.True(Live(Copies(page)));
        Assert.Equal(rig.Texts.Codex.LibraryCopyAbout, Says(Copies(page)));

        page.Rows.OfType<StatusRow>().Last(r => r.Depth == 1 && r.Summary == "Airlock").Opens!.Execute();
        rig.Tick();
        Assert.False(Live(Copies(page)), "a blueprint in storage offers a copy of itself");
    }

    /// <summary>Making a blueprint asks first, naming the chosen logic system, and leaving it stores nothing.</summary>
    [Fact]
    public void MakingABlueprintAsksAndLeavingItStoresNothing()
    {
        var rig = Blueprinted();
        rig.Editor.Program.Systems[0].Name = "Port Lock";
        var page = Reopened(rig);
        Choose(rig, page, "Port Lock");

        ToLibrary(page).Command!.Execute();
        rig.Tick();
        Assert.StartsWith(rig.Texts.LibraryMakeQuestion, rig.Editor.Question());
        Assert.Contains("Port Lock", rig.Editor.Question());

        rig.Editor.PressChoice(rig.Texts.LibraryMakeLeave);
        rig.Tick();

        Assert.Empty(rig.Shelf.All);
    }

    /// <summary>Putting a blueprint on a construct with no logic system made from it asks nothing: it is on at once.</summary>
    [Fact]
    public void PuttingABlueprintOnAsksNothing()
    {
        var rig = WithLost(out _, out var page);
        ChooseLost(rig, page);

        ToConstruct(page).Command!.Execute();
        rig.Tick();

        Assert.Equal("Airlock", Assert.Single(rig.Editor.Program.Systems).Name);
        Assert.False(rig.Editor.Screen.HasQuestion);
    }

    /// <summary>Making a copy asks first, naming the chosen logic system, and leaving it makes no copy.</summary>
    [Fact]
    public void MakingACopyAsksAndLeavingItMakesNone()
    {
        var rig = Blueprinted();
        rig.Editor.Program.Systems[0].Name = "Port Lock";
        var page = Reopened(rig);
        Choose(rig, page, "Port Lock");

        Copies(page).Command!.Execute();
        rig.Tick();
        Assert.StartsWith(rig.Texts.LibraryCopyQuestion, rig.Editor.Question());
        Assert.Contains("Port Lock", rig.Editor.Question());

        rig.Editor.PressChoice(rig.Texts.LibraryCopyLeave);
        rig.Tick();

        Assert.Equal("Port Lock", Assert.Single(rig.Editor.Program.Systems).Name);
    }

    /// <summary>Making a copy, once asked, puts a logic system of its own on this construct.</summary>
    [Fact]
    public void MakingACopyAnsweredMakesOne()
    {
        var rig = Blueprinted();
        var page = Reopened(rig);
        Choose(rig, page, "Airlock");

        Copies(page).Command!.Execute();
        rig.Tick();
        rig.Editor.PressChoice(rig.Texts.LibraryCopyConfirm);
        rig.Tick();

        Assert.Equal(new[] { "Airlock", "Airlock 2" }, rig.Editor.Program.Systems.Select(s => s.Name));
    }

    /// <summary>
    /// The library lists this construct's logic systems under its name, then storage, then the backups, choosing the
    /// first.
    /// </summary>
    [Fact]
    public void TheLibraryListsThisConstructThenStorageThenBackups()
    {
        var rig = Blueprinted();
        rig.Editor.Program.Systems[0].Changed = Saved;
        rig.Shelf.Keep(rig.Editor.Program.Systems[0]);
        rig.Backups.Take("Lost Ship", Saved, new[] { Other() }, new Values());

        rig.Editor.GoToRoot();
        rig.Editor.OpenLibrary();
        rig.Tick();

        var page = Assert.IsType<LibraryPage>(rig.Editor.Current);
        Assert.NotNull(page.View);
        Assert.Equal(rig.Editor.World.Name, Assert.IsType<Label>(page.Rows.First()).Text);
        var rows = page.Rows.OfType<StatusRow>().ToList();
        Assert.Equal(new[] { "Airlock", "Airlock", "Hangar" }, rows.Select(r => r.Summary));
        Assert.Equal("2 installations", rows[0].Status);
        Assert.StartsWith("Changed ", rows[1].Status);
        Assert.StartsWith("Lost ", rows[2].Status);
        Assert.True(rows[0].Chosen);
    }

    /// <summary>
    /// The library's search finds the words typed in a row's name in the order typed, as every search box does, and narrows
    /// the library's side alone: this construct's logic system stays.
    /// </summary>
    [Theory]
    [InlineData("", new[] { "Airlock", "Airlock", "Hangar" })]
    [InlineData("airl", new[] { "Airlock", "Airlock" })]
    [InlineData("air lock", new[] { "Airlock", "Airlock" })]
    [InlineData("lock air", new[] { "Airlock" })]
    [InlineData("HANG", new[] { "Airlock", "Hangar" })]
    public void TheLibrarySearchMatchesAsEverySearchDoes(string typed, string[] shown)
    {
        var rig = Blueprinted();
        rig.Editor.Program.Systems[0].Changed = Saved;
        rig.Shelf.Keep(rig.Editor.Program.Systems[0]);
        rig.Backups.Take("Lost Ship", Saved, new[] { Other() }, new Values());
        rig.Editor.GoToRoot();
        rig.Editor.OpenLibrary();
        rig.Tick();
        var page = Assert.IsType<LibraryPage>(rig.Editor.Current);
        Assert.NotNull(page.View);

        page.Search(typed);
        rig.Tick();

        var rows = page.Rows.OfType<StatusRow>().Where(r => r.IsVisible);
        Assert.Equal(shown, rows.Select(r => r.Summary));
    }

    /// <summary>This construct's search narrows this construct's side alone: the library's rows stay.</summary>
    [Fact]
    public void ThisConstructsSearchNarrowsItsOwnSide()
    {
        var rig = Blueprinted();
        rig.Editor.Program.Systems[0].Changed = Saved;
        rig.Shelf.Keep(rig.Editor.Program.Systems[0]);
        rig.Backups.Take("Lost Ship", Saved, new[] { Other() }, new Values());
        var page = Reopened(rig);

        SearchHere(page, "hang");
        rig.Tick();

        Assert.Equal(new[] { "Airlock", "Hangar" }, page.Rows.OfType<StatusRow>().Where(r => r.IsVisible).Select(r => r.Summary));
        Assert.StartsWith("Changed ", page.Rows.OfType<StatusRow>().First(r => r.IsVisible).Status);
    }

    /// <summary>
    /// Making a blueprint in the library chooses it in storage, clearing a search there that would hide it, so it is in
    /// view.
    /// </summary>
    [Fact]
    public void MakingABlueprintChoosesItInStorage()
    {
        var rig = Blueprinted();
        rig.Backups.Take("Lost Ship", Saved, new[] { Other() }, new Values());
        var page = Reopened(rig);
        page.Search("hang");
        rig.Tick();

        Choose(rig, page, "Airlock");
        Assert.Equal(rig.Texts.Codex.SystemsMakeBlueprintAbout, Says(ToLibrary(page)));
        ToLibrary(page).Command!.Execute();
        rig.Tick();
        rig.Editor.PressChoice(rig.Texts.LibraryMakeConfirm);
        rig.Tick();

        Assert.Equal("", page.SearchBox.Text);
        var chosen = Assert.Single(page.Rows.OfType<StatusRow>(), r => r.Chosen);
        Assert.Equal("Airlock", chosen.Summary);
        Assert.True(chosen.IsVisible);
        Assert.StartsWith("Changed ", chosen.Status);
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
        var copy = rig.Editor.Holdings.Copy(ProgramWriter.Blueprint(rig.Editor.Program.Systems[0]), out var trouble);
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

    /// <summary>Where the library's blueprint was changed after the logic system, the home screen and the library offer to update the logic system from it, and offer no reset, since there is nothing on the logic system to go back from.</summary>
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
        var update = ToConstruct(page);
        Assert.True(Live(update));
        Assert.Equal(rig.Texts.Codex.SystemsUpdateFromLibraryAbout, Says(update));
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
        var volume = Assert.Single(rig.Shelf.All);
        Assert.Equal(volume.Identity, now.Origin);
        Assert.Equal(volume.Changed, now.OriginChanged);
        Assert.False(rig.Editor.Holdings.ChangedSince(now));
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
        Assert.NotEqual(blueprint.Identity, blueprint.Origin);
        Assert.Equal(Saved, blueprint.OriginChanged);

        Assert.Equal("sprocket-bp-Airlock.cog\n", rig.Folder.Files["sprocket-bp.index"]);
        Assert.Equal(ProgramWriter.Blueprint(blueprint, blueprint.Origin), rig.Folder.Files["sprocket-bp-Airlock.cog"]);

        var read = new LibraryShelf(rig.Folder, delegate { });
        Assert.Equal("", read.Read());

        var volume = Assert.Single(read.All);
        Assert.Equal(LibraryEntryState.Read, volume.State);
        Assert.Equal(blueprint.Origin, volume.Identity);
        Assert.Equal("Airlock", volume.Name);
        Assert.Equal(Saved, volume.Changed);
        Assert.True(read.Holds(blueprint));
    }

    /// <summary>A line of the index that gives no blueprint is listed with why, and the lines after it are read all the same.</summary>
    [Theory]
    [InlineData("gone.cog", LibraryEntryState.Missing)]
    [InlineData("Blueprints/given.cog", LibraryEntryState.Path)]
    [InlineData("..\\given.cog", LibraryEntryState.Path)]
    [InlineData("C:given.cog", LibraryEntryState.Path)]
    [InlineData("junk.doot", LibraryEntryState.Unreadable)]
    [InlineData("sprocket-bp-Airlock.cog", LibraryEntryState.Twice)]
    public void ALineThatGivesNoBlueprintSaysWhy(string line, LibraryEntryState state)
    {
        var rig = Blueprinted();
        rig.Shelf.Keep(rig.Editor.Program.Systems[0]);
        rig.Folder.Write("junk.doot", "not a blueprint");
        rig.Folder.Write("sprocket-bp.index", "sprocket-bp-Airlock.cog\n" + line + "\n");

        var read = new LibraryShelf(rig.Folder, delegate { });
        Assert.NotEqual("", read.Read());

        Assert.True(read.Intact);
        Assert.Equal(new[] { LibraryEntryState.Read, state }, read.All.Select(v => v.State));
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
        rig.Folder.Write("stoopid.doot", ProgramWriter.Blueprint(blueprint));
        rig.Folder.Write("sprocket-bp.index", "# from a friend\r\n\r\n  stoopid.doot  \r\n");
        Assert.Equal("", rig.Shelf.Read());

        // Handed to the editor again, as opening the screen hands it, which links the logic system to the file of its identity.
        rig.Editor.Shelf = rig.Shelf;

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

    /// <summary>
    /// A blueprint put on a construct from the library arrives as it was kept, as a logic system with its first installation,
    /// switched off until its slots are filled, and only once.
    /// </summary>
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

        Assert.Equal("", rig.Editor.Holdings.PutOn(volume));

        var arrived = Assert.Single(rig.Editor.Program.Systems);
        Assert.True(arrived.IsBlueprint);
        Assert.NotEqual(volume.Identity, arrived.Identity);
        Assert.Equal(volume.Identity, arrived.Origin);
        Assert.Equal(volume.Changed, arrived.OriginChanged);
        Assert.Equal("Installation 1", Assert.Single(arrived.Instances).Name);
        Assert.False(arrived.Instances[0].Enabled);
        Assert.Equal(blueprint.Parts.Select(p => p.Id), arrived.Parts.Select(p => p.Id));
        Assert.Equal(blueprint.Sequences.Count, arrived.Sequences.Count);

        Assert.Equal(rig.Texts.LibraryOnAlready, rig.Editor.Holdings.PutOn(volume));
        Assert.Single(rig.Editor.Program.Systems);
    }

    /// <summary>A logic system with the identity of a blueprint in the library, and no link, is linked to it as the library is handed to the editor.</summary>
    [Fact]
    public void ALogicSystemSharingABlueprintsIdentityIsLinkedToIt()
    {
        var rig = Blueprinted();
        var system = rig.Editor.Program.Systems[0];
        system.Changed = Saved;
        rig.Folder.Write("sprocket-bp-Airlock.cog", ProgramWriter.Blueprint(system));
        rig.Folder.Write("sprocket-bp.index", "sprocket-bp-Airlock.cog\n");
        Assert.Equal("", rig.Shelf.Read());
        Assert.False(system.HasOrigin);

        rig.Editor.Shelf = rig.Shelf;

        Assert.Equal(system.Identity, system.Origin);
        Assert.Equal(Saved, system.OriginChanged);
        Assert.True(rig.Shelf.Holds(system));
        Assert.False(rig.Editor.Holdings.ChangedSince(system));
    }

    /// <summary>A blueprint put on from the library is a step home's undo takes back, and redo puts it on again, switched off.</summary>
    [Fact]
    public void PuttingABlueprintOnIsTakenBack()
    {
        var made = Blueprinted();
        made.Shelf.Keep(made.Editor.Program.Systems[0]);

        var rig = Simulation.Fresh();
        foreach (var file in made.Folder.Files)
            rig.Folder.Write(file.Key, file.Value);

        rig.Shelf.Read();
        Assert.Equal("", rig.Editor.Holdings.PutOn(rig.Shelf.All[0]));
        rig.Editor.GoToRoot();
        rig.Tick();
        var arrived = Assert.Single(rig.Editor.Program.Systems);

        // Taking it away waits while it may run.
        arrived.Enabled = true;
        rig.Tick();
        Assert.Null(rig.Editor.Screen.Undo);
        arrived.Enabled = false;
        rig.Tick();

        Assert.Equal(Filled.With(rig.Texts.ChangePutOnSystem, "thing", "Airlock"), rig.Editor.Screen.UndoText);
        rig.Editor.Screen.Undo!.Execute();
        rig.Tick();
        Assert.Empty(rig.Editor.Program.Systems);

        rig.Editor.Screen.Redo!.Execute();
        rig.Tick();
        Assert.Same(arrived, Assert.Single(rig.Editor.Program.Systems));
        Assert.False(arrived.Enabled);
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

        rig.Editor.Holdings.TakeOff(blueprint, true);
        Assert.Single(rig.Editor.Program.Systems);
        Assert.Empty(rig.Shelf.All);

        rig.Shelf.Keep(blueprint);
        blueprint.Name = "Airlock Mk II";
        rig.Editor.Holdings.TakeOff(blueprint, false);
        Assert.Empty(rig.Editor.Program.Systems);
        Assert.Equal("Airlock", Assert.Single(rig.Shelf.All).Name);

        rig.Editor.Screen.Undo!.Execute();
        rig.Tick();
        rig.Editor.Holdings.TakeOff(blueprint, true);
        Assert.Equal("Airlock Mk II", Assert.Single(rig.Shelf.All).Name);
    }

    /// <summary>
    /// Taking off a logic system changed after its blueprint in the library first says its installations go with it, then
    /// asks on its own whether to bring the blueprint up to date; answering to leave it takes the logic system off with the
    /// blueprint as it was.
    /// </summary>
    [Fact]
    public void TakingOffAChangedLogicSystemAsksAboutItsBlueprint()
    {
        var rig = Changed(out _);

        Assert.Contains(rig.Texts.LibraryTakeOffGoes, rig.Editor.Question());
        Assert.False(rig.Editor.HasChoice(rig.Texts.LibraryTakeOffUpdate));
        rig.Editor.PressChoice(rig.Texts.LibraryTakeOffConfirm);
        rig.Tick();

        Assert.StartsWith("Airlock Mk II was changed after its blueprint", rig.Editor.Question());
        Assert.True(rig.Editor.HasChoice(rig.Texts.LibraryTakeOffUpdate));
        rig.Editor.PressChoice(rig.Texts.LibraryTakeOffLeave);
        rig.Tick();

        Assert.Empty(rig.Editor.Program.Systems);
        Assert.Equal("Airlock", Assert.Single(rig.Shelf.All).Name);
    }

    /// <summary>Backing out of the question about the blueprint takes nothing off and leaves the blueprint as it was.</summary>
    [Fact]
    public void BackingOutOfTheBlueprintQuestionTakesNothingOff()
    {
        var rig = Changed(out var blueprint);
        rig.Editor.PressChoice(rig.Texts.LibraryTakeOffConfirm);
        rig.Tick();

        rig.Editor.Unask();
        rig.Tick();

        Assert.Same(blueprint, Assert.Single(rig.Editor.Program.Systems));
        Assert.Equal("Airlock", Assert.Single(rig.Shelf.All).Name);
    }

    /// <summary>A blueprinted logic system renamed after its blueprint was kept, with the question to take it off standing.</summary>
    static Simulation Changed(out AutomationSystem blueprint)
    {
        var rig = Blueprinted();
        blueprint = rig.Editor.Program.Systems[0];
        blueprint.Changed = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        rig.Shelf.Keep(blueprint);

        blueprint.Name = "Airlock Mk II";
        rig.Editor.Holdings.AskTakeOff(blueprint);
        return rig;
    }

    /// <summary>Taking off a logic system unchanged since its blueprint asks only whether to take it off, nothing about the blueprint.</summary>
    [Fact]
    public void TakingOffAnUnchangedLogicSystemAsksNothingAboutItsBlueprint()
    {
        var rig = Blueprinted();
        var blueprint = rig.Editor.Program.Systems[0];
        rig.Editor.Stamp(true);
        rig.Shelf.Keep(blueprint);

        rig.Editor.Holdings.AskTakeOff(blueprint);

        Assert.Contains(rig.Texts.LibraryTakeOffKept, rig.Editor.Question());
        Assert.False(rig.Editor.HasChoice(rig.Texts.LibraryTakeOffUpdate));
        rig.Editor.PressChoice(rig.Texts.LibraryTakeOffConfirm);
        rig.Tick();

        Assert.Empty(rig.Editor.Program.Systems);
        Assert.True(rig.Shelf.Holds(blueprint));
    }

    /// <summary>
    /// The home screen offers a logic system a minus where the library has its blueprint, which asks before taking it off with
    /// its installations, and a bin that deletes it at once, undo bringing it back, where this is the only copy.
    /// </summary>
    [Fact]
    public void TheHomeScreenOffersAMinusOnlyWhereTheLibraryHasItsBlueprint()
    {
        var rig = Blueprinted();
        var blueprint = rig.Editor.Program.Systems[0];

        var heading = Assert.Single(rig.Editor.RootHeadings());
        Assert.Null(heading.BinMark);
        Assert.Equal(rig.Texts.Codex.LibraryDeletes, heading.DeleteText);
        heading.Deletes!.Execute();
        rig.Tick();
        Assert.Empty(rig.Editor.Program.Systems);
        rig.Editor.Screen.Undo!.Execute();
        rig.Tick();
        Assert.Same(blueprint, Assert.Single(rig.Editor.Program.Systems));

        rig.Shelf.Keep(blueprint);
        rig.Editor.Invalidate();
        rig.Tick();

        heading = Assert.Single(rig.Editor.RootHeadings());
        Assert.Equal(Symbols.Minus, heading.BinMark);
        Assert.Equal(rig.Texts.Codex.LibraryTakeOff, heading.DeleteText);
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
        Assert.Equal(Filled.With(rig.Texts.ChangeTakeOffSystem, "thing", "Airlock"), rig.Editor.Screen.UndoText);
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

        var copy = rig.Editor.Holdings.Copy(ProgramWriter.Blueprint(blueprint), out var trouble);

        Assert.Equal("", trouble);
        Assert.NotNull(copy);
        Assert.NotEqual(blueprint.Identity, copy.Identity);
        Assert.Equal("Airlock 2", copy.Name);
        Assert.True(copy.IsBlueprint);
        Assert.Equal("Installation 1", Assert.Single(copy.Instances).Name);
        Assert.Equal(2, blueprint.Instances.Count);
        Assert.Equal("Airlock 3", rig.Editor.Holdings.Copy(ProgramWriter.Blueprint(blueprint), out trouble)!.Name);
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
        Assert.Equal("Airlock", backup.Name);
        Assert.Contains("Airlock", rig.WorldFolder.Files[backup.File]);
    }

    /// <summary>A construct deleted whole, as a player in creative or the server's cleanup does it, leaves no backup.</summary>
    [Fact]
    public void DeletingTheConstructTakesNoBackup()
    {
        var rig = Simulation.Fresh();
        rig.Editor.NewButtonSystem("Airlock");
        rig.Tick();

        rig.Machine.Delete(rig.Machine.Sprocket.Grid);
        rig.Tick();

        Assert.True(rig.Unit.Lost);
        Assert.Empty(rig.Backups.All);
    }

    /// <summary>A programmable block that took the automation over as the hero was ground off, deleted with its grid, leaves no backup.</summary>
    [Fact]
    public void DeletingTheConstructAfterTheHeroWasGroundTakesNoBackup()
    {
        var rig = Simulation.Fresh();
        rig.Editor.NewButtonSystem("Airlock");
        rig.Machine.Programmable(200, "Spare");
        rig.Tick();

        rig.Machine.Grind(rig.Machine.Sprocket);
        rig.Tick();
        rig.Machine.Delete(rig.Machine.Sprocket.Grid);
        rig.Tick();

        Assert.True(rig.Unit.Lost);
        Assert.Empty(rig.Backups.All);
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
        system.PartFor("Inner").Expects = "Door";
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

        rig.Editor.Holdings.AskRenew(system);
        rig.Editor.PressChoice(rig.Texts.LibraryRenewConfirm);
        rig.Tick();

        var now = Assert.Single(rig.Editor.Program.Systems);
        Assert.Null(now.FindPart("Inner"));
        Assert.All(now.Instances, i => Assert.Null(i.FilledWith("Inner")));
        Assert.Equal(kept, now.Instances.Select(i => string.Join(",", i.Filled.OrderBy(s => s))).ToList());
    }

    /// <summary>
    /// Where the logic system was changed after its blueprint, the library offers to reset it to the blueprint, and the home
    /// screen does not: it asks first, then puts the blueprint back in its place with its installations, and undo brings the
    /// changes back.
    /// </summary>
    [Fact]
    public void AChangedLogicSystemIsResetToItsBlueprint()
    {
        var rig = Blueprinted();
        var system = rig.Editor.Program.Systems[0];
        Assert.Single(rig.Editor.RootHeadings()).Offers!.Execute();
        rig.Tick();
        rig.Editor.GoToRoot();
        rig.Tick();
        var filled = system.Instances.Select(i => string.Join(",", i.Filled.OrderBy(s => s))).ToList();
        var switched = system.Instances.Select(i => i.Enabled).ToList();

        system.Name = "Airlock Mk II";
        system.Changed = DateTime.UtcNow.AddMinutes(1);
        rig.Editor.Invalidate();
        rig.Tick();
        Assert.Equal(rig.Texts.SystemsUpdateBlueprint, Assert.Single(rig.Editor.RootHeadings()).ButtonText);

        rig.Editor.OpenLibrary();
        rig.Tick();
        var page = Assert.IsType<LibraryPage>(rig.Editor.Current);
        var reset = ToConstruct(page);
        Assert.True(Live(reset));
        Assert.Equal(rig.Texts.Codex.LibraryResetAbout, Says(reset));

        reset.Command!.Execute();
        rig.Tick();
        Assert.Contains(rig.Texts.LibraryResetReplaced, rig.Editor.Question());
        rig.Editor.PressChoice(rig.Texts.LibraryResetConfirm);
        rig.Tick();

        var now = Assert.Single(rig.Editor.Program.Systems);
        Assert.Equal("Airlock", now.Name);
        Assert.Equal(system.Identity, now.Identity);
        Assert.Equal(filled, now.Instances.Select(i => string.Join(",", i.Filled.OrderBy(s => s))).ToList());
        Assert.Equal(switched, now.Instances.Select(i => i.Enabled).ToList());
        Assert.True(rig.Shelf.Holds(now));
        Assert.False(Live(reset), "the logic system and its blueprint are the same, and reset is still offered");

        rig.Editor.GoToRoot();
        rig.Tick();
        Assert.Equal(Filled.With(rig.Texts.ChangeResetToBlueprint, "thing", "Airlock"), rig.Editor.Screen.UndoText);
        rig.Editor.Screen.Undo!.Execute();
        rig.Tick();
        Assert.Equal("Airlock Mk II", Assert.Single(rig.Editor.Program.Systems).Name);
    }

    /// <summary>Updating the blueprint from the home screen leaves the logic system and its blueprint the same, so the heading offers nothing more.</summary>
    [Fact]
    public void UpdatingTheBlueprintFromHomeLeavesNothingToOffer()
    {
        var rig = Blueprinted();
        var system = rig.Editor.Program.Systems[0];
        Assert.Single(rig.Editor.RootHeadings()).Offers!.Execute();
        rig.Tick();
        rig.Editor.GoToRoot();
        rig.Tick();

        system.Name = "Airlock Mk II";
        system.Changed = DateTime.UtcNow.AddMinutes(1);
        rig.Editor.Invalidate();
        rig.Tick();
        var heading = Assert.Single(rig.Editor.RootHeadings());
        Assert.Equal(rig.Texts.SystemsUpdateBlueprint, heading.ButtonText);

        heading.Offers!.Execute();
        rig.Tick();
        rig.Editor.PressChoice(rig.Texts.LibraryUpdateConfirm);
        rig.Tick();

        Assert.IsType<HomePage>(rig.Editor.Current);
        Assert.True(rig.Shelf.Holds(system), "the library's copy is not the logic system as it is");
        Assert.Null(Assert.Single(rig.Editor.RootHeadings()).Offers);
    }

    /// <summary>The same, pressed on the screen: the button, then the answer in the question hanging under it.</summary>
    [Fact]
    public void UpdatingTheBlueprintByPressingStoresIt()
    {
        var screen = Screen.Fresh("press \"logic system\" type Airlock enter escape press \"Make blueprint\" escape".Split(' '));
        var system = Assert.Single(screen.Editor.Program.Systems);
        Assert.True(screen.Editor.Shelf!.Holds(system), "making the blueprint stored nothing");

        // Making the blueprint is written into the block a tick later; the change below comes after that.
        screen.Run(1);
        system.Name = "Airlock Mk II";
        system.Changed = DateTime.UtcNow.AddMinutes(1);
        screen.Editor.Invalidate();
        screen.Run(2);
        Assert.True(screen.Shows("Update blueprint"), screen.Dump);

        screen.Do("press", "\"Update", "blueprint\"");
        screen.Run(1);
        Assert.True(screen.Shows("Update"), "no question");
        screen.Do("press", "Update");
        screen.Run(2);

        Assert.True(screen.Editor.Shelf.Holds(system), "the answer stored nothing");
        Assert.False(screen.Shows("Update blueprint"), "the button is still offered");
    }

    /// <summary>The same, whose blueprint in the library is a newer copy, renamed Airlock Mk II.</summary>
    static Simulation Newer()
    {
        var rig = Blueprinted();
        var system = rig.Editor.Program.Systems[0];
        system.Name = "Airlock Mk II";
        system.Changed = Saved.AddDays(1);
        rig.Shelf.Keep(system);

        // Made from the blueprint's version of a day before, and unchanged since: the library's copy is the newer by its date.
        system.Name = "Airlock";
        system.Changed = Saved;
        system.OriginChanged = Saved;
        rig.Editor.Stamp(true);
        rig.Editor.Invalidate();
        rig.Tick();
        return rig;
    }

    /// <summary>A logic system with a button, made a blueprint, with a second installation made from it.</summary>
    /// <summary>
    /// A construct with no automation whose library lists one lost construct, which had the airlock of
    /// <see cref="Blueprinted" />; <paramref name="before" />, where given, is done before the library opens.
    /// </summary>
    static Simulation WithLost(out AutomationSystem lost, out LibraryPage page, Action<Simulation, AutomationSystem>? before = null)
    {
        // Last changed well before now, so an edit after it is put back, which is stamped with the time now, is a later version.
        lost = Blueprinted().Editor.Program.Systems[0];
        lost.Changed = Saved;
        var rig = Simulation.Fresh();
        rig.Backups.Take("Lost Ship", Saved, new[] { lost }, new Values());
        before?.Invoke(rig, lost);

        rig.Editor.GoToRoot();
        rig.Editor.OpenLibrary();
        rig.Tick();

        page = Assert.IsType<LibraryPage>(rig.Editor.Current);
        Assert.NotNull(page.View);
        return rig;
    }

    /// <summary>Makes the backup of <see cref="WithLost" /> one of a logic system linked to a blueprint the library keeps as Airlock Mk 0.</summary>
    static void Linked(Simulation sim, AutomationSystem system)
    {
        system.Name = "Airlock Mk 0";
        sim.Shelf.Keep(system);

        // Renamed, and dated after the blueprint it is linked to, as an edit on the screen stamps it.
        system.Name = "Airlock";
        system.Changed = (system.Changed ?? Saved).AddDays(1);
        sim.Backups.Forget(sim.Backups.All[0]);
        sim.Backups.Take("Lost Ship", Saved, new[] { system }, new Values());
    }

    /// <summary>The library opened again from the home screen, listing what is there now.</summary>
    static LibraryPage Reopened(Simulation rig)
    {
        rig.Editor.GoToRoot();
        rig.Editor.OpenLibrary();
        rig.Tick();

        var page = Assert.IsType<LibraryPage>(rig.Editor.Current);
        Assert.NotNull(page.View);
        return page;
    }

    /// <summary>Chooses the lost construct's airlock in the library.</summary>
    static void ChooseLost(Simulation rig, LibraryPage page)
    {
        page.Rows.OfType<StatusRow>().Single(r => r.Depth == 1 && r.Summary == "Airlock").Opens!.Execute();
        rig.Tick();
    }

    /// <summary>The construct of <see cref="Blueprinted" /> with a second logic system on it, called Hangar.</summary>
    static Simulation Hangared()
    {
        var rig = Blueprinted();
        rig.Editor.NewButtonSystem("Hangar");
        rig.Editor.GoToRoot();
        rig.Tick();
        Assert.Equal(2, rig.Editor.Program.Systems.Count);
        return rig;
    }

    /// <summary>A blueprint of another airlock, called Hangar, with an identity of its own.</summary>
    static AutomationSystem Other()
    {
        var other = Blueprinted().Editor.Program.Systems[0];
        other.Identity = Guid.NewGuid();
        other.Name = "Hangar";
        return other;
    }

    /// <summary>The strip between the library's panes, once the page's view is made, as reading it makes it.</summary>
    static LibraryMoves Strip(LibraryPage page)
    {
        Assert.NotNull(page.View);
        return page.Moves;
    }

    /// <summary>The strip's button that puts the chosen row on this construct.</summary>
    static MoveButton ToConstruct(LibraryPage page) => Strip(page).ToConstruct;

    /// <summary>The strip's button that puts the chosen row in the library.</summary>
    static MoveButton ToLibrary(LibraryPage page) => Strip(page).ToLibrary;

    /// <summary>The strip's button that copies the chosen logic system.</summary>
    static MoveButton Copies(LibraryPage page) => Strip(page).Copies;

    /// <summary>Whether <paramref name="button" /> can be pressed.</summary>
    static bool Live(MoveButton button) => button.Command!.CanExecute;

    /// <summary>What <paramref name="button" />'s tip says it will do.</summary>
    static string? Says(MoveButton button) => button.Tip?.Text;

    /// <summary>Chooses the first row named <paramref name="name" />, reading this construct's side first.</summary>
    static void Choose(Simulation rig, LibraryPage page, string name)
    {
        page.Rows.OfType<StatusRow>().First(r => r.Depth == 1 && r.Summary == name).Opens!.Execute();
        rig.Tick();
    }

    /// <summary>Types <paramref name="words" /> into the search on this construct's side, over whatever it held.</summary>
    static void SearchHere(LibraryPage page, string words)
    {
        page.ConstructSearchBox.SelectAll();
        if (words.Length == 0)
            page.ConstructSearchBox.Backspace();
        else
            page.ConstructSearchBox.Insert(words);
    }

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

        // Something done to it, renamed, so it stays when the panel is left.
        sim.Editor.Program.Systems[0].Instances[sim.Editor.Program.Systems[0].Instances.Count - 1].Name = "Port airlock";
        sim.Editor.GoToRoot();
        sim.Tick();

        return sim;
    }
}
