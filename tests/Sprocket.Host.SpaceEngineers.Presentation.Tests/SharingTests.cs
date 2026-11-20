using Sprocket.Host.SpaceEngineers.Features.Library;
using Sprocket.Host.SpaceEngineers.Features.Sharing;
using Sprocket.Host.SpaceEngineers.Features.Sharing.Messages;
using Sprocket.Host.SpaceEngineers.Framework.Language.Writing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Network;
using Sprocket.Host.SpaceEngineers.Framework.Storage;
using Sprocket.Host.SpaceEngineers.Framework.Storage.Memory;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using Lumel.Controls;
using Lumel.Input;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>Blueprints other players share show in the library, and a player can use them, keep a copy, or block the sharer.</summary>
public class SharingTests
{
    const long Me = 1;
    const long Ann = 2;

    readonly Dictionary<long, PlayerMemory> _players = new Dictionary<long, PlayerMemory>();
    readonly List<SharedBlueprint> _shares = new List<SharedBlueprint>();
    readonly List<long> _barred = new List<long>();
    readonly List<KeyValuePair<long, ShareTiding>> _told = new List<KeyValuePair<long, ShareTiding>>();
    int _now = 1;
    int _limit = 100;
    readonly BlueprintShares _world;
    readonly ShareDesk _desk;

    public SharingTests()
    {
        _world = new BlueprintShares(_shares, _barred, Player, (_, _) => false, new Values(), () => _now);
        _desk = new ShareDesk(_world, () => _limit, player => player == Ann ? "Ann" : "Me", () => new List<long> { Me, Ann }, Player, () => new List<long> { Me, Ann }, (player, tiding) => _told.Add(new KeyValuePair<long, ShareTiding>(player, tiding)));
    }

    /// <summary>What another player shares is listed with who shared it.</summary>
    [Fact]
    public void AShareIsListedWithItsSharer()
    {
        _world.Share(Ann, Blueprint("Hangar"), ShareReach.Everyone, null, 100);
        var rig = Sharing();

        _now = 2;
        Library(rig);
        var page = Library(rig);

        var row = page.Rows.OfType<StatusRow>().Single(r => r.Summary == "Hangar");
        Assert.Equal("Shared by Ann", row.Status);
    }

    /// <summary>A player away when something is shared finds it marked, and opening the library counts it as seen.</summary>
    [Fact]
    public void WhatChangedWhileAwayIsMarkedUntilTheLibraryIsOpened()
    {
        var rig = Sharing();
        Offer(Blueprint("Hangar"), ShareReach.Everyone);
        var board = rig.Editor.Sharing!;

        board.Ask();
        Assert.True(board.HasNew);

        var page = Library(rig);
        Assert.Equal("New, shared by Ann", page.Rows.OfType<StatusRow>().Single(r => r.Summary == "Hangar").Status);

        _now = 2;
        board.Look();
        board.Ask();
        Assert.False(board.HasNew);
    }

    /// <summary>In single player the library says nothing of sharing.</summary>
    [Fact]
    public void SinglePlayerShowsNoSharing()
    {
        var rig = Simulation.Fresh();

        var page = Library(rig);

        Assert.DoesNotContain(page.Rows.OfType<Lumel.Text.Label>(), label => label.Text == "SHARED WITH YOU");
    }

    /// <summary>The arrow into the library, while a shared blueprint is chosen, cannot be pressed and has no tip, since Save a copy is what saves it.</summary>
    [Fact]
    public void TheArrowIntoTheLibrarySaysNothingOfASharedBlueprint()
    {
        _world.Share(Ann, Blueprint("Hangar"), ShareReach.Everyone, null, 100);
        var rig = Sharing();
        Library(rig);
        var page = Library(rig);

        page.Rows.OfType<StatusRow>().Single(r => r.Summary == "Hangar").Opens!.Execute();
        rig.Tick();

        var arrow = page.Moves.ToLibrary;
        Assert.False(arrow.Command!.CanExecute, "the arrow keeps a shared blueprint");
        Assert.Null(arrow.Tip);
    }

