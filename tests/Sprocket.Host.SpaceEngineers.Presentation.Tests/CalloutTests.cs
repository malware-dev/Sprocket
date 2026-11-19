using Sprocket.Host.SpaceEngineers.Features.Callouts;
using Sprocket.Host.SpaceEngineers.Framework.Game.Players;
using VRage.Game.ModAPI.Ingame.Utilities;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>The callouts that point out what a release brought: where they hang, how they are dismissed, and what the player's file keeps of them.</summary>
public class CalloutTests
{
    /// <summary>A screen shown to a player who has not been offered any callout, on the page the script ends on.</summary>
    static Screen Meeting(params string[] script)
    {
        var screen = Screen.Fresh(script);
        screen.Scene.MeetCallouts();
        return screen.Run(3);
    }

static string[] LogicSystem => new[] { "press", "+ logic system", "type", "Airlock", "enter" };

    static string[] Library => new[] { "press", "+ logic system", "type", "Airlock", "enter", "escape", "press", "Make blueprint" };

    /// <summary>A player with no release offered meets the current release's callout at the place it hangs from, here the header's Share mark.</summary>
    [Fact]
    public void ANewPlayerMeetsTheCalloutAtItsAnchor()
    {
        var screen = Meeting(LogicSystem);
        var texts = screen.Editor.Texts;

        Assert.True(screen.Shows(texts.CalloutShare), screen.Dump);
        Assert.Equal("share", screen.Editor.Callouts.Shown?.Id);
    }

    /// <summary>A screen without any place a callout hangs from has none, whatever is due.</summary>
    [Fact]
    public void NoCalloutShowsWhereItsAnchorIsNot()
    {
        var screen = Meeting();
        var texts = screen.Editor.Texts;

        Assert.NotEmpty(screen.Editor.Callouts.Due);
        Assert.Null(screen.Editor.Callouts.Shown);
        Assert.False(screen.Shows(texts.CalloutShare) || screen.Shows(texts.CalloutImport) || screen.Shows(texts.CalloutSettings), screen.Dump);
    }

    /// <summary>One press anywhere dismisses it for good: it is not hung again where the player meets its other anchors.</summary>
    [Fact]
    public void OnePressDismissesACalloutAtEveryAnchor()
    {
        var screen = Meeting(LogicSystem);
        var texts = screen.Editor.Texts;

        screen.Do("900", "700").Run(3);
        Assert.False(screen.Shows(texts.CalloutShare), screen.Dump);
        Assert.True(screen.Player().HasDismissed("share"));

        // Its other anchors: the home page's row, and the library's chosen blueprint.
        screen.Do("escape", "press", "Make blueprint").Run(3);
        Assert.False(screen.Shows(texts.CalloutShare), screen.Dump);
        Assert.NotEqual("share", screen.Editor.Callouts.Shown?.Id);
    }

    /// <summary>Escape goes to the editor beneath the bubble and never dismisses it: it hangs again at the next anchor the player meets.</summary>
    [Fact]
    public void EscapeLeavesTheCalloutDue()
    {
        var screen = Meeting(LogicSystem);
        var texts = screen.Editor.Texts;

        screen.Do("escape").Run(3);

        Assert.False(screen.Player().HasDismissed("share"));
        Assert.False(screen.Editor.Screen.CanGoBack, screen.Dump);
        Assert.Equal("share", screen.Editor.Callouts.Shown?.Id);
        Assert.True(screen.Shows(texts.CalloutShare), screen.Dump);
    }

    /// <summary>A press on the bubble itself dismisses it too.</summary>
    [Fact]
    public void APressOnTheBubbleDismissesIt()
    {
        var screen = Meeting(LogicSystem);
        var texts = screen.Editor.Texts;

        var at = screen.Text(texts.CalloutShare).At;
        screen.Do(((int)at.X + 8).ToString(), ((int)at.Y + 8).ToString()).Run(3);

        Assert.False(screen.Shows(texts.CalloutShare), screen.Dump);
        Assert.True(screen.Player().HasDismissed("share"));
    }

    /// <summary>Only one callout hangs at a time, the next once it is dismissed.</summary>
    [Fact]
    public void OnlyOneShowsAtATime()
    {
        var screen = Meeting(Library);
        var texts = screen.Editor.Texts;

        Assert.True(screen.Shows(texts.CalloutShare), screen.Dump);

        screen.Do("900", "700").Run(3);

        Assert.False(screen.Shows(texts.CalloutShare), screen.Dump);
        Assert.Equal("import", screen.Editor.Callouts.Shown?.Id);
    }

    /// <summary>The home page's own Share mark carries the callout where the player meets that first.</summary>
    [Fact]
    public void TheHomePageHangsItFromTheLogicSystemRow()
    {
        var screen = Meeting("press", "+ logic system", "type", "Airlock", "enter", "escape");

        Assert.True(screen.Shows(screen.Editor.Texts.CalloutShare), screen.Dump);
    }

    /// <summary>The settings callout hangs from the plus that adds a slot, once the installation panel is open.</summary>
    [Fact]
    public void TheSettingsCalloutHangsFromAddSlot()
    {
        var screen = Meeting(LogicSystem);
        var texts = screen.Editor.Texts;
        screen.Do("900", "700").Run(2).Do("press", "Installation panel").Run(3);

        Assert.True(screen.Shows(texts.CalloutSettings), screen.Dump);
    }

