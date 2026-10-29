using Lumel;
using Lumel.Drawing;
using Lumel.Images;
using Lumel.Layout;
using Lumel.Screens;
using Lumel.Sim;
using Sandbox.ModAPI;
using Sprocket.Sim.Content;
using Sprocket.Sim.Simulation;
using VRageMath;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A display places each child in its room by the child's own alignment, inside its padding, in the area of the screen it asks for.</summary>
public class DisplayTests
{
    /// <summary>A child aligned to the center sits in the middle of the display at its own size.</summary>
    [Fact]
    public void AChildAlignedToTheCenterSitsInTheMiddleAtItsOwnSize()
    {
        var page = new VStack { Align = Align.Center, Placement = FixedSize.Size(200f, 100f) };
        var display = new Display { Children = { page } };

        _ = new Easel(display.Root, 1000f, 600f);

        Assert.Equal(new RectangleF(400f, 250f, 200f, 100f), page.Bounds);
    }

    /// <summary>A child that says nothing of where it goes fills the display.</summary>
    [Fact]
    public void AChildThatSaysNothingFillsTheDisplay()
    {
        var page = new VStack();
        var display = new Display(page);

        _ = new Easel(display.Root, 1000f, 600f);

        Assert.Equal(new RectangleF(0f, 0f, 1000f, 600f), page.Bounds);
    }

    /// <summary>The display's padding keeps what it shows from its edges.</summary>
    [Fact]
    public void PaddingKeepsWhatIsShownFromTheEdges()
    {
        var page = new VStack();
        var display = new Display { Children = { page } };
        display.Root.Base.Padding = new Thickness(24f);

        _ = new Easel(display.Root, 1000f, 600f);

        Assert.Equal(new RectangleF(24f, 24f, 952f, 552f), page.Bounds);
    }

    /// <summary>A workspace on the screen is laid out the size of the game's own terminal screen, centered in the safe area.</summary>
    [Fact]
    public void AWorkspaceIsTheSizeOfTheGamesOwnScreen()
    {
        var graphics = Screen(new RectangleF(240f, 0f, 1440f, 1080f), new Vector2(1920f, 1080f));

        var room = graphics.BoundsOf(new ScreenSpaceWorkspace("Test").Area);

        Assert.Equal(1440f, room.Width, 3);
        Assert.Equal(1080f * ScreenSpaceGraphics.TerminalHeight, room.Height, 3);
        Assert.Equal(0f, room.X, 3);
        Assert.Equal((1080f - room.Height) * 0.5f, room.Y, 3);
    }

    /// <summary>A workspace asking for the whole window is laid out across it, edge to edge, past the safe area.</summary>
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
