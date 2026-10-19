using Sprocket.Host.SpaceEngineers.LcdApps.StatusPanel;
using Sprocket.Sim.Content;
using Sprocket.Sim.Machine;
using VRageMath;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A screen in the harness measures what the same screen measures in the game, so a panel is laid out against its real size.</summary>
public class ScreenShapeTests
{
    /// <summary>The large programmable block's main screen shows 512 by 320 of a square texture.</summary>
    [Fact]
    public void TheProgrammableBlocksScreenIsSixteenByTen()
    {
        var shape = new ScreenShape("programmable-block", 512, 16, 10, 2.5f);

        Assert.Equal(new Vector2I(512, 512), shape.TextureSize);
        Assert.Equal(new Vector2(512f, 320f), shape.SurfaceSize);
    }

    /// <summary>A tall face keeps the texture's height and is cut across.</summary>
    [Fact]
    public void TheAutomatonsScreenIsCutAcross()
    {
        var shape = new ScreenShape("programmable-block-automatons", 512, 4, 5, 2.5f);

        Assert.Equal(new Vector2I(512, 512), shape.TextureSize);
        Assert.Equal(409.6f, shape.SurfaceSize.X, 3);
        Assert.Equal(512f, shape.SurfaceSize.Y);
    }

    /// <summary>A face seven times wider than high gets a texture four times wider than high, and shows a strip of it.</summary>
    [Fact]
    public void ACornerScreenIsAStripOfAWideTexture()
    {
        var shape = new ScreenShape("corner-lcd", 128, 7, 1, 2.5f);

        Assert.Equal(new Vector2I(512, 128), shape.TextureSize);
        Assert.Equal(512f, shape.SurfaceSize.X);
        Assert.Equal(73.143f, shape.SurfaceSize.Y, 3);
    }

    /// <summary>An app's frame lands on the screen it was drawn on, sprite by sprite.</summary>
    [Fact]
    public void AnAppsFrameIsKeptByItsScreen()
    {
        var face = new Face("Large Display") { Shape = ScreenShape.Reference[0] };

        new StatusApp((Sandbox.ModAPI.IMyTextSurface)face.Surface, null, face.Shape.TextureSize).Run();

        Assert.NotEmpty(face.Sprites);
    }
}