    /// <summary>What was dismissed is kept as the dismissed ids, and reads back from the global file; the release is recorded as seen only once every callout due is dismissed.</summary>
    [Fact]
    public void WhatIsDismissedIsKept()
    {
        var screen = Meeting(LogicSystem);
        Assert.Null(screen.Player().Seen);
        Assert.False(screen.Player().HasDismissed("share"));

        screen.Do("900", "700").Run(2);

        var file = new MyIni();
        screen.Player().Write(file);
        var text = file.ToString();
        Assert.DoesNotContain("seen=", text);
        Assert.Contains("dismissed=share", text);

        var read = new MyIni();
        Assert.True(read.TryParse(text));
        var back = PlayerSettings.From(read);
        Assert.Null(back.Seen);
        Assert.True(back.HasDismissed("share"));
        Assert.False(back.HasDismissed("import"));
    }

    /// <summary>A callout not met in one session is due in the next, and the release is recorded as seen only once every callout of it is dismissed.</summary>
    [Fact]
    public void ACalloutNotMetIsStillDueInTheNextSession()
    {
        var screen = Meeting(LogicSystem);
        var texts = screen.Editor.Texts;

        // The first session meets the share callout only.
        screen.Do("900", "700").Run(2);
        Assert.Null(screen.Player().Seen);

        // The next one still has the import and settings callouts due, and shows import at the library.
        screen.Do("escape").Run(2);
        screen.Scene.Reopen();
        screen.Run(3);
        Assert.Equal(new[] { "import", "settings" }, screen.Editor.Callouts.Due.Select(callout => callout.Id));

        screen.Do("press", "Library").Run(3);
        Assert.Equal("import", screen.Editor.Callouts.Shown?.Id);
        screen.Do("900", "700").Run(2);
        Assert.True(screen.Player().HasDismissed("import"));
        Assert.Null(screen.Player().Seen);

        // The last one is met in the installation panel, and the release is then seen.
        screen.Do("escape").Run(2);
        screen.Scene.Reopen();
        screen.Run(3);
        Assert.Equal(new[] { "settings" }, screen.Editor.Callouts.Due.Select(callout => callout.Id));

        screen.Do("press", "Airlock").Run(3).Do("press", "Installation panel").Run(3);
        Assert.True(screen.Shows(texts.CalloutSettings), screen.Dump);
        screen.Do("900", "700").Run(2);
        Assert.Equal(Release.Number, screen.Player().Seen);

        screen.Scene.Reopen();
        screen.Run(3);
        Assert.Empty(screen.Editor.Callouts.Due);
    }

    /// <summary>A player already offered this release has no callout of it, however often the editor opens.</summary>
    [Fact]
    public void ACalloutOfAReleaseAlreadyOfferedDoesNotShow()
    {
        var screen = Screen.Fresh(LogicSystem).Run(3);
        var texts = screen.Editor.Texts;

        Assert.Equal(Release.Number, screen.Player().Seen);
        Assert.Empty(screen.Editor.Callouts.Due);
        Assert.False(screen.Shows(texts.CalloutShare), screen.Dump);

        screen.Scene.Reopen();
        screen.Run(3);
        Assert.False(screen.Shows(texts.CalloutShare), screen.Dump);
    }

    /// <summary>A callout is due where its release is newer than the one last offered, or, with none offered, is the current one; never one dismissed.</summary>
    [Fact]
    public void WhichCalloutsFallDue()
    {
        var all = new[]
        {
            new Callout("old", Release.Number - 1, _ => "", CalloutPlace.HeaderShare),
            new Callout("now", Release.Number, _ => "", CalloutPlace.HeaderShare),
            new Callout("next", Release.Number + 1, _ => "", CalloutPlace.HeaderShare)
        };

        var fresh = Begun(all, new PlayerSettings(true));
        Assert.Equal(new[] { "now" }, fresh.Due.Select(callout => callout.Id));
        Assert.Null(fresh.Choices.Seen);

        Assert.Equal(new[] { "now", "next" }, Begun(all, new PlayerSettings(true, Release.Number - 1)).Due.Select(callout => callout.Id));
        Assert.Equal(new[] { "next" }, Begun(all, new PlayerSettings(true, Release.Number)).Due.Select(callout => callout.Id));
        Assert.Equal(new[] { "next" }, Begun(all, new PlayerSettings(true, Release.Number - 1, new[] { "now" })).Due.Select(callout => callout.Id));
    }

    /// <summary>A book of <paramref name="all" /> for a player whose choices are <paramref name="choices" />, begun as an editor opens.</summary>
    static CalloutBook Begun(IList<Callout> all, PlayerSettings choices)
    {
        var book = new CalloutBook(all);
        var held = new[] { choices };
        book.Keeping(() => held[0], kept => held[0] = kept);
        book.Begin();
        return book;
    }
}

static class CalloutPresses
{
    /// <summary>What the player has chosen as the screen's host keeps it.</summary>
    public static PlayerSettings Player(this Screen screen) => screen.Scene.Player;
}
