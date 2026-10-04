using Lumel.Drawing;
using VRageMath;

namespace Lumel.Controls
{
    /// <summary>
    /// A line down a gutter that ties what is set in by it to what it hangs from, turning in towards them at the middle of
    /// <see cref="Ends" />.
    /// </summary>
    /// <remarks>
    /// The turn is drawn past the spine's own width, into the room between the gutter and what it holds. Something that draws
    /// its own share of a longer spine - one row of many - draws it with <see cref="Down" /> and <see cref="Turning" />, so every
    /// stretch of a spine looks the same.
    /// </remarks>
    public class Spine : Element
    {
        /// <summary>A spine <paramref name="thickness" /> wide in <paramref name="color" />, turning in by <paramref name="turn" />.</summary>
        public Spine(float thickness, float turn, Color color)
        {
            Thickness = thickness;
            Turn = turn;
            Color = color;
        }

        /// <summary>How wide the line is.</summary>
        public float Thickness { get; set; }

        /// <summary>How far the line turns in at its end.</summary>
        public float Turn { get; set; }

        /// <summary>The colour of the line.</summary>
        public Color Color { get; set; }

        /// <summary>The element the spine turns in at, or null for a line the whole height of the spine.</summary>
        public Element Ends { get; set; }

        /// <summary>Draws a stretch of spine <paramref name="thickness" /> wide at <paramref name="x" />, from <paramref name="top" /> to <paramref name="bottom" />.</summary>
        public static void Down(IGraphics g, float x, float top, float bottom, float thickness, Color color)
        {
            g.Fill(new RectangleF(x, top, thickness, bottom - top), color);
        }

        /// <summary>Draws the last stretch of a spine at <paramref name="x" />, from <paramref name="top" /> to where it turns in by <paramref name="turn" /> at <paramref name="at" />.</summary>
        public static void Turning(IGraphics g, float x, float top, float at, float thickness, float turn, Color color)
        {
            Down(g, x, top, at + thickness, thickness, color);
            g.Fill(new RectangleF(x, at, turn, thickness), color);
        }

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            return new Vector2(Thickness, 0f);
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            if (Ends == null || !Ends.IsVisible)
                Down(g, Bounds.X, Bounds.Y, Bounds.Y + Bounds.Height, Thickness, Color);
            else
                Turning(g, Bounds.X, Bounds.Y, Ends.Bounds.Y + Ends.Bounds.Height * 0.5f, Thickness, Turn, Color);
        }
    }
}
