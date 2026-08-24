using VRage.Utils;
using VRageMath;

namespace Phosphor
{
    /// <summary>
    /// One symbol, held to a square and centred in whatever room it is given.
    /// </summary>
    /// <remarks>
    /// Square because every symbol here is, and centred because a symbol beside a line of text belongs on
    /// the middle of that line rather than at the top of the row it shares. A row is as tall as its tallest
    /// thing, and without this a chevron next to a two-line summary would sit against the first line.
    /// </remarks>
    public class Glyph : Element
    {
        public Glyph(MyStringId material, float size, Color tint)
        {
            Material = material;
            Size = size;
            Tint = tint;
        }

        /// <summary>Which symbol. One of <see cref="Symbols" />.</summary>
        public MyStringId Material { get; set; }

        /// <summary>How big, across and down.</summary>
        public float Size { get; set; }

        /// <summary>
        /// What colour to draw it. Transparent draws nothing.
        /// </summary>
        /// <remarks>
        /// The room is still taken when this is transparent, which is the point: a symbol that turns off
        /// and on - a back arrow on the first screen, an undo that has nothing to walk back - would
        /// otherwise drag everything beside it sideways. Use <see cref="Element.Collapsed" /> for a symbol
        /// that should give its room back.
        /// </remarks>
        public Color Tint { get; set; }

        /// <summary>How far round to turn it, clockwise, about its own centre.</summary>
        public float Rotation { get; set; }

        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            return new Vector2(Size, Size);
        }

        protected override void DrawContent(IGraphics g)
        {
            if (Tint.A == 0)
                return;

            RectangleF room = ContentBounds;
            g.DrawSprite(Material, new RectangleF(
                room.X + (room.Width - Size) * 0.5f,
                room.Y + (room.Height - Size) * 0.5f,
                Size, Size), Tint, Rotation);
        }
    }
}