    /// <summary>A copy kept is the player's own: it keeps its maker's name and outlasts the share.</summary>
    [Fact]
    public void ASavedCopyOutlastsTheShare()
    {
        var identity = Guid.NewGuid();
        _world.Share(Ann, Blueprint("Hangar", identity), ShareReach.Everyone, null, 100);
        var rig = Sharing();
        Library(rig);

        rig.Editor.Holdings.SaveCopy(rig.Editor.Sharing!.Reaching[0]);
        rig.Tick();
        _world.Withdraw(Ann, identity);

        var kept = rig.Editor.Shelf!.All.Single();
        Assert.NotEqual(identity, kept.Identity);
        Assert.Equal("Shop", kept.Namespace);
        Assert.Equal("Hangar", kept.Name);
    }

    /// <summary>A logic system put on from a share is the construct's own, linked to nothing that could update it.</summary>
    [Fact]
    public void ABlueprintPutOnFromAShareStandsAlone()
    {
        var identity = Guid.NewGuid();
        _world.Share(Ann, Blueprint("Hangar", identity), ShareReach.Everyone, null, 100);
        var rig = Sharing();
        Library(rig);

        Assert.Equal("", rig.Editor.Holdings.PutOnShared(rig.Editor.Sharing!.Reaching[0]));
        rig.Tick();

        var made = rig.Editor.Program.Systems.Single(system => system.Name == "Hangar");
        Assert.NotEqual(identity, made.Identity);
        Assert.False(made.HasOrigin);
    }

    /// <summary>Blocking takes the sharer's blueprints out of the list and offers the way back.</summary>
    [Fact]
    public void ABlockedSharerGoesAndCanBeLetBack()
    {
        _world.Share(Ann, Blueprint("Hangar"), ShareReach.Everyone, null, 100);
        var rig = Sharing();
        var board = rig.Editor.Sharing!;
        board.Ask();

        board.Block(Ann);
        Assert.Empty(board.Reaching);
        Assert.True(board.Known.Single().IsBlocked);

        board.Unblock(Ann);
        Assert.Single(board.Reaching);
    }

    /// <summary>Only an admin can take back another player's share or bar a player.</summary>
    [Fact]
    public void OnlyAnAdminActsOnOthers()
    {
        var identity = Guid.NewGuid();
        _world.Share(Ann, Blueprint("Hangar", identity), ShareReach.Everyone, null, 100);

        _desk.Hear(new ShareOrder { Act = ShareAct.Withdraw, Target = Ann, Identity = identity.ToString("D") }, Me, false);
        _desk.Hear(new ShareOrder { Act = ShareAct.Bar, Target = Ann }, Me, false);
        Assert.Single(_world.All);
        Assert.Empty(_world.Barred);

        _desk.Hear(new ShareOrder { Act = ShareAct.Withdraw, Target = Ann, Identity = identity.ToString("D") }, Me, true);
        _desk.Hear(new ShareOrder { Act = ShareAct.Bar, Target = Ann }, Me, true);
        Assert.Empty(_world.All);
        Assert.Equal(new[] { Ann }, _world.Barred);
    }

    /// <summary>A new share tells whoever it reaches, and nobody else; the sharer hears nothing.</summary>
    [Fact]
    public void ANewShareIsToldToThoseItReaches()
    {
        Offer(Blueprint("Hangar"), ShareReach.Everyone);

        var told = Assert.Single(_told);
        Assert.Equal(Me, told.Key);
        Assert.False(told.Value.IsUpdate);
        Assert.Equal("Hangar", told.Value.Name);
        Assert.Equal("Ann", told.Value.Sharer);
    }

