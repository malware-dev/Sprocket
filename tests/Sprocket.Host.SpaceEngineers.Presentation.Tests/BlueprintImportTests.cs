using Sprocket.Host.SpaceEngineers.Features.Sharing.Text;
using Sprocket.Host.SpaceEngineers.Framework.Language.Reading;
using Sprocket.Host.SpaceEngineers.Framework.Language.Writing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Storage;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A blueprint copied as text in one library is imported into another from a block's Custom Data.</summary>
public class BlueprintImportTests
{
    /// <summary>A library with one blueprint, Hangar Doors, open on the screen.</summary>
    static Screen Library(Func<string>? customData)
    {
        var screen = Screen.On(Rig.Demo());
        screen.Editor.Shelf = new LibraryShelf(new Folder(), delegate { });
        screen.Editor.Shelf.Keep(new AutomationSystem(1, Guid.NewGuid()) { Name = "Hangar Doors", Namespace = "Shop", IsBlueprint = true });
        screen.Editor.OpenedFromData = customData;
        screen.Editor.OpenLibrary();
        screen.Run(2);
        return screen;
    }

    /// <summary>
    /// Copy as text puts the blueprint itself on the clipboard, and Copy compact its section; each says how to import it, and
    /// Import in another library names it and adds it, under its own identity, once the player says so. The plain blueprint is
    /// the whole of the Custom Data; the section sits among another script's.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ACopiedBlueprintImportsFromCustomData(bool compact)
    {
        var copying = Library(null);
        var texts = copying.Editor.Texts;
        var identity = copying.Editor.Shelf!.All[0].Identity;
        copying.Do("press", "Hangar Doors").Run(2);
        copying.Do("press", texts.LibraryShare).Run(2);
        copying.Do("press", texts.ShareCopy).Run(2);
        copying.Do("press", compact ? texts.ShareCopyCompact : texts.ShareCopyText).Run(2);

        var copied = copying.Editor.Input.Clipboard.Get();
        Assert.StartsWith(compact ? "[" + BlueprintText.Section + " " + identity.ToString("D") + "]" : "sprocket ", copied);
        Assert.True(copying.Shows(texts.ShareCopiedOk), copying.Dump);

        var importing = Library(() => compact ? "[another-script]\nspeed=4\n\n" + copied : copied);
        importing.Editor.Shelf!.Forget(importing.Editor.Shelf.All[0]);
        importing.Do("press", texts.LibraryImport).Run(2);

        Assert.Empty(importing.Editor.Shelf.All);
        Assert.True(importing.Shows("Hangar Doors"), importing.Dump);
        importing.Do("press", texts.LibraryImportConfirm).Run(2);

        var imported = Assert.Single(importing.Editor.Shelf.All);
        Assert.Equal("Hangar Doors", imported.Name);
        Assert.Equal(identity, imported.Identity);
    }

    /// <summary>A newer copy of a blueprint the library has, by its identity, updates the one kept rather than adding another.</summary>
    [Fact]
    public void ANewerCopyUpdatesTheOneKept()
    {
        Screen? screen = null;
        // ReSharper disable once AccessToModifiedClosure - the library reads its Custom Data only when Import is pressed, after the screen is set.
        screen = Library(() => Newer(screen!.Editor.Shelf!.All[0], "Hangar Doors Mark 2"));
        var texts = screen.Editor.Texts;

        screen.Do("press", texts.LibraryImport).Run(2);
        screen.Do("press", texts.LibraryImportConfirm).Run(2);

        Assert.Equal("Hangar Doors Mark 2", Assert.Single(screen.Editor.Shelf!.All).Name);
    }

    /// <summary>A copy of a blueprint the library has as new or newer is left out, and nothing is asked.</summary>
    [Fact]
    public void ASameOrOlderCopyIsLeftAsItIs()
    {
        var copying = Library(null);
        var kept = copying.Editor.Shelf!.All[0];
        var same = BlueprintText.Write(kept.Name, kept.Text);
        var screen = Library(() => same);
        screen.Editor.Shelf!.Forget(screen.Editor.Shelf.All[0]);
        screen.Editor.Shelf.Import(Read(kept.Text));
        var texts = screen.Editor.Texts;

        screen.Do("press", texts.LibraryImport).Run(2);

        Assert.False(screen.Shows(texts.LibraryImportKeep), screen.Dump);
        Assert.Equal(kept.Identity, Assert.Single(screen.Editor.Shelf.All).Identity);
    }

    /// <summary>The text of <paramref name="kept" />'s blueprint, renamed <paramref name="name" /> and changed a day later.</summary>
    static string Newer(LibraryEntry kept, string name)
    {
        var system = Read(kept.Text);
        system.Name = name;
        system.Changed = (kept.Changed ?? DateTime.UtcNow).AddDays(1);
        return BlueprintText.Write(name, ProgramWriter.Blueprint(system));
    }

    static AutomationSystem Read(string blueprint)
    {
        Assert.Equal("", ProgramReader.Lone(blueprint, new Values(), out var system));
        return system;
    }

    /// <summary>Answering Don't import adds nothing.</summary>
    [Fact]
    public void DeclinedImportAddsNothing()
    {
        var copied = BlueprintText.Write("Airlock", ProgramWriter.Blueprint(new AutomationSystem(1, Guid.NewGuid()) { Name = "Airlock", Namespace = "Shop", IsBlueprint = true }));
        var screen = Library(() => copied);
        var texts = screen.Editor.Texts;

        screen.Do("press", texts.LibraryImport).Run(2);
        screen.Do("press", texts.LibraryImportKeep).Run(2);

        Assert.Equal("Hangar Doors", Assert.Single(screen.Editor.Shelf!.All).Name);
    }

    /// <summary>Import with no blueprint text in Custom Data says so, and adds nothing.</summary>
    [Fact]
    public void NothingToImportSaysSo()
    {
        var screen = Library(() => "[another-script]\nspeed=4\n");

        screen.Do("press", screen.Editor.Texts.LibraryImport).Run(2);

        Assert.Single(screen.Editor.Shelf!.All);
        Assert.True(screen.Shows(screen.Editor.Texts.LibraryImportNone), screen.Dump);
    }
}
