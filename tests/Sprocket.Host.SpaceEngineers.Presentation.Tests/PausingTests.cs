using Sprocket.Host.SpaceEngineers.Features.Home;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using Lumel.Controls;
using Lumel.Text;
using Sandbox.ModAPI;
using Sprocket.Sim.Fakes;
using Sprocket.Sim.Machine;
using VRage.Game.ModAPI;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// A construct changing hands within sharing keeps its automation running; changing hands outside it pauses the automation,
/// which then runs and fires nothing, and stays paused through a reload.
/// </summary>
public class PausingTests
{
    const long Anna = 144115188075855873;
    const long Bert = 144115188075855874;
    const long Cora = 144115188075855875;

    /// <summary>A ship shared with its faction, handed to a member of that faction, keeps running whether the new owner shares it with the faction or the game has turned its sharing off.</summary>
    [Theory]
    [InlineData(Sharing.Faction)]
    [InlineData(Sharing.Nobody)]
    public void AHandOverWithinTheFactionKeepsItRunning(Sharing sharing)
    {
        var sim = Airlock(out _);
        sim.Machine.Factions.Join(Anna, "MAL");
        sim.Machine.Factions.Join(Bert, "MAL");
        Owned(sim, Anna, Sharing.Faction);

        Owned(sim, Bert, sharing);

        Assert.False(sim.Editor.Program.Paused);
        Assert.Equal(new[] { Bert }, sim.Editor.Program.Owners);
    }

    /// <summary>A hand-over within the faction stores the automation running while its programmable block has belonged to somebody else all along.</summary>
    [Fact]
    public void AHandOverWithinTheFactionKeepsItRunningWhoeverOwnsTheProgrammableBlock()
    {
        var sim = Airlock(out _);
        sim.Machine.Factions.Join(Anna, "MAL");
        sim.Machine.Factions.Join(Bert, "MAL");
        sim.Machine.Own(Anna, Sharing.Faction);
        sim.Machine.Sprocket.Owner = Cora;
        sim.Tick();
        sim.Tick();

        sim.Machine.Own(Bert, Sharing.Faction);
        sim.Machine.Sprocket.Owner = Cora;
        sim.Tick();
        sim.Tick();

        Assert.False(sim.Editor.Program.Paused);
        Assert.Equal(new[] { Bert }, sim.Editor.Program.Owners);
    }

    /// <summary>A ship taken by somebody it was not shared with pauses, remembering who it was taken from.</summary>
    [Fact]
    public void ACapturePausesIt()
    {
        var sim = Airlock(out _);
        Owned(sim, Anna, Sharing.Nobody);

        Owned(sim, Bert, Sharing.Nobody);

        Assert.True(sim.Editor.Program.Paused);
        Assert.Equal(new[] { Anna }, sim.Editor.Program.PausedFrom);
        Assert.Equal(new[] { Bert }, sim.Editor.Program.Owners);
        Assert.False(sim.Runner.Live);
    }

    /// <summary>A ship taken from an admin using every terminal pauses as from anybody, as that use is no sharing.</summary>
    [Fact]
    public void ACaptureFromAnAdminPausesIt()
    {
        var sim = Airlock(out _);
        sim.Machine.Admins.Add(Anna);
        Owned(sim, Anna, Sharing.Nobody);

        Owned(sim, Bert, Sharing.Nobody);

        Assert.True(sim.Editor.Program.Paused);
        Assert.Equal(new[] { Anna }, sim.Editor.Program.PausedFrom);
    }

    /// <summary>A construct nobody owned becoming somebody's is no change of hands.</summary>
    [Fact]
    public void FirstOwnershipPausesNothing()
    {
        var sim = Airlock(out _);

        Owned(sim, Anna, Sharing.Nobody);

        Assert.False(sim.Editor.Program.Paused);
        Assert.Equal(new[] { Anna }, sim.Editor.Program.Owners);
    }

    /// <summary>A paused automation runs nothing when its button is pressed.</summary>
    [Fact]
    public void APausedButtonDoesNothing()
    {
        var sim = Airlock(out var sequence);
        Owned(sim, Anna, Sharing.Nobody);
        Owned(sim, Bert, Sharing.Nobody);

        sim.Press("Airlock Button");
        sim.Tick();

        var run = sim.Runner.Get(sequence, sequence.System.First);
        Assert.True(run == null || !run.IsRunning, "a paused automation ran");
    }

