using Sprocket.Host.SpaceEngineers.Features.Codex;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>The codex's account of the members in the organized lists, on the blocks the harness can build.</summary>
public class MemberAuditTests
{
    /// <summary>Every kind of block the harness's airlock holds has a text for every row, in each list it stands in.</summary>
    [Fact]
    public void EveryRowHasAText()
    {
        var screen = Screen.Fresh();
        screen.Editor.NewSystem("Airlock");

        var missing = new MemberAudit(screen.Editor).Undescribed();

        Assert.True(missing.Count == 0, string.Join("\n", missing));
    }

    /// <summary>Pressing the button says what it found, on the screen it was pressed on.</summary>
    [Fact]
    public void TheButtonSaysWhatItFound()
    {
        var screen = Screen.Fresh();
        screen.Editor.DeveloperMode = true;
        screen.Editor.NewSystem("Airlock");
        screen.Run(2);

        screen.Do("press", "CHECK MEMBER TEXTS").Run(2);

        Assert.Contains(screen.Legible, words => words.StartsWith("Every member"));
    }

    /// <summary>A text panel's rows and show on HUD find their texts.</summary>
    [Fact]
    public void ScreensAndTheHudHaveTheirTexts()
    {
        var made = Rig.Demo();
        var panel = made.TextPanel(900, "Sign");
        var screen = Screen.On(made);
        screen.Editor.NewSystem("Airlock");

        var missing = new MemberAudit(screen.Editor).Undescribed();
        var kind = screen.Editor.World.FindEntity(panel.Id).Kind;

        Assert.True(missing.Count == 0, string.Join("\n", missing));
        Assert.NotEqual("", screen.Editor.Codex.Member(kind, "show on HUD", false));
        Assert.NotEqual("", screen.Editor.Codex.Member(kind, "toggle show on HUD", false));
    }

    /// <summary>The audit leaves the list as it found it.</summary>
    [Fact]
    public void TheAuditLeavesTheTerminalListOn()
    {
        var screen = Screen.Fresh();
        screen.Editor.ShowsTerminal = true;

        new MemberAudit(screen.Editor).Undescribed();

        Assert.True(screen.Editor.ShowsTerminal);
    }
}
