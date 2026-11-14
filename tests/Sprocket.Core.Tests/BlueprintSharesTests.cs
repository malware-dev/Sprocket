using Sprocket.Host.SpaceEngineers.Features.Sharing;
using Sprocket.Host.SpaceEngineers.Framework.Language.Writing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Storage.Memory;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Tests;

/// <summary>What the server keeps of shared blueprints, and who sees them.</summary>
public class BlueprintSharesTests
{
    const long Ann = 1;
    const long Bo = 2;
    const long Cy = 3;
    const int Limit = 100;

    int _now = 1;

    readonly Dictionary<long, PlayerMemory> _players = new Dictionary<long, PlayerMemory>();
    readonly List<SharedBlueprint> _kept = new List<SharedBlueprint>();
    readonly List<long> _barred = new List<long>();

    /// <summary>Everyone is reached by a share for everyone, the sharer's own list excepted.</summary>
    [Fact]
    public void AShareForEveryoneReachesEveryoneElse()
    {
        var shares = Shares();

        Assert.Equal(ShareAnswer.Shared, shares.Share(Ann, Blueprint("Airlock"), ShareReach.Everyone, null, Limit));

        Assert.Equal("Airlock", shares.Reaching(Bo).Single().Name);
        Assert.Empty(shares.Reaching(Ann));
        Assert.Single(shares.By(Ann));
    }

    /// <summary>A faction share follows the sharer's faction as it is when somebody looks.</summary>
    [Fact]
    public void AFactionShareReachesWhoeverIsInTheFactionNow()
    {
        var together = true;
        // The share asks whether two players are together when it is read, so the test changes it after sharing.
        // ReSharper disable once AccessToModifiedClosure
        var shares = Shares((_, _) => together);
        shares.Share(Ann, Blueprint("Airlock"), ShareReach.Faction, null, Limit);

        Assert.Single(shares.Reaching(Bo));

        together = false;
        Assert.Empty(shares.Reaching(Bo));
    }

    /// <summary>A share for named players reaches them and nobody else.</summary>
    [Fact]
    public void ASharePickedByNameReachesThosePlayersAlone()
    {
        var shares = Shares();
        shares.Share(Ann, Blueprint("Airlock"), ShareReach.Players, new[] { Bo }, Limit);

        Assert.Single(shares.Reaching(Bo));
        Assert.Empty(shares.Reaching(Cy));
    }

    /// <summary>Sharing a blueprint again replaces the share, so a new version and a new reach take its place.</summary>
    [Fact]
    public void SharingAgainReplacesTheShare()
    {
        var shares = Shares();
        var identity = Guid.NewGuid();
        shares.Share(Ann, Blueprint("Airlock", identity), ShareReach.Everyone, null, Limit);

        shares.Share(Ann, Blueprint("Airlock Mark 2", identity), ShareReach.Players, new[] { Cy }, Limit);

        Assert.Equal("Airlock Mark 2", shares.All.Single().Name);
        Assert.Empty(shares.Reaching(Bo));
        Assert.Single(shares.Reaching(Cy));
    }

    /// <summary>A share is new to a player until they look, and new again only when its blueprint changes.</summary>
    [Fact]
    public void AShareIsNewUntilItIsSeenOrChanged()
    {
        var shares = Shares();
        var identity = Guid.NewGuid();
        shares.Share(Ann, Blueprint("Airlock", identity), ShareReach.Everyone, null, Limit);
        Assert.True(shares.IsNewTo(Bo, shares.All.Single()));

        _now = 2;
        shares.Saw(Bo);
        Assert.False(shares.IsNewTo(Bo, shares.All.Single()));

        _now = 3;
        shares.Share(Ann, Blueprint("Airlock", identity), ShareReach.Faction, null, Limit);
        Assert.False(shares.IsNewTo(Bo, shares.All.Single()));

        shares.Share(Ann, Blueprint("Airlock Mark 2", identity), ShareReach.Everyone, null, Limit);
        Assert.True(shares.IsNewTo(Bo, shares.All.Single()));
    }

    /// <summary>A share withdrawn is gone from everyone's list.</summary>
    [Fact]
    public void AWithdrawnShareIsGone()
    {
        var shares = Shares();
        var identity = Guid.NewGuid();
        shares.Share(Ann, Blueprint("Airlock", identity), ShareReach.Everyone, null, Limit);

        shares.Withdraw(Ann, identity);

        Assert.Empty(shares.Reaching(Bo));
        Assert.Empty(shares.All);
    }

