using System;
using System.IO;
using System.Text;
using Lumel.Drawing;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.LcdApps.StatusPanel;
using Sprocket.Sim.Content;
using Sprocket.Sim.Machine;
using Sprocket.Sim.Simulation;
using VRage.Game.GUI.TextPanel;
using VRageMath;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>The automation panel lays out every scene on every screen of the reference set, page after page, inside the screen and at a readable size.</summary>
public class StatusPictureTests
{
    /// <summary>Every page of every scene draws on every screen, and draws again as the pages take turns.</summary>
    [Fact]
    public void EveryPageDrawsAgainAndAgain()
    {
        foreach (var scene in StatusScenes.Names)
        {
            var status = StatusScenes.Make(scene, out var texts);
            foreach (ScreenShape shape in ScreenShape.Reference)
            {
                var face = Face(shape);
                var picture = new StatusPicture();
                for (var turn = 0; turn <= picture.Pages * 2; turn++)
                {
                    Draw(picture, face, status, texts, turn);
                    Assert.NotEmpty(face.Sprites);
                }
            }
        }
    }

    /// <summary>Nothing is drawn past the screen's edge, and no line of text is shorter than the smallest size, on any page; a mark may be smaller.</summary>
    [Fact]
    public void EverythingIsInsideTheScreenAndReadable()
    {
        foreach (var scene in StatusScenes.Names)
        {
            var status = StatusScenes.Make(scene, out var texts);
            foreach (ScreenShape shape in ScreenShape.Reference)
            {
                var face = Face(shape);
                var picture = new StatusPicture();
                for (var turn = 0; turn < picture.Pages; turn++)
                {
                    Draw(picture, face, status, texts, turn);
                    Inside(face, scene + " on " + shape.Name + ", page " + (turn + 1));
                }
            }
        }
    }

    /// <summary>
    /// In every set of app colors, text stands apart from the background by the ratio text is read at and a mark by the ratio
    /// a mark is seen at, or as far as the player's own text color does where theirs falls short.
    /// </summary>
    [Fact]
    public void EveryColorCanBeSeenOnEveryBackground()
    {
        foreach (var colors in PanelColors.All)
        {
            var ink = colors.Value[0];
            var ground = colors.Value[1];
            foreach (var scene in new[] { "trouble", "all-normal" })
            {
                var status = StatusScenes.Make(scene, out var texts);
                foreach (ScreenShape shape in ScreenShape.Reference)
                {
                    var face = Face(shape);
                    face.Foreground = ink;
                    face.Background = ground;
                    Draw(new StatusPicture(), face, status, texts, 0);

                    foreach (MySprite sprite in face.Sprites)
                    {
                        if (sprite.Type != SpriteType.TEXT || string.IsNullOrEmpty(sprite.Data))
                            continue;

                        var wanted = sprite.FontId == Symbols.Font ? Contrast.Mark : Contrast.Text;
                        var reached = Math.Min(wanted, Contrast.Ratio(ink, ground));
                        Assert.True(Contrast.Ratio(sprite.Color!.Value, ground) >= reached - 0.01f,
                            colors.Key + ", " + scene + " on " + shape.Name + ": \"" + sprite.Data + "\" stands "
                            + Contrast.Ratio(sprite.Color.Value, ground) + " apart, short of " + reached);
                    }
                }
            }
        }
    }

    static void Inside(Face face, string where)
    {
        Vector2 size = face.Shape.SurfaceSize;
        Vector2 corner = (face.Shape.TextureSize - size) / 2f;
        var surface = face.Surface;

        foreach (MySprite sprite in face.Sprites)
        {
            var at = sprite.Position!.Value - corner;
            if (sprite.Type == SpriteType.TEXTURE)
            {
                var half = sprite.Size!.Value / 2f;
                Assert.True(at.X - half.X >= -0.5f && at.Y - half.Y >= -0.5f && at.X + half.X <= size.X + 0.5f && at.Y + half.Y <= size.Y + 0.5f,
                    where + ": " + sprite.Data + " runs past the screen");
                continue;
            }

            if (sprite.Type != SpriteType.TEXT || string.IsNullOrEmpty(sprite.Data))
                continue;

            var drawn = surface.MeasureStringInPixels(new StringBuilder(sprite.Data), sprite.FontId, sprite.RotationOrScale);
            var line = surface.MeasureStringInPixels(new StringBuilder("A"), sprite.FontId, sprite.RotationOrScale).Y;
            var left = sprite.Alignment == TextAlignment.RIGHT ? at.X - drawn.X : at.X;
            Assert.True(left >= -0.5f && left + drawn.X <= size.X + 0.5f && at.Y + line * TextSizes.Baseline <= size.Y + 0.5f,
                where + ": \"" + sprite.Data + "\" runs past the screen");
            if (sprite.FontId != Symbols.Font)
                Assert.True(line >= StatusLook.Smallest - 0.5f, where + ": \"" + sprite.Data + "\" is " + line + " pixels tall");
        }
    }

    static void Draw(StatusPicture picture, Face face, ConstructStatus status, Localization.Texts texts, int turn)
    {
        face.Sprites.Clear();
        var graphics = new SpriteGraphics(face.Surface, face.Sprites, null);
        graphics.Begin();
        picture.Draw(graphics, status, texts, face.Foreground, face.Background, face.Padding, face.Shape.Metres, turn);
    }

    /// <summary>A screen of <paramref name="shape" /> measuring with the harness's stand-in widths and the mod's own faces.</summary>
    static Face Face(ScreenShape shape)
    {
        var device = new ScreenSpaceDevice();
        var fonts = new FontSet(new FallbackFonts(device, "Segoe UI"));
        foreach (var font in ModFonts.All)
            fonts = fonts.And(font, SeFontMetrics.LoadFrom(device, Path.Combine(ModFonts.Folder, font + ".xml"), "the mod's own content", true));

        device.Fonts = fonts;
        return new Face(shape.Name) { Shape = shape, Measuring = device };
    }
}
