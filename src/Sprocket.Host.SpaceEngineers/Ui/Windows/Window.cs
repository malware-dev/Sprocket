using Sprocket.Host.SpaceEngineers.Ui.Drawing;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui.Windows
{
    /// <summary>Something that floats over the screen in a window layer, holding one element and placed by the layer rather than by a container.</summary>
    /// <remarks>
    ///     A modal window takes all input for as long as it is open: nothing beneath it answers, and the layer
    ///     paints <see cref="Backdrop" /> over everything beneath it first.
    /// </remarks>
    public class Window : Element
    {
        readonly ElementCollection _children;
        Element _content;

        /// <summary>An empty window.</summary>
        public Window()
        {
            _children = new ElementCollection(this);
        }

        /// <summary>What the window holds.</summary>
        public Element Content
        {
            get { return _content; }
            set
            {
                if (_content == value)
                    return;

                if (_content != null)
                    _children.Remove(_content);

                if (value != null)
                    _children.Add(value);

                _content = value;
            }
        }

        /// <summary>Whether nothing beneath the window takes input while it is open.</summary>
        public bool IsModal { get; set; }

        /// <summary>What a modal window paints over everything beneath it.</summary>
        public Color Backdrop { get; set; }

        /// <summary>Where the window stands in <paramref name="area" /> at <paramref name="size" />: in the middle, unless a kind of window says otherwise.</summary>
        protected internal virtual RectangleF Place(RectangleF area, Vector2 size)
        {
            return new RectangleF(area.X + (area.Width - size.X) * 0.5f, area.Y + (area.Height - size.Y) * 0.5f, size.X,
                size.Y);
        }

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            if (_content == null)
                return Vector2.Zero;

            _content.Measure(g, available);
            return _content.DesiredSize;
        }

        /// <inheritdoc />
        protected override void ArrangeContent(RectangleF bounds)
        {
            if (_content != null)
                _content.Arrange(bounds);
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            if (_content != null)
                _content.Draw(g);
        }

        /// <inheritdoc />
        protected override Element UnderContent(Vector2 point)
        {
            return _content != null ? _content.Under(point) : null;
        }
    }
}
