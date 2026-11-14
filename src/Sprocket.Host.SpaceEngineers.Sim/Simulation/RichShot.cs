using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Lumel;
using Lumel.Layout;
using VRage.Game;
using VRageMath;
using Color = VRageMath.Color;
using Size = System.Windows.Size;
using Thickness = Lumel.Thickness;
using TextAlignment = Lumel.Text.TextAlignment;
using Lumel.Text;
using Lumel.Viewing;
using Lumel.Sim;
using Lumel.Styling;

namespace Sprocket.Sim.Simulation;

/// <summary>Draws a piece of markdown-lite through the rich text element, on the editor's page, and writes the picture.</summary>
/// <remarks>At 1080 high, where one UI unit is one pixel, and cut to the text so the picture is as big as it is.</remarks>
public static class RichShot
{
    static readonly IStyle<RichText> Page = new RichTextStyle
    {
        Base =
        {
            CodeBackground = new Color(255, 255, 255, 10), CodePadding = new Thickness(4f, -1f),
            Font = MyFontEnum.White,
            StrongFont = TextSizes.Bold,
            EmphasisFont = TextSizes.Italic,
            StrongEmphasisFont = TextSizes.BoldItalic,
            CodeFont = TextSizes.Mono,
            FontSize = 20f,
            Heading1Size = 32f,
            Heading2Size = 24f,
            Heading3Size = 20f,
            Color = Palette.Muted,
            StrongColor = Palette.Muted,
            EmphasisColor = Palette.Muted,
            LinkColor = Palette.Link,
            HeadingColor = Palette.Ink,
            RuleColor = Palette.Line,
            BlockSpacing = 12f,
            Indent = 24f,
            Callouts =
            {
                ["warning"] = { BarColor = Palette.Notice, Background = Palette.NoticeWash, Color = Palette.Ink },
                ["player"] = { BarColor = Palette.Accent, Background = Palette.Panel, IsEmphasized = true }
            }
        }
    };

    /// <summary>Writes <paramref name="markdown" />, laid out <paramref name="wide" /> across, to <paramref name="path" />, and answers how big the picture is.</summary>
    /// <remarks>Drawn in <paramref name="style" />, or the style of a page where that is null. A strong, emphasis or link color given here is tried in place of the style's, for comparing one against another; the page is laid out as <paramref name="align" /> says, and shrinks to fit a room <paramref name="fit" /> tall where that is above zero.</remarks>
    public static Vector2 Save(string path, string markdown, float wide, IStyle<RichText> style, Color? strong, Color? emphasis, Color? link,
        TextAlignment align, float fit)
    {
        var text = new RichText { Base = { TextAlignment = align }, Style = style ?? Page, Document = Markdown.Parse(markdown) };

        if (strong.HasValue)
            text.Base.StrongColor = strong.Value;

        if (link.HasValue)
            text.Base.LinkColor = link.Value;

        if (emphasis.HasValue)
            text.Base.EmphasisColor = emphasis.Value;

        Element shown = fit > 0f ? new Viewbox { Content = text, StretchDirection = StretchDirection.DownOnly, Placement = FixedSize.Size(wide, fit) } : text;
        var page = new VStack { Base = { Background = Palette.Page, Padding = new Thickness(24f) }, Children = { shown } };

        var surface = ScreenSession.Surface();
        surface.Target = new PixelSize(1920, 1080);
        var drawn = Vector2.Zero;

        surface.Frame += g =>
        {
            page.Measure(g, new Vector2(wide + 48f, 1080f));
            drawn = page.DesiredSize;
            page.Arrange(new RectangleF(0f, 0f, drawn.X, drawn.Y));
            page.Draw(g);
        };

        var bitmap = new RenderTargetBitmap(1920, 1080, 96d, 96d, PixelFormats.Pbgra32);
        var frame = new DrawingVisual();
        using (DrawingContext context = frame.RenderOpen())
            surface.Paint(context, new Size(1920, 1080));

        bitmap.Render(frame);

        var left = (1920 - 1440) / 2;
        var width = (int)System.Math.Ceiling(drawn.X);
        var height = (int)System.Math.Min(1080, System.Math.Ceiling(drawn.Y));
        var cut = new CroppedBitmap(bitmap, new Int32Rect(left, 0, width, height));

        var png = new PngBitmapEncoder();
        png.Frames.Add(BitmapFrame.Create(cut));
        using (FileStream file = File.Create(path))
            png.Save(file);

        return new Vector2(width, height);
    }
}
