using Sprocket.Host.SpaceEngineers.Features.Sharing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Storage;
using Sprocket.Host.SpaceEngineers.Framework.Storage.Memory;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>The Share menu, in the library and in the editor: sharing through the library, and the copies of a blueprint as text.</summary>
public class ShareMenuTests
{
    const long Me = 1;

    readonly ShareDesk _desk;

    /// <summary>A desk for shares that answers this player as a server answers a client.</summary>
    public ShareMenuTests()
    {
        var players = new Dictionary<long, PlayerMemory>();
        PlayerMemory Player(long id)
        {
            if (!players.TryGetValue(id, out var memory))
                players[id] = memory = new PlayerMemory();
            return memory;
        }

        var world = new BlueprintShares(new List<SharedBlueprint>(), new List<long>(), Player, delegate { return false; }, new Values(), () => 1);
        _desk = new ShareDesk(world, () => 100, _ => "Me", () => new List<long> { Me }, Player, () => new List<long> { Me }, delegate { });
    }

    /// <summary>A screen with one logic system, Airlock, on the construct and the library kept, sharing allowed where <paramref name="sharing" />.</summary>
    Screen Editing(bool sharing)
    {
        var screen = Screen.On(Rig.Demo());
        screen.Editor.Shelf = new LibraryShelf(new Folder(), delegate { });
        if (sharing)
        {
            ShareBoard board = null!;
            // ReSharper disable once AccessToModifiedClosure - the board answers itself, and is set before it is ever asked.
            board = new ShareBoard(order => board.Heard(_desk.Hear(order, Me, false)));
            screen.Editor.Sharing = board;
            board.Changed += screen.Editor.Invalidate;
            board.Ask();
        }

        screen.Editor.NewSystem("Airlock");
        // The name a new logic system arrives to be given is settled before anything else is pressed.
        screen.Run(2).Do("enter").Run(2);
        return screen;
    }

    static AutomationSystem Airlock(Screen screen) => screen.Editor.Program.Systems.Single(system => system.Name.StartsWith("Airlock"));

    /// <summary>Chooses the library's copy of Airlock, the second of the two the library lists.</summary>
    static void ChooseKept(Screen screen)
    {
        var kept = screen.Drawings.Last(drawn => drawn.IsText && drawn.Text == "Airlock");
        screen.Scene.Press(Screen.Ui(kept.At.X + 4f, screen.MiddleOf(kept)), 1);
        screen.Run(2);
    }

    static bool DialogIsOpen(Screen screen) => screen.Shows(screen.Editor.Texts.ShareInWorld);

    /// <summary>The library's Share menu offers Share... and both copies, and Share... opens a dialog that is only about who to share with.</summary>
    [Fact]
    public void TheLibraryShareMenuOffersTheDialogAndTheCopies()
    {
        var screen = Editing(true);
        var texts = screen.Editor.Texts;
        screen.Editor.Holdings.Shelve(Airlock(screen));
        screen.Editor.OpenLibrary();
        screen.Run(2);
        ChooseKept(screen);

        screen.PressDetailShare();
        Assert.True(screen.Shows(texts.LibraryShare), screen.Dump);
        Assert.True(screen.Shows(texts.ShareCopyText), screen.Dump);
        Assert.True(screen.Shows(texts.ShareCopyCompact), screen.Dump);

        screen.Do("press", texts.LibraryShare).Run(2);
        Assert.True(screen.Shows(texts.ShareConfirm), screen.Dump);
        Assert.False(screen.Shows(texts.ShareCopyText), screen.Dump);
    }

    /// <summary>Where the world allows no sharing, the library's Share menu holds the copies alone.</summary>
    [Fact]
    public void WithoutSharingTheLibraryMenuHoldsTheCopiesAlone()
    {
        var screen = Editing(false);
        var texts = screen.Editor.Texts;
        screen.Editor.Holdings.Shelve(Airlock(screen));
        screen.Editor.OpenLibrary();
        screen.Run(2);
        ChooseKept(screen);

        screen.PressDetailShare();

        Assert.False(screen.Shows(texts.LibraryShare), screen.Dump);
        Assert.True(screen.Shows(texts.ShareCopyText), screen.Dump);
    }

