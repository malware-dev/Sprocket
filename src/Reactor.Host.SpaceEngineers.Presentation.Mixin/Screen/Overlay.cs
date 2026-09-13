using VRageMath;
using Reactor.Host.SpaceEngineers.Presentation.Graphics;
using Reactor.Host.SpaceEngineers.Presentation.Ui;

namespace Reactor.Host.SpaceEngineers.Presentation.Screen
{
    /// <summary>
    /// Something shown over the screen, hanging off whatever was pressed to open it.
    /// </summary>
    /// <remarks>
    /// Its own thing rather than three fields on the screen, because everything it does is about
    /// itself: where it sits, whether it is up, and whether a press landed on it. The screen only ever
    /// needs those three answers.
    /// <para>
    /// It is not laid out with the rest of the screen. It is measured and placed at drawing time
    /// against the anchor's final position, which is the only moment that position is known.
    /// </para>
    /// </remarks>
    public sealed class Overlay
    {
        private Element _content;
        private Element _anchor;
        private RectangleF _bounds;

        /// <summary>Whether anything is up.</summary>
        public bool IsUp
        {
            get { return _content != null; }
        }

        /// <summary>Where it was last drawn. Empty while nothing is up.</summary>
        public RectangleF Bounds
        {
            get { return _bounds; }
        }

        /// <summary>Whether this particular thing is the one showing.</summary>
        public bool Is(Element content)
        {
            return _content == content;
        }

        public void Show(Element content, Element anchor)
        {
            _content = content;
            _anchor = anchor;
        }

        public void Hide()
        {
            _content = null;
            _anchor = null;
            _bounds = new RectangleF();
        }

        /// <summary>Whether a point is on it. False while nothing is up.</summary>
        public bool Covers(Vector2 point)
        {
            return IsUp
                && point.X >= _bounds.X && point.X <= _bounds.X + _bounds.Width
                && point.Y >= _bounds.Y && point.Y <= _bounds.Y + _bounds.Height;
        }

        /// <summary>
        /// Places it under its anchor and draws it, kept inside <paramref name="bounds" />.
        /// </summary>
        /// <remarks>
        /// Pushed back inside on both axes rather than allowed to hang off the screen, because a list
        /// opened near the bottom edge is exactly the list somebody most needs to read all of.
        /// </remarks>
        public void Draw(IGraphics g, RectangleF bounds, float width)
        {
            if (_content == null || _anchor == null)
            {
                _bounds = new RectangleF();
                return;
            }

            _content.Measure(g, new Vector2(width, bounds.Height));

            float height = _content.DesiredSize.Y;
            float wide = _content.DesiredSize.X;
            float x = _anchor.Bounds.X;
            float y = _anchor.Bounds.Y + _anchor.Bounds.Height + 2f;

            if (y + height > bounds.Y + bounds.Height)
                y = bounds.Y + bounds.Height - height;
            if (y < bounds.Y)
                y = bounds.Y;

            if (x + wide > bounds.X + bounds.Width)
                x = bounds.X + bounds.Width - wide;
            if (x < bounds.X)
                x = bounds.X;

            _bounds = new RectangleF(x, y, wide, height);
            _content.Arrange(_bounds);
            _content.Draw(g);
        }
    }
}