    /// <summary>The limit stops a new share, leaves a replacement alone, and at nothing closes sharing.</summary>
    [Fact]
    public void TheLimitCountsEachPlayersShares()
    {
        var shares = Shares();
        var identity = Guid.NewGuid();
        shares.Share(Ann, Blueprint("One", identity), ShareReach.Everyone, null, 1);

        Assert.Equal(ShareAnswer.AtLimit, shares.Share(Ann, Blueprint("Two"), ShareReach.Everyone, null, 1));
        Assert.Equal(ShareAnswer.Shared, shares.Share(Ann, Blueprint("One again", identity), ShareReach.Everyone, null, 1));
        Assert.Equal(ShareAnswer.Shared, shares.Share(Bo, Blueprint("Theirs"), ShareReach.Everyone, null, 1));
        Assert.Equal(ShareAnswer.Closed, shares.Share(Cy, Blueprint("Three"), ShareReach.Everyone, null, 0));
    }

    /// <summary>What is no blueprint, or too long to be one, is refused.</summary>
    [Fact]
    public void WhatWillNotReadIsRefused()
    {
        var shares = Shares();

        Assert.Equal(ShareAnswer.Unreadable, shares.Share(Ann, "not a blueprint", ShareReach.Everyone, null, Limit));
        Assert.Equal(ShareAnswer.Unreadable, shares.Share(Ann, new string(' ', BlueprintShares.Longest + 1), ShareReach.Everyone, null, Limit));
        Assert.Empty(shares.All);
    }

    /// <summary>A player who blocks a sharer stops seeing what they share; others still do, and the sharer's list shows no sign.</summary>
    [Fact]
    public void BlockingHidesASharerFromThatPlayerAlone()
    {
        var shares = Shares();
        shares.Share(Ann, Blueprint("Airlock"), ShareReach.Everyone, null, Limit);

        shares.Block(Bo, Ann);

        Assert.Empty(shares.Reaching(Bo));
        Assert.Single(shares.Reaching(Cy));
        Assert.Single(shares.By(Ann));

        shares.Unblock(Bo, Ann);
        Assert.Single(shares.Reaching(Bo));
    }

    /// <summary>A barred player's shares go for everyone, and they can share nothing until they are let back.</summary>
    [Fact]
    public void BarringTakesBackEverythingAndStopsSharing()
    {
        var shares = Shares();
        shares.Share(Ann, Blueprint("Airlock"), ShareReach.Everyone, null, Limit);

        shares.Bar(Ann);

        Assert.Empty(shares.Reaching(Bo));
        Assert.Equal(ShareAnswer.Barred, shares.Share(Ann, Blueprint("Again"), ShareReach.Everyone, null, Limit));

        shares.Unbar(Ann);
        Assert.Equal(ShareAnswer.Shared, shares.Share(Ann, Blueprint("Again"), ShareReach.Everyone, null, Limit));
    }

    /// <summary>What the world keeps comes back from a save with its name and identity, and a block with its player.</summary>
    [Fact]
    public void SharesAndBarsAndBlocksSurviveASave()
    {
        var memories = new Memories(new ProtoBinary());
        var shares = new BlueprintShares(memories.Shares, memories.Barred, memories.Player, (_, _) => true, new Values(), () => 0);
        var identity = Guid.NewGuid();
        shares.Share(Ann, Blueprint("Airlock", identity), ShareReach.Players, new[] { Bo }, Limit);
        shares.Block(Cy, Ann);
        shares.Bar(Cy);

        var saved = memories.Write(0, new List<ConstructMemory>());
        var back = new Memories(new ProtoBinary());
        back.Read(saved, 0);
        var kept = new BlueprintShares(back.Shares, back.Barred, back.Player, (_, _) => true, new Values(), () => 0);

        var share = kept.All.Single();
        Assert.Equal(identity, share.Identity);
        Assert.Equal("Airlock", share.Name);
        Assert.Equal(new[] { Bo }, share.Players);
        Assert.Equal(new[] { Cy }, kept.Barred);
        Assert.Equal(new[] { Ann }, back.Player(Cy).Blocked);
    }

    BlueprintShares Shares(Func<long, long, bool>? sameFaction = null)
    {
        return new BlueprintShares(_kept, _barred, Player, sameFaction ?? ((_, _) => false), new Values(), () => _now);
    }

    PlayerMemory Player(long identity)
    {
        PlayerMemory player;
        if (!_players.TryGetValue(identity, out player!))
            _players[identity] = player = new PlayerMemory { Player = identity };

        return player;
    }

    static string Blueprint(string name, Guid? identity = null)
    {
        var system = new AutomationSystem(1, identity ?? Guid.NewGuid()) { Name = name, Namespace = "Shop", IsBlueprint = true };
        return ProgramWriter.Blueprint(system);
    }
}
