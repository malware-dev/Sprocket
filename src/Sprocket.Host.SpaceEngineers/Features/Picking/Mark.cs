using Sprocket.Host.SpaceEngineers.Look;
using Sprocket.Host.SpaceEngineers.Ui.Drawing;
using Sprocket.Host.SpaceEngineers.Ui.Images;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Features.Picking
{
    /// <summary>The mark at the head of something to choose: a ring for one of several, and a box for any number of them.</summary>
    /// <remarks>
    /// Round where exactly one holds at a time, which is what the shape says before anybody reads the words beside it, and
    /// square where several may be collected, with a tick in it for each one taken.
    /// </remarks>
    public class Mark : Glyph
    {
        const float Side = 16f;

        bool _many;
        bool _checked;
        bool _pressed;

        /// <summary>A ring, not chosen.</summary>
        public Mark()
        {
            Show();
        }

        /// <summary>Whether this is one of any number that may be chosen together, drawn as a box rather than a ring.</summary>
        public bool Many
        {
            get { return _many; }
            set
            {
                _many = value;
                Show();
            }
        }

        /// <summary>Whether what it heads has been chosen.</summary>
        public bool IsChecked
        {
            get { return _checked; }
            set
            {
                _checked = value;
                Show();
            }
        }

        /// <summary>Whether the mark is pressed by itself, which lights it under the pointer.</summary>
        public bool IsPressed
        {
            get { return _pressed; }
            set
            {
                _pressed = value;
                Show();
            }
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            if (_many)
            {
                var room = ContentBounds;
                Outline(g, new RectangleF(room.X + (room.Width - Side) * 0.5f, room.Y + (room.Height - Side) * 0.5f, Side, Side),
                    Tint, 1f);
            }

            base.DrawContent(g);
        }

        void Show()
        {
            Symbol = _many ? _checked ? Symbols.Check : "" : _checked ? Symbols.Disc : Symbols.Ring;
            Style = _checked
                ? _pressed ? Styles.MarkOnPressed : Styles.MarkOn
                : _pressed ? Styles.MarkPressed : Styles.Mark;
        }
    }
}
