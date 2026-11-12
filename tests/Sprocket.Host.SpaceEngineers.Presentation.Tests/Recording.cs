using System.Text;
using Lumel;
using Lumel.Drawing;
using Lumel.Images;
using VRage.Utils;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A graphics that measures every character as half its size wide.</summary>
sealed class Recording(RectangleF area) : IGraphics
{
    /// <summary>Every picture drawn from a part of a texture, with where it was drawn, in order.</summary>
    public List<(MyStringId Material, RectangleF Destination)> Pieces { get; } = new List<(MyStringId, RectangleF)>();

    public Vector2 Size => new Vector2(area.Width, area.Height);

    public RectangleF ScreenBounds => area;

    public IImageCatalog? Images => null;

    /// <summary>The tick every frame is drawn at: the first.</summary>
    public int Now => 0;

    /// <summary>How many times something drawn asked for the next frame too.</summary>
    public int FramesRequested { get; private set; }

    public void RequestFrame() => FramesRequested++;

    /// <summary>Whether it draws part of a picture as the screen does, or the whole picture as a text panel does.</summary>
    public bool SupportsSourceRectangles { get; set; } = true;

    public Vector2 MeasureText(string font, StringBuilder text, float size) => new Vector2(text.Length * size * 0.5f, size);

    public Vector2 ToDevicePixels(Vector2 point) => point;

    public Vector2 FromDevicePixels(Vector2 pixels) => pixels;

    public void Fill(RectangleF destination, Color color, float rotation = 0f) { }

    /// <summary>How many clips were pushed.</summary>
    public int Clips { get; private set; }

    public void PushClip(RectangleF bounds) => Clips++;

    public void PopClip()
    {
    }

    public void PushTransform(RenderTransform transform, Vector2 origin)
    {
    }

    public void PopTransform()
    {
    }

    public void DrawSprite(MyStringId material, RectangleF destination, Color color, float rotation) =>
        Pieces.Add((material, destination));

    public void DrawSprite(MyStringId material, RectangleF destination, Rectangle source, Color color) =>
        Pieces.Add((material, destination));

    public void DrawText(string font, string text, Vector2 position, float size, Color color)
    {
    }

    public void DrawText(string font, string text, Vector2 position, float size, Color color, TextAlign align)
    {
    }
}
