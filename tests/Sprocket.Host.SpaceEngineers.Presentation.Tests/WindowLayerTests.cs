using System.Text;
using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Controls;
using Sprocket.Host.SpaceEngineers.Ui.Drawing;
using Sprocket.Host.SpaceEngineers.Ui.Input;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.Ui.Windows;
using VRage.Utils;
using VRageMath;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>Windows over a screen: placed by the layer, drawn over what is beneath, and a modal one keeping input for itself.</summary>
public class WindowLayerTests
{
    static readonly RectangleF Area = new RectangleF(0f, 0f, 1000f, 800f);

    /// <summary>A window stands in the middle of the area, as wide as its placement says.</summary>
    [Fact]
    public void AWindowStandsInTheMiddle()
    {
        var (layer, window, _) = Opened(modal: false);

        Assert.Equal(300f, window.Bounds.Width);
        Assert.Equal(Area.Width / 2f, window.Bounds.X + window.Bounds.Width / 2f, 1);
        Assert.Equal(Area.Height / 2f, window.Bounds.Y + window.Bounds.Height / 2f, 1);
        Assert.Equal(1, layer.Count);
    }

    /// <summary>A press on what a window holds reaches it, and the screen beneath is told to stay out of it.</summary>
    [Fact]
    public void APressOnAWindowReachesWhatItHolds()
    {
        var (layer, _, button) = Opened(modal: false);

        var hit = layer.Under(Middle(button), out var stopped);

        Assert.Same(button, hit);
        Assert.True(stopped);
    }

    /// <summary>Beside a modeless window, a press is left for the screen beneath; beside a modal one it reaches nothing at all.</summary>
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void AModalWindowKeepsPressesFromTheScreen(bool modal, bool kept)
    {
        var (layer, _, _) = Opened(modal);

        var hit = layer.Under(new Vector2(5f, 5f), out var stopped);

        Assert.Null(hit);
        Assert.Equal(kept, stopped);
        Assert.Equal(modal, layer.Modal != null);
    }

    /// <summary>A modal window's backdrop is painted over the whole area before the window is drawn.</summary>
    [Fact]
    public void AModalWindowPaintsItsBackdropFirst()
    {
        var graphics = new Recording();
        var layer = new WindowLayer();
        layer.Open(new Dialog { Backdrop = Color.Black, Background = Color.White, Content = new Button { Text = "Go" } });

        layer.Draw(graphics, Area);

        Assert.Equal(Area, graphics.Fills[0]);
        Assert.True(graphics.Fills.Count > 1, "the window itself was not drawn");
    }

    /// <summary>Closing a window takes it away, and the screen has the input back.</summary>
    [Fact]
    public void ClosingGivesTheInputBack()
    {
        var (layer, window, _) = Opened(modal: true);

        layer.Close(window);
        layer.Under(new Vector2(5f, 5f), out var stopped);

        Assert.False(stopped);
        Assert.Equal(0, layer.Count);
    }

    static (WindowLayer, Window, Button) Opened(bool modal)
    {
        var button = new Button { Text = "Go", Command = new Relay(() => { }) };
        var window = new Window { IsModal = modal, Placement = Fixed.Wide(300f), Content = new VStack { Children = { button } } };
        var layer = new WindowLayer();
        layer.Open(window);
        layer.Draw(new Recording(), Area);
        return (layer, window, button);
    }

    static Vector2 Middle(Element element) =>
        new Vector2(element.Bounds.X + element.Bounds.Width / 2f, element.Bounds.Y + element.Bounds.Height / 2f);

    /// <summary>A graphics that measures every character as half its size wide, and keeps where it was asked to fill.</summary>
    sealed class Recording : IGraphics
    {
        public List<RectangleF> Fills { get; } = new List<RectangleF>();

        public Vector2 Size => new Vector2(Area.Width, Area.Height);

        public Vector2 MeasureText(string font, StringBuilder text, float size) => new Vector2(text.Length * size * 0.5f, size);

        public void Fill(RectangleF destination, Color color, float rotation = 0f) => Fills.Add(destination);

        public void PushClip(RectangleF bounds)
        {
        }

        public void PopClip()
        {
        }

        public void PushTransform(RenderTransform transform, Vector2 origin)
        {
        }

        public void PopTransform()
        {
        }

        public void DrawSprite(MyStringId material, RectangleF destination, Color color, float rotation)
        {
        }

        public void DrawText(string font, string text, Vector2 position, float size, Color color)
        {
        }

        public void DrawText(string font, string text, Vector2 position, float size, Color color, Align align)
        {
        }
    }
}
