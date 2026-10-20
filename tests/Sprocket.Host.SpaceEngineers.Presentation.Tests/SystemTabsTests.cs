using Lumel.Controls;
using Lumel.Images;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A logic system's screen in four tabs: triggers, sequences, state and the installation panel.</summary>
public class SystemTabsTests
{
    /// <summary>A logic system with a sequence nothing starts, which is a warning, back on the logic system's screen.</summary>
    static Screen WithAnUnstartedSequence()
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
        screen.Editor.BackKeeping();
        return screen.Run(120);
    }

    static Tab[] Tabs(Screen screen) => screen.Editor.Current!.View.Within().OfType<Tab>().ToArray();

    /// <summary>The four tabs stand in order, each named.</summary>
    [Fact]
    public void TheScreenHasFourTabs()
    {
        var screen = WithAnUnstartedSequence();
        var texts = screen.Editor.Texts;

        Assert.Equal(new[] { texts.SystemTriggers, texts.SystemSequences, texts.SystemState, texts.SystemInstallationPanel },
            Tabs(screen).Select(tab => tab.Header).ToArray());
    }

    /// <summary>The screen opens on the tab it was left on, for each logic system apart.</summary>
    [Fact]
    public void TheTabLastOpenIsOpenedAgain()
    {
        var screen = WithAnUnstartedSequence();
        screen.Editor.PressTab(screen.Editor.Texts.SystemState);
        screen.Run(2);

        screen.Editor.GoToRoot();
        screen.Run(2);
        screen.Editor.NewSystem("Hangar");
        screen.Run(2);
        Assert.True(Tabs(screen)[0].IsCurrent, "a logic system never opened did not open on its first tab");

        screen.Editor.GoToRoot();
        screen.Run(2);
        screen.Editor.OpenBlueprintSystem(0);
        screen.Run(2);
        Assert.True(Tabs(screen)[2].IsCurrent, "the logic system did not open on the tab it was left on");
    }

    /// <summary>A sequence that wants looking at marks the Sequences tab in its color, wherever the screen is, and the Triggers tab stays plain.</summary>
    [Fact]
    public void AWarningMarksItsTab()
    {
        var screen = WithAnUnstartedSequence();
        var tabs = Tabs(screen);

        var sequences = Assert.IsType<Glyph>(tabs[1].Marker);
        Assert.True(sequences.IsVisible, "the Sequences tab carries no mark for a sequence nothing starts");
        Assert.Equal(Palette.Notice, sequences.Tint);
        Assert.False(tabs[0].Marker!.IsVisible);

        screen.Editor.PressTab(screen.Editor.Texts.SystemSequences);
        screen.Run(2);
        Assert.True(sequences.IsVisible, "the Sequences tab lost its mark while it was the tab shown");

        // Opened again straight onto the Sequences tab, the screen marks it as it is built.
        screen.Editor.GoToRoot();
        screen.Run(2);
        screen.Editor.OpenBlueprintSystem(0);
        screen.Run(130);
        var again = Tabs(screen);
        Assert.True(again[1].IsCurrent);
        Assert.True(again[1].Marker!.IsVisible, "the Sequences tab opened onto carries no mark");
        Assert.True(again[1].Marker!.Bounds.Width > 0f, "the Sequences tab's mark was given no room");
    }
}
