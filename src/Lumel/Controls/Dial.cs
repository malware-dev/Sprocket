using System;
using Lumel.Drawing;
using VRageMath;

namespace Lumel.Controls
{
    /// <summary>An angle shown and set as a needle on a dial: marks round the edge, and the needle from the middle.</summary>
    public class Dial : Element
    {
        const float Across = 96f;
        const float Mark = 8f;
        const float Quarter = 12f;
        const float Thin = 2f;
        const float Needle = 4f;
        const float Hub = 8f;
        const float RightAngle = 90f;

        float _degrees;
        float _most = 360f;
        bool _half;

        /// <summary>A dial with no angle on it, reaching a whole turn either way.</summary>
        public Dial()
        {
            MarkColor = Color.Gray;
            QuarterColor = Color.White;
            NeedleColor = Color.White;
            HeldColor = Color.White;
        }

        /// <summary>Raised when the needle has been turned by the pointer, rather than by setting <see cref="Degrees" />.</summary>
        public event Action Turned;

        /// <summary>Where the needle points, clockwise from straight up, held within <see cref="Most" /> either way.</summary>
        public float Degrees
        {
            get { return _degrees; }
            set { _degrees = Math.Max(-_most, Math.Min(_most, value)); }
        }

        /// <summary>Whether there is an angle to point at, without which only the marks and the hub are drawn.</summary>
        public bool Shows { get; set; }

        /// <summary>How far the needle may go either way; a quarter turn or less is drawn as the top half of a dial, half as tall.</summary>
        public float Most
        {
            get { return _most; }
            set
            {
                _most = Math.Max(0f, value);
                _degrees = Math.Max(-_most, Math.Min(_most, _degrees));

                var half = _most <= RightAngle;
                if (half == _half)
                    return;

                _half = half;
                InvalidateMeasure();
            }
        }

        /// <summary>The color of the marks between the quarters.</summary>
        public Color MarkColor { get; set; }

        /// <summary>The color of the marks on the quarters.</summary>
        public Color QuarterColor { get; set; }

        /// <summary>The color of the needle and the hub.</summary>
        public Color NeedleColor { get; set; }

        /// <summary>The color of the needle and the hub while the pointer is on the dial or turning it.</summary>
        public Color HeldColor { get; set; }

        /// <summary>Whether the dial is being turned right now.</summary>
        public bool IsTurning { get; private set; }

        /// <inheritdoc />
        protected override bool TakesPointer => true;

        /// <summary>Where the needle turns about: the middle of a whole dial, the foot of a half one.</summary>
        Vector2 Pivot
        {
            get
            {
                var room = ContentBounds;
                return _half ? new Vector2(room.Center.X, room.Y + room.Height - Hub) : room.Center;
            }
        }

        /// <summary>How far from the pivot the marks reach.</summary>
        float Reach
        {
            get
            {
                var room = ContentBounds;
                return _half ? Math.Min(room.Width / 2f, room.Height - Hub) : Math.Min(room.Width, room.Height) / 2f;
            }
        }

        /// <inheritdoc />
        protected override void OnPointerDown(Vector2 point, int presses, bool shift, bool ctrl)
        {
            IsTurning = true;
            Follow(point);
        }

        /// <inheritdoc />
        protected override void OnPointerMove(Vector2 point)
        {
            Follow(point);
        }

        /// <inheritdoc />
        protected override void OnPointerUp(Vector2 point)
        {
            IsTurning = false;
        }

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            return _half ? new Vector2(Across, Across / 2f + Hub) : new Vector2(Across, Across);
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            var center = Pivot;
            var reach = Reach;

            // A mark every fifteen degrees, longer on each quarter; a half dial has only the ones it reaches.
            var from = _half ? -6 : 0;
            var to = _half ? 6 : 23;
            for (var i = from; i <= to; i++)
            {
                var quarter = i % 6 == 0;
                Line(g, center, i * 15f, reach - (quarter ? Quarter : Mark), reach, quarter ? QuarterColor : MarkColor, Thin);
            }

            var hand = IsTurning || IsHot ? HeldColor : NeedleColor;
            if (Shows)
                Line(g, center, _degrees, 0f, reach - Quarter - Thin, hand, Needle);

            g.Fill(new RectangleF(center.X - Hub / 2f, center.Y - Hub / 2f, Hub, Hub), hand);
        }

        /// <summary>Turns the needle the short way from where it stands towards <paramref name="point" />, in whole degrees.</summary>
        void Follow(Vector2 point)
        {
            var center = Pivot;
            var dx = point.X - center.X;
            var dy = point.Y - center.Y;
            if (dx * dx + dy * dy < Hub * Hub)
                return;

            var pointing = (float)(Math.Atan2(dx, -dy) * 180.0 / Math.PI);
            var moved = Wrapped(pointing - Wrapped(_degrees) + 180f) - 180f;
            Degrees = (float)Math.Round(_degrees + moved);
            Shows = true;

            var turned = Turned;
            if (turned != null)
                turned();
        }

        /// <summary>An angle brought into one turn, nought to three hundred and sixty.</summary>
        static float Wrapped(float degrees)
        {
            var wrapped = degrees % 360f;
            return wrapped < 0 ? wrapped + 360f : wrapped;
        }

        /// <summary>A stroke out from the middle at an angle, between two distances from it.</summary>
        static void Line(IGraphics g, Vector2 center, float degrees, float from, float to, Color color, float thick)
        {
            var turned = MathHelper.ToRadians(degrees);
            var along = new Vector2((float)Math.Sin(turned), -(float)Math.Cos(turned));
            var middle = center + along * ((from + to) / 2f);
            var length = to - from;

            g.Fill(new RectangleF(middle.X - thick / 2f, middle.Y - length / 2f, thick, length), color, turned);
        }
    }
}
