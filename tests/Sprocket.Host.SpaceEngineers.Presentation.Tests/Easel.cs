using System.Text;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Drawing;
using Sprocket.Host.SpaceEngineers.Layout;
using Sprocket.Sim.Content;
using Sprocket.Sim.Simulation;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// One element, measured, arranged and drawn with nothing else on the screen.
/// </summary>
/// <remarks>
/// The same device and graphics <see cref="Offscreen" /> uses, without the editor above them, so a
/// control can be asked what it does on its own rather than through whichever screen happens to use
/// it. What comes back is the frame as a list of calls, which is what makes cost assertable.
/// </remarks>
sealed class Easel
{
    readonly ScreenSpaceDevice _device;
    readonly ScreenSpaceGraphics _graphics;
    readonly Element _element;

    readonly bool _exact;

    public Easel(Element element, float width, float height) : this(element, width, height, false)
    {
    }

    /// <summary>
    /// The same, optionally measuring with the game's own font rather than a stand-in.
    /// </summary>
    /// <remarks>
    /// The stand-in's widths are not the game's and it says so: its digits are all one width, where the
    /// game's are not remotely - a <c>1</c> is less than half a <c>0</c>. Anything about how text of
    /// different widths lines up therefore cannot be seen with it, and has to ask for this instead.
    /// </remarks>
    public Easel(Element element, float width, float height, bool exact)
    {
        _element = element;

        _device = new ScreenSpaceDevice();

        SeFontMetrics? game = exact ? SeFontMetrics.Load(_device) : null;
        _exact = game != null;
        _device.Fonts = game ?? (IFontMetrics)new FallbackFonts(_device, "Segoe UI");

        _device.Install();
        _device.SafeArea = new RectangleF(0f, 0f, width, height);
        _device.ViewportSize = new Vector2(width, height);

        _graphics = new ScreenSpaceGraphics(_device, MyScreenSpaceLayer.AboveHud);

        Draw(width, height);
    }

    /// <summary>Everything the last frame asked for.</summary>
    public IList<DrawCommand> Commands => _device.Commands;

    /// <summary>The runs of text the last frame drew.</summary>
    public IEnumerable<DrawCommand> Written => Commands.Where(c => c.Kind == DrawKind.Text);

    /// <summary>The sprites the last frame drew, which for a bare control means its thumbs.</summary>
    public IEnumerable<DrawCommand> Sprites => Commands.Where(c => c.Kind == DrawKind.Sprite);

    /// <summary>How many times anything has asked for the width of a string.</summary>
    public int Measured => _device.Measured;

    /// <summary>A line of the default face, in the units the screen lays out in.</summary>
    public float LineHeight => _graphics.LineHeight(VRage.Game.MyFontEnum.White, 1f);

    /// <summary>
    /// Window pixels per UI unit.
    /// </summary>
    /// <remarks>
    /// A recorded call carries pixels and a layout carries UI units, and the two are only the same on
    /// a screen the height the layout was written against. Anything comparing a drawn position with a
    /// number the layout was given goes through here.
    /// </remarks>
    public float Pixels => _device.ViewportSize.Y / _graphics.Size.Y;

    /// <summary>Whether the widths here are the game's own, rather than a stand-in's.</summary>
    public bool IsExact => _exact;

    /// <summary>How wide a run is, in the units a drawn position is recorded in.</summary>
    public float WidthOf(string said)
    {
        _measuring.Length = 0;
        _measuring.Append(said);

        return _graphics.MeasureText(VRage.Game.MyFontEnum.White, _measuring, 1f).X * Pixels;
    }

    readonly StringBuilder _measuring = new StringBuilder(32);

    /// <summary>Lays out and draws again, which is what a frame is.</summary>
    public void Draw(float width, float height)
    {
        _device.BeginFrame();
        _graphics.Begin();

        _element.Measure(_graphics, new Vector2(width, height));
        _element.Arrange(new RectangleF(0f, 0f, width, height));
        _element.Draw(_graphics);
    }
}
