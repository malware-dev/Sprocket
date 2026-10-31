using Lumel.Drawing;
using Lumel.Images;
using Lumel.Sim;
using Sandbox.ModAPI;
using Sprocket.Sim.Content;
using Sprocket.Sim.Simulation;
using VRageMath;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>The areas of the screen a window is laid out in: the game's terminal, or the whole window.</summary>
public class ScreenAreaTests
{
    /// <summary>The terminal's area is the size of the game's own terminal screen, centered in the safe area.</summary>
    [Fact]
    public void TheTerminalIsTheSizeOfTheGamesOwnScreen()
    {
        var graphics = Screen(new RectangleF(240f, 0f, 1440f, 1080f), new Vector2(1920f, 1080f));

        var room = graphics.BoundsOf(ScreenArea.Terminal);

        Assert.Equal(1440f, room.Width, 3);
        Assert.Equal(1080f * ScreenSpaceGraphics.TerminalHeight, room.Height, 3);
        Assert.Equal(0f, room.X, 3);
        Assert.Equal((1080f - room.Height) * 0.5f, room.Y, 3);
    }

    /// <summary>The whole window's area reaches edge to edge, past the safe area.</summary>
    [Fact]
    public void TheWholeWindowReachesPastTheSafeArea()
    {
        var graphics = Screen(new RectangleF(240f, 0f, 1440f, 1080f), new Vector2(1920f, 1080f));

        var room = graphics.BoundsOf(ScreenArea.Window);

        Assert.Equal(new RectangleF(-240f, 0f, 1920f, 1080f), room);
    }

    /// <summary>Graphics on a screen with <paramref name="safe" /> inside a window <paramref name="viewport" /> big, begun for a frame.</summary>
    static ScreenSpaceGraphics Screen(RectangleF safe, Vector2 viewport)
    {
        var device = new ScreenSpaceDevice(SprocketContent.Instance);
        device.Fonts = new FallbackFonts(device, "Segoe UI");
        ScreenSession.Install(device);
        device.SafeArea = safe;
        device.ViewportSize = viewport;

        var graphics = new ScreenSpaceGraphics(device, MyScreenSpaceLayer.AboveHud, new MaterialImages(device.TextureSize), () => 0);
        graphics.Begin();
        return graphics;
    }
}
