using System;
using Lumel.Drawing;
using VRageMath;

namespace Lumel.Controls
{
    /// <summary>The handle something is dragged by, such as a row or a window: six small squares, two across and three down.</summary>
    /// <remarks>
    ///     It takes the pointer itself while it can be carried, and says where it went on the screen: what it is the
    ///     handle of moves with the pointer, so the grip's own layout is no place to measure the pointer from.
    /// </remarks>
    public class Grip : Element
    {
        const int Across = 2;
        const int Down = 3;

        float _square = 3f;
        float _gap = 3f;

        /// <summary>A grip that cannot be carried yet, drawn in white, its squares 3 units on a side and 3 apart.</summary>
        public Grip()
        {
            Color = Color.White;
        }

        /// <summary>Raised with where the pointer is on the screen when the grip is taken hold of.</summary>
        public event Action<Vector2> Taken;

        /// <summary>Raised with where the pointer is on the screen as the grip is carried.</summary>
        public event Action<Vector2> Carried;

        /// <summary>Raised with where the pointer is on the screen when the grip is let go of.</summary>
        public event Action<Vector2> Dropped;

        /// <summary>The color of its squares.</summary>
        public Color Color { get; set; }

        /// <summary>The color of its squares while the pointer rests on it or carries it, or none to keep <see cref="Color" />.</summary>
        /// <remarks>What tells a player it can be taken hold of, where a host cannot change its pointer to say so.</remarks>
        public Color HeldColor { get; set; }

        /// <summary>Whether it can be taken hold of now.</summary>
        public bool CanCarry { get; set; }

        /// <summary>Whether it is being carried right now.</summary>
        public bool IsCarrying { get; private set; }

        /// <summary>How long a side of each square is, in UI units.</summary>
        public float Square
        {
            get { return _square; }
            set
            {
                if (_square == value)
                    return;

                _square = value;
                InvalidateMeasure();
            }
        }

        /// <summary>The room between one square and the next, in UI units.</summary>
        public float Gap
        {
            get { return _gap; }
            set
            {
                if (_gap == value)
                    return;

                _gap = value;
                InvalidateMeasure();
            }
        }

        /// <inheritdoc />
        protected override bool TakesPointer => CanCarry;

        /// <inheritdoc />
        public override void OnPointerDown(Vector2 point, int presses, bool shift, bool ctrl)
        {
            IsCarrying = true;
            var taken = Taken;
            if (taken != null)
                taken(ToScreen(point));
        }

        /// <inheritdoc />
        public override void OnPointerMove(Vector2 point)
        {
            var carried = Carried;
            if (carried != null)
                carried(ToScreen(point));
        }

        /// <inheritdoc />
        public override void OnPointerUp(Vector2 point)
        {
            IsCarrying = false;
            var dropped = Dropped;
            if (dropped != null)
                dropped(ToScreen(point));
        }

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            return new Vector2(Across * _square + (Across - 1) * _gap, Down * _square + (Down - 1) * _gap);
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            var width = Across * _square + (Across - 1) * _gap;
            var height = Down * _square + (Down - 1) * _gap;

            var left = ContentBounds.X + (ContentBounds.Width - width) * 0.5f;
            var top = ContentBounds.Y + (ContentBounds.Height - height) * 0.5f;
            var color = CanCarry && (IsHot || IsCarrying) && HeldColor.A > 0 ? HeldColor : Color;

            for (var x = 0; x < Across; x++)
            for (var y = 0; y < Down; y++)
                g.Fill(new RectangleF(left + x * (_square + _gap), top + y * (_square + _gap), _square, _square), color);
        }
    }
}
