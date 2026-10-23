using System.Text;
using Lumel.Drawing;
using Lumel.Images;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Features.Look
{
    /// <summary>One symbol, drawn in the color the screens draw marks in, three quarters the height of its line and centered in its room.</summary>
    public sealed class MarkImage : IImageSource
    {
        // Chosen by eye against the words of a page.
        const float Height = 0.75f;

        readonly string _symbol;
        readonly StringBuilder _measuring;

        // The symbol's size at a size of one: a measure is the font's pixel metrics times the scale the size gives
        // (Sim/Content/SeFontMetrics.cs, transcribed from the game), so it grows in step with the size.
        Vector2 _perSize;
        bool _measured;

        /// <summary>The mark <paramref name="symbol" />.</summary>
        public MarkImage(string symbol)
        {
            _symbol = symbol;
            _measuring = new StringBuilder(symbol);
        }

        /// <inheritdoc />
        public Vector2 Size => new Vector2(Height, 1f);

        /// <inheritdoc />
        /// <remarks>Measures the symbol the first time it is drawn, and scales that for every height after.</remarks>
        public void Draw(IGraphics g, RectangleF destination, Color tint, float rotation)
        {
            var size = destination.Height * Height;
            if (!_measured && size > 0f)
            {
                _perSize = g.MeasureText(Symbols.Font, _measuring, size) / size;
                _measured = true;
            }

            var measured = _perSize * size;

            var ink = Palette.Muted;
            var color = new Color(ink.R * tint.R / 255, ink.G * tint.G / 255, ink.B * tint.B / 255, ink.A * tint.A / 255);
            g.DrawText(Symbols.Font, _symbol, new Vector2(
                destination.X + (destination.Width - measured.X) * 0.5f,
                destination.Y + (destination.Height - measured.Y) * 0.5f), size, color);
        }
    }
}