    /// <summary>Being paused is saved with the world, so a reload keeps it paused.</summary>
    [Fact]
    public void APauseOutlivesAReload()
    {
        var sim = Simulation.On(Store.Nowhere());
        sim.Editor.NewButtonSystem("Airlock");
        sim.Tick();
        Owned(sim, Anna, Sharing.Nobody);
        Owned(sim, Bert, Sharing.Nobody);

        sim.Reload();
        sim.Tick();

        Assert.True(sim.Editor.Program.Paused);
        Assert.Equal(new[] { Anna }, sim.Editor.Program.PausedFrom);
    }

    /// <summary>A copy of a paused construct, pasted from what its block keeps, is nobody's and runs.</summary>
    [Fact]
    public void APastedCopyStartsUnpaused()
    {
        var store = Store.Nowhere();
        var sim = Simulation.On(store);
        sim.Editor.NewButtonSystem("Airlock");
        sim.Tick();
        Owned(sim, Anna, Sharing.Nobody);
        Owned(sim, Bert, Sharing.Nobody);
        sim.Save();

        var pasted = Simulation.Pasted(store);
        pasted.Tick();

        Assert.False(pasted.Editor.Program.Paused);
    }

    /// <summary>Opening paused automation lands on the resume screen, which says why and how much it holds.</summary>
    [Fact]
    public void OpeningPausedAutomationLandsOnTheResumeScreen()
    {
        var sim = Captured();

        sim.Editor.Opened();
        sim.Tick();

        Assert.IsType<PausedPage>(sim.Editor.Current);
        var said = Said(sim);
        Assert.Contains("# Automation is paused", said);
        Assert.Contains("Test Rig passed from ", said);
        Assert.Contains("It holds one logic system.", said);
    }

    /// <summary>Resume starts it again and goes on to the home screen.</summary>
    [Fact]
    public void ResumeStartsItAgain()
    {
        var sim = Captured();
        sim.Editor.Opened();
        sim.Tick();

        Press(sim, sim.Texts.PausedResume);
        sim.Tick();

        Assert.False(sim.Editor.Program.Paused);
        Assert.IsType<HomePage>(sim.Editor.Current);
        Assert.True(sim.Runner.Live);
    }

    /// <summary>Leave paused goes on into the editor under a banner whose button opens the resume screen again.</summary>
    [Fact]
    public void LeavingItPausedLeavesABannerBack()
    {
        var sim = Captured();
        sim.Editor.Opened();
        sim.Tick();

        Press(sim, sim.Texts.PausedLeave);
        sim.Tick();

        Assert.True(sim.Editor.Program.Paused);
        Assert.IsType<HomePage>(sim.Editor.Current);
        var notice = sim.Editor.Notice!;
        Assert.Equal(NoticeLevel.Warning, notice.Level);
        Assert.Equal("Paused when Test Rig changed hands.", notice.Text);

        notice.Offers!.Execute();
        sim.Tick();
        Assert.IsType<PausedPage>(sim.Editor.Current);
    }

    /// <summary>Remove all asks first, then takes every logic system out and starts what is left.</summary>
    [Fact]
    public void RemoveAllAsksAndRemovesEveryLogicSystem()
    {
        var sim = Captured();
        sim.Editor.Opened();
        sim.Tick();

        Press(sim, sim.Texts.PausedRemoveAll);
        sim.Tick();
        Assert.Equal("Remove the logic system on Test Rig?", sim.Editor.Question());

        sim.Editor.PressChoice(sim.Texts.PausedRemoveAll);
        sim.Tick();

        Assert.Empty(sim.Editor.Program.Systems);
        Assert.False(sim.Editor.Program.Paused);
    }

    /// <summary>A block the new owners may not use is counted as out of reach.</summary>
    [Fact]
    public void ABlockOutOfReachIsCounted()
    {
        var sim = Captured();
        var door = sim.Machine.Fitted.First(fitting => fitting.Name == "Hangar Door");
        door.Owner = Anna;
        sim.Tick();

        sim.Editor.Opened();
        sim.Tick();

        Assert.Contains("One of its blocks is out of its reach.", Said(sim));
    }

