using Sprocket.Host.SpaceEngineers.Memory;
using Sprocket.Host.SpaceEngineers.Shares;
using Sprocket.Host.SpaceEngineers.Storage;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// The screens the tour of the editor does not reach, in English and in Norwegian, measured with the game's own letter widths:
/// the admin settings on each of their tabs, the dialog that shares a blueprint, and a step that moves items. No words run past
/// the side of what holds them, and no two runs of words are drawn over each other.
/// </summary>
public class DialogFitTests
{
    const long Me = 1;

    readonly ShareDesk _desk;

    /// <summary>A desk for shares that answers this player as a server answers a client.</summary>
    public DialogFitTests()
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

    /// <summary>The admin settings, on each tab they open to, fit.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("no")]
    public void TheAdminSettingsFit(string language)
    {
        var screen = Fonted(Rig.Demo());
        screen.Scene.Ship.Admin = true;
        screen.Scene.Reopen();
        screen.Run(2);
        var texts = screen.Editor.Texts;
        screen.Do("press", texts.AdminButton).Run(2);
        Assert.True(screen.Shows(texts.AdminTitle), "the admin settings did not open");

        var found = new List<string>();
        Look(screen, language, "this world", found);
        screen.Do("press", texts.AdminEveryWorld).Run(2);
        Look(screen, language, "every world", found);
        if (screen.Shows(texts.AdminSharing))
        {
            screen.Do("press", texts.AdminSharing).Run(2);
            Look(screen, language, "sharing", found);
        }

        Assert.True(found.Count == 0, string.Join("\n", found));
    }

    /// <summary>The dialog that shares a blueprint, opened from the blueprint in the library, fits.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("no")]
    public void TheShareDialogFits(string language)
    {
        var screen = Fonted(Rig.Demo());
        ShareBoard board = null!;
        // ReSharper disable once AccessToModifiedClosure - the board answers itself, and is set before it is ever asked.
        board = new ShareBoard(order => board.Heard(_desk.Hear(order, Me, false)));
        screen.Editor.Sharing = board;
        screen.Editor.Shelf = new LibraryShelf(new Folder(), delegate { });
        var texts = screen.Editor.Texts;
        screen.Editor.NewSystem("Airlock");
        screen.Run(2);
        screen.Editor.GoToRoot();
        screen.Run(2);
        screen.Do("press", texts.SystemsMakeBlueprint).Run(2);
        // The library asks the desk for the share limit as it opens, and offers sharing once it has heard it.
        screen.Editor.GoToRoot();
        screen.Run(2);
        screen.Do("press", texts.SystemsLoad).Run(2);
        // The second Airlock is the library's kept copy, under the construct's own.
        var kept = screen.Drawings.Last(drawn => drawn.IsText && drawn.Text == "Airlock");
        screen.Scene.Press(Screen.Ui(kept.At.X + 4f, screen.MiddleOf(kept)), 1);
        screen.Run(2);
        var found = new List<string>();
        Look(screen, language, "the library", found);
        screen.Do("press", texts.LibraryShare).Run(2);
        Look(screen, language, "the share dialog", found);

        Assert.True(found.Count == 0, string.Join("\n", found));
    }

    /// <summary>A step that moves items, on the selector while it is filled and as its line on the sequence, fits.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("no")]
    public void AStepThatMovesItemsFits(string language)
    {
        var rig = Rig.Demo();
        rig.Refinery(40, "Refinery 1");
        rig.Cargo(41, "Cargo 1");
        var screen = Fonted(rig);
        screen.Editor.NewButtonSystem("Stock");
        screen.Run(2);
        screen.Editor.PressAdd("task");
        screen.Editor.PressBlock("Refinery 1");
        screen.Run(2);
        var found = new List<string>();
        screen.Editor.PressChoice("move items");
        screen.Run(2);
        Look(screen, language, "move items chosen", found);

        ((Features.Fields.SlotsField)screen.Editor.Pick().Arguments!.Form.Fields[0]).Select();
        screen.Run(2);
        Look(screen, language, "where the items go", found);
        screen.Editor.PressBlock("Cargo 1");
        screen.Run(2);
        screen.Editor.PressPrimary();
        screen.Run(2);
        Look(screen, language, "move items filled", found);
        screen.Editor.PressPrimary();
        screen.Run(2);
        Look(screen, language, "the step's line", found);

        Assert.True(found.Count == 0, string.Join("\n", found));
    }

    /// <summary>Every term page of the codex, opened in a tip on the home screen, fits.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("no")]
    public void EveryCodexPageFits(string language)
    {
        var screen = Fonted(Rig.Demo());
        if (language.Length > 0)
        {
            screen.Scene.Speak(language);
            screen.Run(2);
        }

        var at = screen.Scene.Find(screen.Editor.Texts.SystemsNew);
        screen.Do("move", ((int)at.X).ToString(), ((int)at.Y).ToString(), "tick", (Features.Look.Styles.TipDelay + 2).ToString());
        var tip = screen.Scene.CodexTip;
        Assert.NotNull(tip);

        var ids = System.Xml.Linq.XDocument.Load(System.IO.Path.Combine(Sim.Content.ResxTexts.Beside, "Codex.resx")).Root!.Elements("data")
            .Select(data => (string)data.Attribute("name")!)
            .Where(name => name.StartsWith(Features.Codex.CodexPages.Prefix + "Term."))
            .Select(name => name.Substring(Features.Codex.CodexPages.Prefix.Length))
            .Distinct().ToList();
        var found = new List<string>();
        foreach (var id in ids)
        {
            var page = screen.Editor.Codex.Find(id);
            tip.Navigate(page);
            screen.Run(2);
            Assert.Same(page, tip.Page);
            foreach (var said in screen.Cut.Concat(screen.Overlapping))
                found.Add($"{id}: {said}");
        }

        Assert.True(ids.Count > 20, $"only {ids.Count} term pages were found");
        Assert.True(found.Count == 0, string.Join("\n", found));
    }

    /// <summary>A screen on <paramref name="rig" /> with the game's fonts, pressed in English.</summary>
    static Screen Fonted(Rig rig)
    {
        var screen = Screen.WithGameFonts(rig);
        Assert.True(screen.HasGameFonts, "the game's font was not found, so nothing was measured as the game measures it");
        return screen;
    }

    /// <summary>Looks at <paramref name="screen" /> as it is said in <paramref name="language" />, English where it is empty, and goes back to English to be pressed on.</summary>
    static void Look(Screen screen, string language, string where, List<string> into)
    {
        if (language.Length > 0)
        {
            screen.Scene.Speak(language);
            screen.Run(2);
        }

        foreach (var said in screen.Cut.Concat(screen.Overlapping))
            into.Add($"{where}: {said}");

        if (language.Length > 0)
        {
            screen.Scene.Speak("");
            screen.Run(2);
        }
    }
}
