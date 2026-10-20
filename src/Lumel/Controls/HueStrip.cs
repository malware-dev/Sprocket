using System;
using Lumel.Drawing;
using VRage.Utils;
using VRageMath;

namespace Lumel.Controls
{
    /// <summary>Every hue round the wheel, down a strip, with a mark at the one chosen: red at the top and again at the bottom.</summary>
    public class HueStrip : Element
    {
        float _hue;

        /// <summary>The picture of every hue in order, red at the top round to red at the bottom.</summary>
        public MyStringId Hues { get; set; }

        /// <summary>Raised when the mark has been moved by the pointer, rather than by setting <see cref="Hue" />.</summary>
        public event Action Moved;

        /// <summary>Where the mark is, from nought at the top to one at the bottom.</summary>
        public float Hue
        {
            get { return _hue; }
            set { _hue = value < 0f ? 0f : value > 1f ? 1f : value; }
        }

        /// <inheritdoc />
        protected override bool TakesPointer => true;

        /// <inheritdoc />
        protected override void OnPointerDown(Vector2 point, int presses, bool shift, bool ctrl)
        {
            Follow(point);
        }

        /// <inheritdoc />
        protected override void OnPointerMove(Vector2 point)
        {
            Follow(point);
        }

        /// <inheritdoc />
        /// <remarks>Asks for no room of its own: its placement gives it its size.</remarks>
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            return Vector2.Zero;
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            var room = ContentBounds;
            g.DrawSprite(Hues, room, Color.White, 0f);

            // A bar across the strip that shows on any hue: black round white.
            var y = room.Y + room.Height * _hue;
            g.Fill(new RectangleF(room.X - 2f, y - 3f, room.Width + 4f, 6f), Color.Black);
            g.Fill(new RectangleF(room.X, y - 1f, room.Width, 2f), Color.White);
        }

        /// <summary>Puts the mark under <paramref name="point" />.</summary>
        void Follow(Vector2 point)
        {
            var room = ContentBounds;
            Hue = room.Height <= 0f ? 0f : (point.Y - room.Y) / room.Height;

            var moved = Moved;
            if (moved != null)
                moved();
        }
    }
}