    /// <summary>Sharing a changed blueprint again is told as an update; sharing the same one again tells nobody.</summary>
    [Fact]
    public void AChangedShareIsToldAsAnUpdate()
    {
        var identity = Guid.NewGuid();
        Offer(Blueprint("Hangar", identity), ShareReach.Everyone);
        _told.Clear();

        Offer(Blueprint("Hangar", identity), ShareReach.Everyone);
        Assert.Empty(_told);

        Offer(Blueprint("Hangar Mark 2", identity), ShareReach.Everyone);
        Assert.True(Assert.Single(_told).Value.IsUpdate);
    }

    /// <summary>A blueprint the player shares goes out again as it changes in their library, to the same people.</summary>
    [Fact]
    public void ChangingABlueprintInTheLibraryRenewsItsShare()
    {
        var rig = Sharing();
        var board = rig.Editor.Sharing!;
        var identity = Guid.NewGuid();
        board.Share(Blueprint("Hangar", identity), ShareReach.Players, new[] { Ann });

        board.Renew(identity, Blueprint("Hangar Mark 2", identity));

        var share = _world.All.Single();
        Assert.Equal("Hangar Mark 2", share.Name);
        Assert.Equal(ShareReach.Players, share.Reach);
        Assert.Equal(new[] { Ann }, share.Players);
        Assert.True(_told.Last().Value.IsUpdate);
        Assert.Equal(Ann, _told.Last().Key);
    }

    /// <summary>A blueprint the player doesn't share stays theirs alone as it changes.</summary>
    [Fact]
    public void ABlueprintNotSharedGoesNowhere()
    {
        var rig = Sharing();
        var identity = Guid.NewGuid();

        rig.Editor.Sharing!.Renew(identity, Blueprint("Hangar", identity));

        Assert.Empty(_world.All);
    }

    /// <summary>A player who has blocked the sharer is told nothing.</summary>
    [Fact]
    public void ABlockedSharerTellsNobody()
    {
        _world.Block(Me, Ann);

        Offer(Blueprint("Hangar"), ShareReach.Everyone);

        Assert.Empty(_told);
    }

    /// <summary>A tiding comes off the wire as it went on.</summary>
    [Fact]
    public void ATidingSurvivesTheWire()
    {
        var tiding = new ShareTiding { IsUpdate = true, Name = "Hangar", Sharer = "Ann" };
        var data = new byte[ShareTiding.SizeOf(ref tiding)];
        var offset = 0;
        ShareTiding.Write(ref tiding, data, ref offset);

        offset = 0;
        ShareTiding.Read(data, ref offset, out var read);

        Assert.True(read.IsUpdate);
        Assert.Equal("Hangar", read.Name);
        Assert.Equal("Ann", read.Sharer);
    }

    /// <summary>An order comes off the wire as it went on.</summary>
    [Fact]
    public void AnOrderSurvivesTheWire()
    {
        var order = new ShareOrder { Act = ShareAct.Share, Reach = ShareReach.Players, Target = 9, Identity = "id", Players = "2 3", Blueprint = "text" };
        var data = new byte[ShareOrder.SizeOf(ref order)];
        var offset = 0;
        ShareOrder.Write(ref order, data, ref offset);

        offset = 0;
        ShareOrder.Read(data, ref offset, out var read);

        Assert.Equal(ShareAct.Share, read.Act);
        Assert.Equal(ShareReach.Players, read.Reach);
        Assert.Equal(9, read.Target);
        Assert.Equal("id", read.Identity);
        Assert.Equal("2 3", read.Players);
        Assert.Equal("text", read.Blueprint);
    }

