using System;
using Lumel.Drawing;
using VRage.Utils;
using VRageMath;

namespace Lumel.Controls
{
    /// <summary>Every saturation and value of one hue, as a square to put a mark on: saturation across, value up.</summary>
    /// <remarks>Drawn in three: a fill of the hue, <see cref="Pale" /> over it, and <see cref="Dark" /> over both.</remarks>
    public class ColorPlane : Element
    {
        /// <summary>How wide the mark is.</summary>
        const float Mark = 10f;

        float _hue;
        float _saturation;
        float _value;

        /// <summary>White, solid at the left edge and gone at the right, in a straight run: saturation across.</summary>
        public MyStringId Pale { get; set; }

        /// <summary>Black, gone at the top edge and solid at the bottom, in a straight run: value up.</summary>
        public MyStringId Dark { get; set; }

        /// <summary>Raised when the mark has been moved by the pointer, rather than by setting the parts.</summary>
        public event Action Moved;

        /// <summary>The hue the square shows, from nought to one round the wheel.</summary>
        public float Hue
        {
            get { return _hue; }
            set { _hue = Held(value); }
        }

        /// <summary>Where the mark is across, from none of the hue at the left to all of it at the right.</summary>
        public float Saturation
        {
            get { return _saturation; }
            set { _saturation = Held(value); }
        }

        /// <summary>Where the mark is up, from black at the bottom to full at the top.</summary>
        public float Value
        {
            get { return _value; }
            set { _value = Held(value); }
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
            g.Fill(room, new Vector3(_hue, 1f, 1f).HSVtoColor());
            g.DrawSprite(Pale, room, Color.White, 0f);
            g.DrawSprite(Dark, room, Color.White, 0f);

            // A ring that shows on light and dark alike: black round white.
            var at = new Vector2(room.X + room.Width * _saturation, room.Y + room.Height * (1f - _value));
            Ring(g, at, Mark + 4f, Color.Black);
            Ring(g, at, Mark, Color.White);
        }

        /// <summary>A square outline <paramref name="size" /> across, centered on <paramref name="at" />.</summary>
        static void Ring(IGraphics g, Vector2 at, float size, Color color)
        {
            var corner = at - new Vector2(size / 2f);
            g.Fill(new RectangleF(corner.X, corner.Y, size, 2f), color);
            g.Fill(new RectangleF(corner.X, corner.Y + size - 2f, size, 2f), color);
            g.Fill(new RectangleF(corner.X, corner.Y, 2f, size), color);
            g.Fill(new RectangleF(corner.X + size - 2f, corner.Y, 2f, size), color);
        }

        /// <summary>Puts the mark under <paramref name="point" />.</summary>
        void Follow(Vector2 point)
        {
            var room = ContentBounds;
            Saturation = room.Width <= 0f ? 0f : (point.X - room.X) / room.Width;
            Value = room.Height <= 0f ? 0f : 1f - (point.Y - room.Y) / room.Height;

            var moved = Moved;
            if (moved != null)
                moved();
        }

        static float Held(float part)
        {
            return part < 0f ? 0f : part > 1f ? 1f : part;
        }
    }
}
