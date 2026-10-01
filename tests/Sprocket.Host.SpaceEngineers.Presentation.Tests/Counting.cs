using System.Text;
using Lumel;
using Lumel.Drawing;
using VRage.Utils;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A graphics that measures every character as half its size wide, and counts what it is asked to measure.</summary>
/// <remarks>Exact and the same on every machine, which is what a budget test needs from it.</remarks>
sealed class Counting : IGraphics
{
    /// <summary>How many times anything was measured.</summary>
    public int Calls { get; private set; }

    /// <summary>How many characters were handed over to be measured, in all.</summary>
    public long Characters { get; private set; }

    public Vector2 Size => new Vector2(1440f, 1080f);

    /// <summary>Starts both counts again.</summary>
    public void Reset()
    {
        Calls = 0;
        Characters = 0;
    }

    public Vector2 MeasureText(string font, StringBuilder text, float size)
    {
        Calls++;
        Characters += text.Length;
        return new Vector2(text.Length * size * 0.5f, size);
    }

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

    public void Fill(RectangleF destination, Color color, float rotation = 0f)
    {
    }

    public void DrawText(string font, string text, Vector2 position, float size, Color color)
    {
    }

    public void DrawText(string font, string text, Vector2 position, float size, Color color, Align align)
    {
    }
}