    /// <summary>News comes off the wire as it went on, with what it holds in order.</summary>
    [Fact]
    public void NewsSurvivesTheWire()
    {
        var news = new ShareNews
        {
            Answer = ShareAnswer.AtLimit,
            Limit = 25,
            IsBarred = true,
            Reaching = new List<ShareLine> { new ShareLine { Sharer = 2, SharerName = "Ann", Identity = "id", Name = "Hangar", Reach = ShareReach.Faction, Blueprint = "text" } },
            Mine = new List<ShareLine>(),
            All = new List<ShareLine> { new ShareLine { Sharer = 3, Players = "1 2" } },
            Known = new List<KnownPlayer> { new KnownPlayer { Identity = 2, Name = "Ann", IsBlocked = true } }
        };
        var data = new byte[ShareNews.SizeOf(ref news)];
        var offset = 0;
        ShareNews.Write(ref news, data, ref offset);

        offset = 0;
        ShareNews.Read(data, ref offset, out var read);

        Assert.Equal(ShareAnswer.AtLimit, read.Answer);
        Assert.Equal(25, read.Limit);
        Assert.True(read.IsBarred);
        var line = Assert.Single(read.Reaching);
        Assert.Equal("Hangar", line.Name);
        Assert.Equal(ShareReach.Faction, line.Reach);
        Assert.Equal("text", line.Blueprint);
        Assert.Empty(read.Mine);
        Assert.Equal("1 2", Assert.Single(read.All).Players);
        Assert.True(Assert.Single(read.Known).IsBlocked);
    }

    /// <summary>A new share opens on the player's faction, and on players they choose where they are in no faction.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ANewShareOpensOnTheFactionWhereThereIsOne(bool inFaction)
    {
        var screen = Screen.On(Rig.Demo());
        ShareBoard board = null!;
        // ReSharper disable once AccessToModifiedClosure - the board answers itself, and is set before it is ever asked.
        board = new ShareBoard(order => board.Heard(_desk.Hear(order, Me, false)));
        screen.Editor.Sharing = board;
        screen.Editor.IsInFaction = () => inFaction;
        screen.Editor.Shelf = new LibraryShelf(new Folder(), delegate { });
        screen.Editor.Shelf.Save(new AutomationSystem(1, Guid.NewGuid()) { Name = "Hangar", Namespace = "Shop", IsBlueprint = true });
        var texts = screen.Editor.Texts;
        screen.Editor.OpenLibrary();
        screen.Run(2);
        screen.Do("press", "Hangar").Run(2);
        screen.PressDetailShare();
        screen.Do("press", texts.LibraryShare).Run(2);

        var who = Target.Within(screen.Scene.Workspace.Windows.Top).OfType<ListField>().Single();
        Assert.Equal(inFaction ? texts.ShareFaction : texts.SharePlayers, who.Text);
    }

    /// <summary>A player ticked in the share dialog, with Chosen players picked, is the one the blueprint is shared with.</summary>
    [Fact]
    public void ATickedPlayerIsSharedWith()
    {
        var screen = Screen.On(Rig.Demo());
        ShareBoard board = null!;
        // ReSharper disable once AccessToModifiedClosure - the board answers itself, and is set before it is ever asked.
        board = new ShareBoard(order => board.Heard(_desk.Hear(order, Me, false)));
        screen.Editor.Sharing = board;
        board.Changed += screen.Editor.Invalidate;
        board.Ask();
        screen.Editor.Shelf = new LibraryShelf(new Folder(), delegate { });
        screen.Editor.Shelf.Save(new AutomationSystem(1, Guid.NewGuid()) { Name = "Hangar", Namespace = "Shop", IsBlueprint = true });
        var texts = screen.Editor.Texts;
        screen.Editor.OpenLibrary();
        screen.Run(2);
        screen.Do("press", "Hangar").Run(2);
        screen.PressDetailShare();
        screen.Do("press", texts.LibraryShare).Run(2);

        var who = Target.Within(screen.Scene.Workspace.Windows.Top).OfType<ListField>().Single();
        ((IInputTarget)who).Text(texts.SharePlayers[0]);
        screen.Run(2);
        Assert.Equal(texts.SharePlayers, who.Text);
        screen.Do("press", "Ann").Run(2);
        var ann = Target.Within(screen.Scene.Workspace.Windows.Top).OfType<CheckBox>().Single(tick => tick.Text == "Ann");
        Assert.True(ann.IsChecked, screen.Dump);
        screen.Do("press", texts.ShareConfirm).Run(2);

        var shared = Assert.Single(_shares);
        Assert.Equal(ShareReach.Players, shared.Reach);
        Assert.Contains(Ann, shared.Players);
    }