    /// <summary>A block the old owners could not use and the new ones may is counted as back in reach.</summary>
    [Fact]
    public void ABlockBackInReachIsCounted()
    {
        var sim = Airlock(out _);
        Owned(sim, Anna, Sharing.Nobody);
        var door = sim.Machine.Fitted.First(fitting => fitting.Name == "Hangar Door");
        door.Owner = Bert;
        sim.Tick();
        sim.Tick();

        sim.Machine.Own(Bert);
        sim.Tick();
        sim.Tick();
        sim.Editor.Opened();
        sim.Tick();

        Assert.True(sim.Editor.Program.Paused);
        Assert.Contains("One is back in its reach.", Said(sim));
    }

    /// <summary>The airlock taken from Anna by Bert, paused.</summary>
    static Simulation Captured()
    {
        var sim = Airlock(out _);
        Owned(sim, Anna, Sharing.Nobody);
        Owned(sim, Bert, Sharing.Nobody);
        Assert.True(sim.Editor.Program.Paused);
        return sim;
    }

    /// <summary>What the page on top says, as the rich text it is written in.</summary>
    static string Said(Simulation sim)
    {
        return sim.Editor.Current.View.Within().OfType<RichText>().Single().Markdown;
    }

    /// <summary>Presses the page's button saying <paramref name="text" />.</summary>
    static void Press(Simulation sim, string text)
    {
        sim.Editor.Current.View.Within().OfType<Button>().Single(button => button.Text == text).Command!.Execute();
    }

    /// <summary>
    /// A transfer that reaches the programmable block before the rest of the construct leaves the automation running
    /// throughout, judged once whole from the owners before the transfer.
    /// </summary>
    [Fact]
    public void ATransferReachingTheProgrammableBlockFirstRunsThroughout()
    {
        var sim = Airlock(out _);
        sim.Machine.Factions.Join(Anna, "MAL");
        sim.Machine.Factions.Join(Bert, "MAL");
        Owned(sim, Anna, Sharing.Faction);

        sim.Machine.Sprocket.Share = Sharing.Nobody;
        sim.Machine.Sprocket.Owner = Bert;
        sim.Tick();
        sim.Tick();

        Assert.False(sim.Unit.Lost);
        Assert.True(sim.Runner.Live);

        Owned(sim, Bert, Sharing.Nobody);

        Assert.False(sim.Unit.Lost);
        Assert.False(sim.Editor.Program.Paused);
        Assert.Equal(new[] { Bert }, sim.Editor.Program.Owners);
        Assert.True(sim.Runner.Live);
    }

    /// <summary>A capture that reaches the programmable block first leaves the automation running, and pauses it once the capture is whole.</summary>
    [Fact]
    public void ACaptureReachingTheProgrammableBlockFirstPausesOnceItIsWhole()
    {
        var sim = Airlock(out _);
        Owned(sim, Anna, Sharing.Nobody);

        sim.Machine.Sprocket.Owner = Bert;
        sim.Tick();
        sim.Tick();

        Assert.True(sim.Runner.Live);
        Assert.False(sim.Editor.Program.Paused);

        Owned(sim, Bert, Sharing.Nobody);

        Assert.True(sim.Editor.Program.Paused);
        Assert.Equal(new[] { Anna }, sim.Editor.Program.PausedFrom);
    }

    /// <summary>A member of the faction a construct is shared with may open its automation; anybody else may not.</summary>
    [Fact]
    public void AConstructSharedWithItsFactionOpensForItsMembers()
    {
        var sim = Airlock(out _);
        sim.Machine.Factions.Join(Anna, "MAL");
        sim.Machine.Factions.Join(Bert, "MAL");
        Owned(sim, Anna, Sharing.Faction);

        var hero = sim.Unit.Block.EntityId;
        Assert.True(sim.Unit.MayOpen(hero, Anna));
        Assert.True(sim.Unit.MayOpen(hero, Bert));
        Assert.False(sim.Unit.MayOpen(hero, Cora));
    }

    /// <summary>A construct shared with nobody opens for its big owners alone.</summary>
    [Fact]
    public void AConstructSharedWithNobodyOpensForItsBigOwnersAlone()
    {
        var sim = Airlock(out _);
        sim.Machine.Factions.Join(Anna, "MAL");
        sim.Machine.Factions.Join(Bert, "MAL");
        Owned(sim, Anna, Sharing.Nobody);

        var hero = sim.Unit.Block.EntityId;
        Assert.True(sim.Unit.MayOpen(hero, Anna));
        Assert.False(sim.Unit.MayOpen(hero, Bert));
    }

