using System;
using Lumel.Drawing;
using VRage.Utils;
using VRageMath;

namespace Lumel.Controls
{
    /// <summary>One amount between none and all of it, set by putting a grip where it goes along a track.</summary>
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

        /// <summary>How tall the track is drawn, centered down the slider, or zero for its whole height.</summary>
        public float TrackHeight { get; set; }

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
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            return new Vector2(float.IsInfinity(available.X) ? Thick : available.X, Thick);
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            var room = ContentBounds;
            var track = TrackHeight > 0f && TrackHeight < room.Height
                ? new RectangleF(room.X, room.Y + (room.Height - TrackHeight) * 0.5f, room.Width, TrackHeight)
                : room;
            if (Track == MyStringId.NullOrEmpty)
                g.Fill(track, TrackTint);
            else
                g.DrawSprite(Track, track, TrackTint, 0f);

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
