using VRage.Input;
using Lumel.Input;
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
        var easel = new Easel(Notice().Windows.Top!, Width, Height);

        var said = string.Join(" ", easel.Written.Select(text => text.Text));
        Assert.Contains("Hailstone", said);
        Assert.Contains("OK", said);
        Assert.DoesNotContain("{construct}", said);
    }

    /// <summary>Pressing OK, or Enter, which does as its OK does, closes the notice.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PressingOkOrEnterClosesTheNotice(bool byEnter)
    {
        var stage = Notice();
        if (byEnter)
            Assert.True(stage.Key(MyKeys.Enter, false, false));
        else
        {
            var easel = new Easel(stage.Windows.Top!, Width, Height);
            var ok = easel.Written.First(text => text.Text == "OK");
            var at = new Vector2(ok.Position.X, ok.Position.Y) / easel.Pixels + new Vector2(4f, 4f);

            Assert.Equal(1, stage.Windows.Count);
            stage.PointerDown(at, PointerButton.Left, 1, false, false);
            stage.PointerUp(at, PointerButton.Left);
        }

        Assert.Equal(0, stage.Windows.Count);
    }

    static Workspace Notice()
    {
        var arguments = new Arguments();
        arguments.Set("construct", "Hailstone");
        return TerminalNotice.Hosted("Only the owners of {construct} can open its automation.", arguments, "OK");
    }
}
