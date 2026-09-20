using VRageMath;
using Sprocket.Host.SpaceEngineers.Drawing;
using Sprocket.Host.SpaceEngineers.Layout;

namespace Sprocket.Host.SpaceEngineers.Screen
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

        /// <summary>Whether anything is up.</summary>
        public bool IsUp
        {
            get { return _content != null; }
        }

        /// <summary>What is hanging there, for the screen to walk while it owns the screen.</summary>
        public Element Content
        {
            get { return _content; }
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
        }

        /// <summary>Whether a point is on it. False while nothing is up.</summary>
        public bool Covers(Vector2 point)
        {
            return IsUp && _content.Holds(point);
        }

        /// <summary>
        /// Places it under its anchor and draws it, kept inside <paramref name="bounds" />.
        /// </summary>
        /// <remarks>
        /// Pushed back inside on both axes rather than allowed to hang off the screen, because a list
        /// opened near the bottom edge is exactly the list somebody most needs to read all of.
        /// <para>
        /// <b>It is offered the screen and takes what it needs.</b> A dialog is given a width,
        /// because a form of empty boxes sized to its contents would open as a row of slivers. A
        /// menu is the opposite case: it already holds its words, so its width is a fact about it
        /// rather than a decision. Given a dialog's width it took one, which is how a list of five
        /// short block kinds came to be as wide as a form.
        /// </para>
        /// </remarks>
        public void Draw(IGraphics g, RectangleF bounds)
        {
            if (_content == null || _anchor == null)
                return;

            _content.Measure(g, new Vector2(bounds.Width, bounds.Height));

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

            _content.Arrange(new RectangleF(x, y, wide, height));
            _content.Draw(g);
        }
    }
}
