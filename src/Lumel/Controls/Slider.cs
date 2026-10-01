using System;
using Lumel.Drawing;
using VRage.Utils;
using VRageMath;

namespace Lumel.Controls
{
    /// <summary>One amount between none and all of it, set by putting a grip where it goes along a track.</summary>
    /// <remarks>The track takes the pointer itself: a press moves the grip there at once, and dragging carries it.</remarks>
    public class Slider : Element
    {
        const float Thick = 24f;
        const float Grip = 8f;
        const float Lip = 4f;

        float _fraction;

        /// <summary>A slider resting at none, on a track filled plainly.</summary>
        public Slider()
        {
            TrackTint = Color.White;
            GripColor = Color.White;
        }

        /// <summary>Raised when the grip has been moved by the pointer, rather than by setting <see cref="Fraction" />.</summary>
        public event Action Moved;

        /// <summary>Where the grip is, from none at the left to all at the right.</summary>
        /// <remarks>Changes only what is drawn, so it invalidates nothing.</remarks>
        public float Fraction
        {
            get { return _fraction; }
            set { _fraction = value < 0f ? 0f : value > 1f ? 1f : value; }
        }

        /// <summary>A picture the track is drawn with, or the empty id for a plain fill.</summary>
        public MyStringId Track { get; set; }

        /// <summary>What the track is tinted, which is white for a picture that is already its own colors.</summary>
        public Color TrackTint { get; set; }

        /// <summary>What the grip is filled with.</summary>
        public Color GripColor { get; set; }

        /// <inheritdoc />
        protected override bool TakesPointer => true;

        /// <inheritdoc />
        public override void OnPointerDown(Vector2 point, int presses, bool shift, bool ctrl)
        {
            Follow(point);
        }

        /// <inheritdoc />
        public override void OnPointerMove(Vector2 point)
        {
            Follow(point);
        }

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            return new Vector2(float.IsInfinity(available.X) ? Thick : available.X, Thick);
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            var room = ContentBounds;
            if (Track == MyStringId.NullOrEmpty)
                g.Fill(room, TrackTint);
            else
                g.DrawSprite(Track, room, TrackTint, 0f);

            var left = room.X + (room.Width - Grip) * _fraction;
            g.Fill(new RectangleF(left, room.Y - Lip, Grip, room.Height + Lip * 2f), GripColor);
        }

        /// <summary>Puts the grip under <paramref name="point" />, measured over the grip's travel so both ends can be reached.</summary>
        void Follow(Vector2 point)
        {
            var room = ContentBounds;
            var travel = room.Width - Grip;
            Fraction = travel <= 0f ? 0f : (point.X - (room.X + Grip / 2f)) / travel;

            var moved = Moved;
            if (moved != null)
                moved();
        }
    }
}