    /// <summary>
    /// With the world's limit set to 0 while the library is open, sharing is refused with the reason the world gives, and the
    /// Share menu opened again offers only the copies.
    /// </summary>
    [Fact]
    public void ALimitOfNoneRefusesWithItsReason()
    {
        var screen = Screen.On(Rig.Demo());
        ShareBoard board = null!;
        // ReSharper disable once AccessToModifiedClosure - the board answers itself, and is set before it is ever asked.
        board = new ShareBoard(order => board.Heard(_desk.Hear(order, Me, false)));
        screen.Editor.Sharing = board;
        // The session draws the editor again whenever the board hears news (SprocketSession.SharesChanged).
        board.Changed += screen.Editor.Invalidate;
        screen.Editor.Shelf = new LibraryShelf(new Folder(), delegate { });
        var texts = screen.Editor.Texts;
        screen.Editor.NewSystem("Airlock");
        screen.Run(2);
        screen.Editor.GoToRoot();
        screen.Run(2);
        screen.Do("press", texts.SystemsMakeBlueprint).Run(2);
        screen.Editor.GoToRoot();
        screen.Run(2);
        screen.Do("press", texts.SystemsLoad).Run(2);
        // The second Airlock is the library's kept copy, under the construct's own.
        var kept = screen.Drawings.Last(drawn => drawn.IsText && drawn.Text == "Airlock");
        screen.Scene.Press(Screen.Ui(kept.At.X + 4f, screen.MiddleOf(kept)), 1);
        screen.Run(2);

        _limit = 0;
        screen.PressDetailShare();
        screen.Do("press", texts.LibraryShare).Run(2);
        screen.Do("press", texts.ShareConfirm).Run(2);
        Assert.True(screen.Shows(texts.LibraryShareClosed), screen.Dump);

        screen.Editor.GoToRoot();
        screen.Run(2);
        screen.Do("press", texts.SystemsLoad).Run(2);
        kept = screen.Drawings.Last(drawn => drawn.IsText && drawn.Text == "Airlock");
        screen.Scene.Press(Screen.Ui(kept.At.X + 4f, screen.MiddleOf(kept)), 1);
        screen.Run(2);
        screen.PressDetailShare();
        Assert.True(screen.Shows(texts.ShareCopyText), screen.Dump);
        Assert.False(screen.Shows(texts.LibraryShare), screen.Dump);
    }

    /// <summary>Ann offers <paramref name="blueprint" /> for those <paramref name="reach" /> says, through the desk.</summary>
    void Offer(string blueprint, ShareReach reach)
    {
        Assert.Equal(ShareAnswer.Shared, _desk.Hear(new ShareOrder { Act = ShareAct.Share, Reach = reach, Blueprint = blueprint }, Ann, false).Answer);
    }

    /// <summary>A rig whose player talks to this test's desk as a client talks to the server.</summary>
    Simulation Sharing()
    {
        var rig = Simulation.Fresh();
        ShareBoard board = null!;
        // ReSharper disable once AccessToModifiedClosure - the board answers itself, and is set before it is ever asked.
        board = new ShareBoard(order => board.Heard(_desk.Hear(order, Me, false)));
        rig.Editor.Sharing = board;
        return rig;
    }

    static LibraryPage Library(Simulation rig)
    {
        var page = new LibraryPage(rig.Editor, Guid.Empty);
        rig.Editor.Push(page);
        rig.Tick();
        Assert.NotNull(page.View);
        return page;
    }

    PlayerMemory Player(long identity)
    {
        if (!_players.TryGetValue(identity, out var player))
            _players[identity] = player = new PlayerMemory { Player = identity };

        return player;
    }

    static string Blueprint(string name, Guid? identity = null)
    {
        return ProgramWriter.Blueprint(new AutomationSystem(1, identity ?? Guid.NewGuid()) { Name = name, Namespace = "Shop", IsBlueprint = true });
    }
}
