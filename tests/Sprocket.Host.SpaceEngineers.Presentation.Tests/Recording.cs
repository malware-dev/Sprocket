using System.Text;
using Lumel;
using Lumel.Drawing;
using Lumel.Images;
using VRage.Utils;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A graphics that measures every character as half its size wide, and keeps where it was asked to fill.</summary>
sealed class Recording(RectangleF area) : IGraphics
{
    public List<RectangleF> Fills { get; } = new List<RectangleF>();

    /// <summary>Every picture drawn from a part of a texture, with where it was drawn, in order.</summary>
    public List<(MyStringId Material, RectangleF Destination)> Pieces { get; } = new List<(MyStringId, RectangleF)>();

    public Vector2 Size => new Vector2(area.Width, area.Height);

    public RectangleF ScreenBounds => area;

    public IImageCatalog? Images => null;

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

    public void DrawSprite(MyStringId material, RectangleF destination, Rectangle source, Color color) =>
        Pieces.Add((material, destination));

    public void DrawText(string font, string text, Vector2 position, float size, Color color)
    {
    }

    public void DrawText(string font, string text, Vector2 position, float size, Color color, TextAlign align)
    {
    }
}
