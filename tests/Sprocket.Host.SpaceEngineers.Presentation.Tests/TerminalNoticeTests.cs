using Lumel.Screens;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using VRageMath;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A notice over the terminal says its message, with its slots filled, and closes on its one button.</summary>
public class TerminalNoticeTests
{
    const float Width = 1920f;
    const float Height = 1080f;

    /// <summary>The notice names the construct it is about and offers OK.</summary>
    [Fact]
    public void ANoticeSaysItsMessageWithTheConstructNamed()
    {
        var easel = new Easel(Notice().Root, Width, Height);

        var said = string.Join(" ", easel.Written.Select(text => text.Text));
        Assert.Contains("Hailstone", said);
        Assert.Contains("OK", said);
        Assert.DoesNotContain("{construct}", said);
    }

    /// <summary>Pressing OK closes the notice.</summary>
    [Fact]
    public void PressingOkClosesTheNotice()
    {
        var stage = Notice();
        var easel = new Easel(stage.Root, Width, Height);
        var ok = easel.Written.First(text => text.Text == "OK");
        var at = new Vector2(ok.Position.X, ok.Position.Y) / easel.Pixels + new Vector2(4f, 4f);

        Assert.False(stage.TakeClose());
        stage.PointerDown(at, 1, false, false);
        stage.PointerUp(at);

        Assert.True(stage.TakeClose());
    }

    /// <summary>Enter closes the notice, as its OK does.</summary>
    [Fact]
    public void EnterClosesTheNotice()
    {
        var stage = Notice();

        Assert.True(stage.IsQuestioning);
        Assert.True(stage.TakeDefault());
        Assert.True(stage.TakeClose());
    }

    static Workspace Notice()
    {
        var arguments = new Arguments();
        arguments.Set("construct", "Hailstone");
        return TerminalNotice.Hosted("Only the owners of {construct} can open its automation.", arguments, "OK");
    }
}
