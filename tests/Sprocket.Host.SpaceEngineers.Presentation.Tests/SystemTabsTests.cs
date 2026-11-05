using Lumel.Controls;
using Lumel.Images;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A logic system's screen in four tabs: triggers, sequences, state and the installation panel.</summary>
public class SystemTabsTests
{
    /// <summary>A logic system with a sequence nothing starts, which is a warning, back on the logic system's screen.</summary>
    static Screen WithAWarnedSequence()
    {
        var screen = Screen.Fresh();
        screen.Editor.NewSystem("Airlock");
        screen.Run(2);

        screen.Editor.PressStart("sequence");
        screen.Editor.PressAdd("task");
        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Editor.PressChoice("close");
        screen.Editor.PressPrimary();
        screen.Run(2);
        screen.Editor.Program.Sequences[screen.Editor.Program.Sequences.Count - 1].Steps.Insert(0, new Sequences.Await());
        screen.Editor.Program.Edited();
        screen.Editor.BackKeeping();
        return screen.Run(120);
    }

    static Tab[] Tabs(Screen screen) => screen.Editor.Current!.View.Within().OfType<Tab>().ToArray();

    /// <summary>The four tabs stand in order, each named.</summary>
    [Fact]
    public void TheScreenHasFourTabs()
    {
        var screen = WithAWarnedSequence();
        var texts = screen.Editor.Texts;

        Assert.Equal(new[] { texts.SystemTriggers, texts.SystemSequences, texts.SystemState, texts.SystemInstallationPanel },
            Tabs(screen).Select(tab => tab.Header).ToArray());
    }

    /// <summary>Each tab explains the term it is named for where the pointer rests on it.</summary>
    [Fact]
    public void EachTabExplainsItsTerm()
    {
        var screen = WithAWarnedSequence();
        var codex = screen.Editor.Texts.Codex;

        Assert.Equal(new[] { codex.TermTrigger, codex.TermSequence, codex.TermLogicSystemStates, codex.TermInstallationPanel },
            Tabs(screen).Select(tab => tab.Tip?.Text).ToArray());
    }

    /// <summary>A tab's mark says where to look for what it marks.</summary>
    [Fact]
    public void ATabsMarkSaysWhereToLook()
    {
        var screen = WithAWarnedSequence();

        var sequences = Assert.IsType<Glyph>(Tabs(screen)[1].Badge);
        Assert.True(sequences.IsVisible);
        Assert.Equal(screen.Editor.Texts.Codex.SystemTabFlagged, sequences.Tip?.Text);
    }

    /// <summary>A flagged tab lies on its level's wash and stripes, an unflagged one stays plain, and the screen stands at the graver of them.</summary>
    [Fact]
    public void AFlaggedTabLiesOnStripesAndTheScreenStandsByIt()
    {
        var screen = WithAWarnedSequence();
        var tabs = Tabs(screen);
        var mark = Assert.IsType<Glyph>(tabs[1].Badge);
        var warned = mark.Tint == Palette.Notice;

        Assert.True(tabs[1].Is(warned ? LookStates.Warned : LookStates.Broken));
        var current = tabs[1].IsSelected;
        Assert.Same(warned ? current ? Styles.NoticeStripesHot : Styles.NoticeStripes : current ? Styles.ProblemStripesHot : Styles.ProblemStripes, tabs[1].Pattern);
        Assert.Same(Styles.Tab, tabs[0].Style);
        Assert.False(tabs[0].Is(LookStates.Warned) || tabs[0].Is(LookStates.Broken));
        Assert.Equal(warned ? Features.Shell.NoticeLevel.Warning : Features.Shell.NoticeLevel.Problem, screen.Editor.Screen.Standing);
    }

    /// <summary>The screen opens on the tab it was left on, for each logic system apart.</summary>
    [Fact]
    public void TheTabLastOpenIsOpenedAgain()
    {
        var screen = WithAWarnedSequence();
        screen.Editor.PressTab(screen.Editor.Texts.SystemState);
        screen.Run(2);

        screen.Editor.GoToRoot();
        screen.Run(2);
        screen.Editor.NewSystem("Hangar");
        screen.Run(2);
        Assert.True(Tabs(screen)[0].IsSelected, "a logic system never opened did not open on its first tab");

        screen.Editor.GoToRoot();
        screen.Run(2);
        screen.Editor.OpenBlueprintSystem(0);
        screen.Run(2);
        Assert.True(Tabs(screen)[2].IsSelected, "the logic system did not open on the tab it was left on");
    }

    /// <summary>A sequence that wants looking at marks the Sequences tab in its color, wherever the screen is, and the Triggers tab stays plain.</summary>
    [Fact]
    public void AWarningMarksItsTab()
    {
        var screen = WithAWarnedSequence();
        var tabs = Tabs(screen);

        var sequences = Assert.IsType<Glyph>(tabs[1].Badge);
        Assert.True(sequences.IsVisible, "the Sequences tab carries no mark for a sequence with a warning");
        Assert.Equal(Palette.Notice, sequences.Tint);
        Assert.False(tabs[0].Badge!.IsVisible);

        screen.Editor.PressTab(screen.Editor.Texts.SystemSequences);
        screen.Run(2);
        Assert.True(sequences.IsVisible, "the Sequences tab lost its mark while it was the tab shown");

        // Opened again straight onto the Sequences tab, the screen marks it as it is built.
        screen.Editor.GoToRoot();
        screen.Run(2);
        screen.Editor.OpenBlueprintSystem(0);
        screen.Run(130);
        var again = Tabs(screen);
        Assert.True(again[1].IsSelected);
        Assert.True(again[1].Badge!.IsVisible, "the Sequences tab opened onto carries no mark");
        Assert.True(again[1].Badge!.Bounds.Width > 0f, "the Sequences tab's mark was given no room");
    }
}
