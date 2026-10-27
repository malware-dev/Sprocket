using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Lumel;
using Lumel.Layout;
using Lumel.Text;
using Lumel.Viewing;
using VRage.Game;
using VRageMath;
using Color = VRageMath.Color;
using Size = System.Windows.Size;
using Thickness = Lumel.Thickness;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Lumel.Sim;
using Lumel.Styling;

namespace Sprocket.Sim.Simulation;

/// <summary>A text panel's screen as the rich text app would show it, drawn to a picture of the screen's own size.</summary>
/// <remarks>
/// Lays out the full text wrapped to the screen, and where it runs past the screen's height, the short text in its place:
/// how an empty panel picks between its two sets of instructions.
/// </remarks>
public static class PanelShot
{
    static readonly IStyle<RichText> Screen = new RichTextStyle
    {
        Base =
        {
            Font = MyFontEnum.White,
            StrongFont = TextSizes.Bold,
            EmphasisFont = TextSizes.Italic,
            StrongEmphasisFont = TextSizes.BoldItalic,
            CodeFont = Surface.GameMonospace,
            FontSize = 24f,
            Heading1Size = 36f,
            Heading2Size = 30f,
            Heading3Size = 24f,
            Color = Palette.Ink,
            StrongColor = Palette.Ink,
            EmphasisColor = Palette.Ink,
            HeadingColor = Palette.Ink,
            RuleColor = Palette.Line,
            BlockSpacing = 12f,
            Indent = 24f
        }
    };

    /// <summary>Writes the screen <paramref name="wide" /> by <paramref name="high" /> to <paramref name="path" />, and answers which text it showed.</summary>
    /// <remarks>
    /// The full text wraps at the screen's own size, and the brief one stands in where the full one does not fit its height, as
    /// the app does. The words are drawn in <paramref name="ink" /> on <paramref name="screen" /> where those are given, as a
    /// panel's script colors would draw them, and in the editor's colors otherwise. Where <paramref name="fit" /> is set the
    /// full text is fitted to the screen, as a panel set to fit its text draws it. Code is drawn in the game's monospace face,
    /// as a panel draws it, or in <paramref name="code" /> where that is given.
    /// </remarks>
    public static string Save(string path, string full, string brief, float wide, float high, Color? ink, Color? screen, bool fit,
        string code)
    {
        const float margin = 8f;
        var room = new Vector2(wide - margin * 2f, high - margin * 2f);

        var text = new RichText { Style = Screen, Document = Markdown.Parse(full) };
        if (code != null)
            text.Base.CodeFont = code;

        if (ink.HasValue)
        {
            text.Base.Color = ink.Value;
            text.Base.StrongColor = ink.Value;
            text.Base.EmphasisColor = ink.Value;
            text.Base.HeadingColor = ink.Value;
            text.Base.RuleColor = Color.Lerp(screen ?? Palette.Page, ink.Value, 0.5f);
        }

        Element shown = fit ? new Viewbox { Content = text, Placement = FixedSize.Size(room.X, room.Y) } : text;
        var page = new VStack { Base = { Background = screen ?? Palette.Page, Padding = new Thickness(margin) }, Placement = FixedSize.Size(wide, high), Children = { shown } };

        var surface = ScreenSession.Surface();
        surface.Target = new PixelSize(1920, 1080);
        var said = "full";

        surface.Frame += g =>
        {
            page.Measure(g, new Vector2(wide, high));
            if (!fit && text.DesiredSize.Y > room.Y && said == "full")
            {
                said = "brief";
                text.Document = Markdown.Parse(brief);
                page.Measure(g, new Vector2(wide, high));
            }

            page.Arrange(new RectangleF(0f, 0f, wide, high));
            page.Draw(g);
        };

        var bitmap = new RenderTargetBitmap(1920, 1080, 96d, 96d, PixelFormats.Pbgra32);
        var frame = new DrawingVisual();
        using (DrawingContext context = frame.RenderOpen())
            surface.Paint(context, new Size(1920, 1080));

        bitmap.Render(frame);

        var left = (1920 - 1440) / 2;
        var cut = new CroppedBitmap(bitmap, new Int32Rect(left, 0, (int)wide, (int)high));

        var png = new PngBitmapEncoder();
        png.Frames.Add(BitmapFrame.Create(cut));
        using (FileStream file = File.Create(path))
            png.Save(file);

        return said;
    }
}