    /// <summary>The copies in the editor put the logic system as it is on screen on the clipboard, with no save to the library, in single player too.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheEditorCopiesTheSystemAsItIsOnScreen(bool compact)
    {
        var screen = Editing(false);
        var texts = screen.Editor.Texts;
        screen.Editor.Screen.Heading[0].Renames!("Hangar");
        screen.Run(40);

        screen.Do("press", Symbols.Share).Run(2);
        Assert.False(screen.Shows(texts.LibraryShare), screen.Dump);
        screen.Do("press", compact ? texts.ShareCopyCompact : texts.ShareCopyText).Run(2);

        var copied = screen.Editor.Input.Clipboard.Get();
        Assert.StartsWith(compact ? "[" : "sprocket ", copied);
        Assert.Contains("Hangar", copied);
        Assert.Empty(screen.Editor.Shelf!.All);
    }

    /// <summary>Share... on a logic system that is not in the library puts it there and opens the dialog.</summary>
    [Fact]
    public void ShareFromTheEditorMakesTheBlueprintFirst()
    {
        var screen = Editing(true);
        var texts = screen.Editor.Texts;

        screen.Do("press", Symbols.Share).Run(2);
        screen.Do("press", texts.LibraryShare).Run(2);

        Assert.Single(screen.Editor.Shelf!.All);
        Assert.True(DialogIsOpen(screen), screen.Dump);
    }

    /// <summary>Share... on a logic system that differs from its blueprint asks first: confirming updates the library and opens the dialog, keeping does neither.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ShareFromTheEditorAsksWhereTheBlueprintDiffers(bool update)
    {
        var screen = Editing(true);
        var texts = screen.Editor.Texts;
        screen.Editor.Holdings.Shelve(Airlock(screen));
        screen.Run(2);
        screen.Editor.Screen.Heading[0].Renames!("Hangar");
        screen.Run(2);

        screen.Do("press", Symbols.Share).Run(2);
        screen.Do("press", texts.LibraryShare).Run(2);
        Assert.True(screen.Shows(texts.LibraryUpdateQuestion), screen.Dump);
        Assert.False(DialogIsOpen(screen), screen.Dump);

        screen.Do("press", update ? texts.LibraryUpdateConfirm : texts.LibraryUpdateKeep).Run(2);

        Assert.Equal(update, DialogIsOpen(screen));
        Assert.Equal(update ? "Hangar" : "Airlock", Assert.Single(screen.Editor.Shelf!.All).Name);
    }

    /// <summary>Share... on a logic system identical to its blueprint opens the dialog at once.</summary>
    [Fact]
    public void ShareFromTheEditorOpensAtOnceWhereTheBlueprintMatches()
    {
        var screen = Editing(true);
        var texts = screen.Editor.Texts;
        screen.Editor.Holdings.Shelve(Airlock(screen));
        screen.Run(2);

        screen.Do("press", Symbols.Share).Run(2);
        screen.Do("press", texts.LibraryShare).Run(2);

        Assert.False(screen.Shows(texts.LibraryUpdateQuestion), screen.Dump);
        Assert.True(DialogIsOpen(screen), screen.Dump);
    }

