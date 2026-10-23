using VRage.Utils;
using VRageMath;

namespace Lumel.Drawing
{
    /// <summary>A square picture laid edge to edge across a rectangle, from its top left corner, tinted one color: a texture repeated.</summary>
    /// <remarks>Never changes once made, so one pattern is shared by every style that lays it.</remarks>
    public sealed class Pattern
    {
        readonly MyStringId _material;
        readonly int _size;
        readonly float _tile;
        readonly Color _color;

        /// <summary>The picture <paramref name="material" />, <paramref name="size" /> pixels square, laid <paramref name="tile" /> UI units square, tinted <paramref name="color" />.</summary>
        public Pattern(MyStringId material, int size, float tile, Color color)
        {
            _material = material;
            _size = size;
            _tile = tile;
            _color = color;
        }

        /// <summary>Lays the picture across <paramref name="bounds" />, the tiles at its right and bottom edges cut to fit.</summary>
        /// <remarks>One sprite per tile, so a row as tall as a tile costs one for each tile of its width.</remarks>
        public void Draw(IGraphics g, RectangleF bounds)
        {
            if (_tile <= 0f)
                return;

            var right = bounds.X + bounds.Width;
            var bottom = bounds.Y + bounds.Height;
            for (var y = bounds.Y; y < bottom; y += _tile)
            {
                var high = y + _tile <= bottom ? _tile : bottom - y;
                for (var x = bounds.X; x < right; x += _tile)
                {
                    var wide = x + _tile <= right ? _tile : right - x;
                    var source = new Rectangle(0, 0, Pixels(wide), Pixels(high));
                    g.DrawSprite(_material, new RectangleF(x, y, wide, high), source, _color);
                }
            }
        }

        /// <summary>How many of the picture's pixels <paramref name="span" /> of a tile shows, at least one.</summary>
        int Pixels(float span)
        {
            var pixels = (int)(span / _tile * _size + 0.5f);
            return pixels < 1 ? 1 : pixels;
        }
    }
}