    /// <summary>A programmable block no big owner owns opens the automation for a big owner it is shared with, and for nobody else.</summary>
    [Fact]
    public void AProgrammableBlockNoBigOwnerOwnsOpensForABigOwnerItIsSharedWith()
    {
        var sim = Airlock(out _);
        Programmable spare = sim.Machine.Programmable(200, "Spare");
        Owned(sim, Anna, Sharing.Nobody);

        spare.Share = Sharing.All;
        spare.Owner = Bert;
        sim.Tick();
        sim.Tick();

        Assert.True(sim.Unit.MayOpen(spare.Block.EntityId, Anna));
        Assert.False(sim.Unit.MayOpen(spare.Block.EntityId, Bert));
        Assert.False(sim.Unit.MayOpen(spare.Block.EntityId, Cora));
        Assert.Same(sim.Unit, sim.Roster.On(spare.Block));
    }

    /// <summary>A programmable block no big owner owns, shared with nobody, opens the automation for its owner alone, who is no big owner.</summary>
    [Fact]
    public void AProgrammableBlockSharedWithNobodyKeepsOutTheBigOwners()
    {
        var sim = Airlock(out _);
        Programmable spare = sim.Machine.Programmable(200, "Spare");
        Owned(sim, Anna, Sharing.Nobody);

        spare.Owner = Bert;
        sim.Tick();
        sim.Tick();

        Assert.False(sim.Unit.MayOpen(spare.Block.EntityId, Anna));
        Assert.False(sim.Unit.MayOpen(spare.Block.EntityId, Bert));
    }

    /// <summary>An admin using every terminal may open the automation through any block; an admin without that setting may not.</summary>
    [Fact]
    public void AnAdminUsingEveryTerminalOpensAnyAutomation()
    {
        var sim = Airlock(out _);
        Owned(sim, Anna, Sharing.Nobody);
        var hero = sim.Unit.Block.EntityId;

        Assert.False(sim.Unit.MayOpen(hero, Cora));
        Assert.True(AsAdmin(Cora, MyAdminSettingsEnum.UseTerminals, () => sim.Unit.MayOpen(hero, Cora)));
        Assert.False(AsAdmin(Cora, MyAdminSettingsEnum.Invulnerable, () => sim.Unit.MayOpen(hero, Cora)));
    }

    /// <summary>A logic system whose button opens the inner door, switched on.</summary>
    static Simulation Airlock(out Sequence sequence)
    {
        var sim = Simulation.Fresh();
        sim.Editor.NewButtonSystem("Airlock");
        sim.Editor.PressAdd("task");
        sim.Editor.PressBlock("Airlock Inner Door");
        sim.Editor.PressChoice("open");
        sim.Editor.PressPrimary();
        sim.Editor.SwitchOn();
        sequence = sim.Editor.Program.Sequences[0];
        for (var i = 0; i < 3; i++)
            sim.Tick();

        return sim;
    }

    /// <summary>Hands the whole construct to <paramref name="player" />, shared as <paramref name="share" /> says, and lets it settle.</summary>
    static void Owned(Simulation sim, long player, Sharing share)
    {
        sim.Machine.Own(player, share);
        sim.Tick();
        sim.Tick();
    }

    /// <summary>Asks <paramref name="ask" /> with the game saying <paramref name="player" /> is an admin with <paramref name="settings" /> turned on.</summary>
    static bool AsAdmin(long player, MyAdminSettingsEnum settings, Func<bool> ask)
    {
        const ulong steamId = 76561197960287930;
        var players = MyAPIGateway.Players;
        var session = MyAPIGateway.Session;
        MyAPIGateway.Players = Pretend.To<IMyPlayerCollection>()
            .Does("TryGetSteamId", arguments => (long)arguments[0] == player ? steamId : 0UL)
            .Done();
        MyAPIGateway.Session = Pretend.To<IMySession>()
            .Answers("TryGetAdminSettings", call =>
            {
                var admin = (ulong)call.Arguments[0] == steamId;
                call.Arguments[1] = admin ? settings : MyAdminSettingsEnum.None;
                return admin;
            })
            .Done();
        try
        {
            return ask();
        }
        finally
        {
            MyAPIGateway.Players = players;
            MyAPIGateway.Session = session;
        }
    }
}
