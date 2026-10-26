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
using Lumel.Sim;

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

    /// <summary>Halfway through a turn both pages are drawn, the one before moved left and the next right, the top line where it was.</summary>
    [Fact]
    public void HalfwayThroughATurnBothPagesAreDrawnSideBySide()
    {
        var status = StatusScenes.Make("trouble", out var texts);
        var face = Face(ScreenShape.Reference.First(shape => shape.Name == "programmable-block-automatons"));
        var picture = new StatusPicture();
        Draw(picture, face, status, texts, 0);
        Assert.True(picture.Pages > 1, "the scene needs a second page");
        var first = Words(face);
        Draw(picture, face, status, texts, 1);
        var second = Words(face);

        picture.Look.Slid = 0.5f;
        Draw(picture, face, status, texts, 1, 0);
        var sliding = Words(face);
        var width = face.Surface.SurfaceSize.X;

        Assert.Equal(first[texts.PanelHeading], sliding[texts.PanelHeading]);
        foreach (var word in first.Keys.Except(second.Keys))
            Assert.True(sliding[word].X < first[word].X - width / 4f, "\"" + word + "\" from the page before is not moved left");
        foreach (var word in second.Keys.Except(first.Keys))
            Assert.True(sliding[word].X > second[word].X + width / 4f, "\"" + word + "\" from the next page is not moved right");
    }

    /// <summary>A sliding page drawn again is drawn as a fresh layout of it would be.</summary>
    [Fact]
    public void ASlidingPageDrawnAgainIsDrawnAsLaidOut()
    {
        var status = StatusScenes.Make("trouble", out var texts);
        var face = Face(ScreenShape.Reference.First(shape => shape.Name == "programmable-block-automatons"));
        var picture = new StatusPicture();
        picture.Look.Slid = 0.3f;
        Draw(picture, face, status, texts, 1, 0);
        picture.Look.Slid = 0.7f;
        Draw(picture, face, status, texts, 1, 0);
        var laid = face.Sprites.ToList();

        picture.Look.Slid = 0.3f;
        Draw(picture, face, status, texts, 1, 0);
        face.Sprites.Clear();
        var graphics = new SpriteGraphics(face.Surface, face.Sprites, null);
        graphics.Begin();
        picture.Look.Slid = 0.7f;
        picture.Redraw(graphics);

        Assert.Equal(laid, face.Sprites);
    }

    /// <summary>Where each text on the screen starts, by what it says.</summary>
    static Dictionary<string, Vector2> Words(Face face)
    {
        var words = new Dictionary<string, Vector2>();
        foreach (var sprite in face.Sprites)
        {
            if (sprite.Type == SpriteType.TEXT && sprite.Data != null && !words.ContainsKey(sprite.Data))
                words.Add(sprite.Data, sprite.Position ?? Vector2.Zero);
        }

        return words;
    }

    /// <summary>On a construct with no automation, a screen with room says so and what to do about it, and names no construct.</summary>
    [Fact]
    public void NoAutomationIsSaidWithWhatToDo()
    {
        var status = StatusScenes.Make("no-automation", out var texts);
        var face = Face(ScreenShape.Reference.First(shape => shape.Name == "lcd-panel"));

        Draw(new StatusPicture(), face, status, texts, 0);
        var drawn = Drawn(face);

        Assert.Contains(texts.PanelNothing, drawn);
        Assert.Contains(texts.PanelNothingFix, drawn);
        Assert.DoesNotContain(StatusScenes.Construct, drawn);
    }

    /// <summary>On a construct with no automation, a corner screen says so alone.</summary>
    [Fact]
    public void NoAutomationOnACornerScreenIsSaidAlone()
    {
        var status = StatusScenes.Make("no-automation", out var texts);
        var face = Face(ScreenShape.Reference.First(shape => shape.Name == "corner-lcd"));

        Draw(new StatusPicture(), face, status, texts, 0);
        var drawn = Drawn(face);

        Assert.Contains(texts.PanelNothing, drawn);
        Assert.DoesNotContain(texts.PanelNothingFix.Split(' ')[0], drawn);
    }

    /// <summary>Every line of text drawn on <paramref name="face" />, joined by spaces.</summary>
    static string Drawn(Face face)
    {
        return string.Join(" ", face.Sprites.Where(sprite => sprite.Type == SpriteType.TEXT && sprite.Data != null).Select(sprite => sprite.Data));
    }

    /// <summary>A newly arrived installation's mark is lit toward the text color as it pulses.</summary>
    [Fact]
    public void AFreshMarkIsLitAsItPulses()
    {
        var status = StatusScenes.Make("trouble", out var texts);
        var face = Face(ScreenShape.Reference[0]);
        var picture = new StatusPicture();
        picture.Look.Fresh.Add(status.Items[0]);
        picture.Look.Pulse = 1f;

        Draw(picture, face, status, texts, 0);

        Assert.Contains(face.Sprites, sprite => sprite.Data == Symbols.Close && sprite.Color == face.Foreground);
    }

    static void Draw(StatusPicture picture, Face face, ConstructStatus status, Localization.Texts texts, int turn)
    {
        Draw(picture, face, status, texts, turn, turn);
    }

    static void Draw(StatusPicture picture, Face face, ConstructStatus status, Localization.Texts texts, int turn, int from)
    {
        face.Sprites.Clear();
        var graphics = new SpriteGraphics(face.Surface, face.Sprites, null);
        graphics.Begin();
        picture.Draw(graphics, status, texts, face.Foreground, face.Background, face.Padding, face.Shape.Metres, turn, from);
    }

    /// <summary>A screen of <paramref name="shape" /> measuring with the harness's stand-in widths and the mod's own faces.</summary>
    static Face Face(ScreenShape shape)
    {
        var device = new ScreenSpaceDevice(SprocketContent.Instance);
        var fonts = new FontSet(new FallbackFonts(device, "Segoe UI"));
        foreach (var font in SprocketContent.Instance.Fonts)
            fonts = fonts.And(font.Name, font.Metrics(device));

        device.Fonts = fonts;
        return new Face(shape.Name) { Shape = shape, Measuring = device };
    }
}