    /// <summary>On the library's construct side a logic system's row has the share mark: its menu offers Share... and the copies, and Share... saves the system to the library first.</summary>
    [Fact]
    public void ALogicSystemOnTheConstructSideOffersTheMenu()
    {
        var screen = Editing(true);
        var texts = screen.Editor.Texts;
        screen.Editor.OpenLibrary();
        screen.Run(2);

        screen.PressShare();
        Assert.True(screen.Shows(texts.LibraryShare), screen.Dump);
        Assert.True(screen.Shows(texts.ShareCopyText), screen.Dump);
        Assert.True(screen.Shows(texts.ShareCopyCompact), screen.Dump);
        Assert.Empty(screen.Editor.Shelf!.All);

        screen.Do("press", texts.LibraryShare).Run(2);

        Assert.Single(screen.Editor.Shelf!.All);
        Assert.True(DialogIsOpen(screen), screen.Dump);
    }

    /// <summary>On the construct side, copying puts the system as it is on screen on the clipboard, with no save to the library.</summary>
    [Fact]
    public void TheConstructSideCopiesWithoutSaving()
    {
        var screen = Editing(false);
        var texts = screen.Editor.Texts;
        screen.Editor.OpenLibrary();
        screen.Run(2);

        screen.PressShare();
        Assert.False(screen.Shows(texts.LibraryShare), screen.Dump);
        screen.Do("press", texts.ShareCopyText).Run(2);

        Assert.StartsWith("sprocket ", screen.Editor.Input.Clipboard.Get());
        Assert.Empty(screen.Editor.Shelf!.All);
    }

    /// <summary>The library's share is the same mark as everywhere, in the chosen blueprint's detail alone, and pressing it opens the menu.</summary>
    [Fact]
    public void TheLibraryDetailSharesWithTheSameMark()
    {
        var screen = Editing(true);
        var texts = screen.Editor.Texts;
        screen.Editor.Holdings.Shelve(Airlock(screen));
        screen.Editor.OpenLibrary();
        screen.Run(2);
        ChooseKept(screen);

        Assert.Single(screen.Marks(Symbols.Share));
        screen.PressDetailShare();
        Assert.True(screen.Shows(texts.ShareCopyText), screen.Dump);
    }

    /// <summary>A logic system's group on the home page has the same menu: Share... makes the blueprint and opens the dialog, and the copies work.</summary>
    [Fact]
    public void TheHomeGroupHasTheShareMenu()
    {
        var screen = Editing(true);
        var texts = screen.Editor.Texts;
        screen.Editor.GoToRoot();
        screen.Run(2);

        screen.Do("press", Symbols.Share).Run(2);
        screen.Do("press", texts.LibraryShare).Run(2);

        Assert.Single(screen.Editor.Shelf!.All);
        Assert.True(DialogIsOpen(screen), screen.Dump);
    }

    /// <summary>The home group's copy puts the logic system on the clipboard, and its Share menu holds the copies alone without sharing.</summary>
    [Fact]
    public void TheHomeGroupCopiesWithoutSharing()
    {
        var screen = Editing(false);
        var texts = screen.Editor.Texts;
        screen.Editor.GoToRoot();
        screen.Run(2);

        screen.Do("press", Symbols.Share).Run(2);
        Assert.False(screen.Shows(texts.LibraryShare), screen.Dump);
        screen.Do("press", texts.ShareCopyText).Run(2);

        Assert.StartsWith("sprocket ", screen.Editor.Input.Clipboard.Get());
    }
}

/// <summary>Presses the share marks on a screen.</summary>
static class SharePresses
{
    /// <summary>Presses the share mark that stands <paramref name="nth" /> from the left, counting from 0, and lets the screen settle.</summary>
    public static Screen PressShare(this Screen screen, int nth = 0)
    {
        var mark = screen.Marks(Symbols.Share).OrderBy(drawn => drawn.At.X).ElementAt(nth);
        screen.Scene.Press(Screen.Ui(mark.At.X + 4f, screen.MiddleOf(mark)), 1);
        return screen.Run(2);
    }

    /// <summary>Presses the share mark in the library's detail, the rightmost on the screen.</summary>
    public static Screen PressDetailShare(this Screen screen)
    {
        var marks = screen.Marks(Symbols.Share);
        return screen.PressShare(marks.Count - 1);
    }
}
