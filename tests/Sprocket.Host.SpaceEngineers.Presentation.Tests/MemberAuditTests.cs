using Sprocket.Host.SpaceEngineers.Features.Codex;
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
