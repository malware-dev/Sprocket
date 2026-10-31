using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A logic system's page scrolls the section under its chosen tab, under a heading and a tab row that stay where they are.</summary>
public class TabScrollTests
{
    [Fact]
    public void OnlyTheTabsSectionScrolls()
    {
        var screen = Screen.Fresh();
        var editor = screen.Editor;
        editor.NewButtonSystem("Airlock");
        editor.GoToRoot();
        screen.Run(2);

        var system = editor.Program.Systems[0];
        for (var i = 0; i < 30; i++)
            editor.Program.AddSequence(system).Name = "Long list " + i;

        system.Enabled = false;
        editor.Program.Edited();
        editor.OpenBlueprintSystem(0);
        editor.PressTab(editor.Texts.SystemSequences);
        screen.Run(2);

        var tab = screen.Text(editor.Texts.SystemSequences).At.Y;
        var heading = screen.Text("Selected installation").At.Y;
        var row = screen.Text("Long list 3").At.Y;

        screen.Do("scroll", "900", "700", "300");

        Assert.Equal(tab, screen.Text(editor.Texts.SystemSequences).At.Y);
        Assert.Equal(heading, screen.Text("Selected installation").At.Y);
        Assert.True(screen.Text("Long list 3").At.Y < row, "the tab's section did not scroll");
    }
}
