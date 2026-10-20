using Lumel.Drawing;
using Lumel.Text;
using VRageMath;

namespace Lumel.Controls
{
    /// <summary>One page of a <see cref="TabView" />: the header that shows it, with words and a mark after them, and the page itself.</summary>
    /// <remarks>The tab is its header; the page is drawn by the view, below every header, while the tab is the current one.</remarks>
    public class Tab : Element, IPadded, ITextual
    {
        readonly Label _label = new Label();
        readonly Collection _parts;
        Thickness _padding;
        Element _marker;
        bool _current;

        /// <summary>A tab with no words and no page.</summary>
        public Tab()
        {
            _parts = new Collection(this);
            _parts.Add(_label);
        }

        /// <summary>The words on the header.</summary>
        public string Header
        {
            get { return _label.Text; }
            set { _label.Text = value; }
        }

        /// <summary>What the header shows after its words, such as a mark that something on the page wants looking at, or null for nothing.</summary>
        /// <remarks>Its own padding holds it apart from the words.</remarks>
        public Element Marker
        {
            get { return _marker; }
            set
            {
                if (ReferenceEquals(_marker, value))
                    return;

                if (_marker != null)
                    _parts.Remove(_marker);

                _marker = value;
                if (value != null)
                    _parts.Add(value);

                InvalidateMeasure();
            }
        }

        /// <summary>The page the tab shows.</summary>
        public Element Content { get; set; }

        /// <summary>Whether the tab's page is the one shown.</summary>
        /// <remarks>Set by the view the tab is in.</remarks>
        public bool IsCurrent
        {
            get { return _current; }
            set
            {
                if (_current == value)
                    return;

                _current = value;
                StateChanged();
            }
        }

        /// <summary>Which face the words are drawn in.</summary>
        public string Font
        {
            get { return _label.Font; }
            set { _label.Font = value; }
        }

        /// <summary>How tall a line of the words is, in UI units.</summary>
        public float FontSize
        {
            get { return _label.FontSize; }
            set { _label.FontSize = value; }
        }

        /// <summary>The color the words are drawn in.</summary>
        public Color Color
        {
            get { return _label.Color; }
            set { _label.Color = value; }
        }

        /// <summary>Room held clear between the edge and the words.</summary>
        public Thickness Padding
        {
            get { return _padding; }
            set
            {
                if (_padding == value)
                    return;

                _padding = value;
                InvalidateMeasure();
            }
        }

        /// <inheritdoc />
        protected override Thickness Inset => _padding;

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            _label.Measure(g, available);
            var size = _label.DesiredSize;
            if (_marker == null)
                return size;

            // Measured hidden too, so the change when it is shown reaches the header, as a change only rises from what was measured.
            _marker.Measure(g, available);
            return new Vector2(size.X + _marker.DesiredSize.X, MathHelper.Max(size.Y, _marker.DesiredSize.Y));
        }

        /// <summary>Puts the words, and the marker after them, in the middle of the room the header is given.</summary>
        protected override void ArrangeContent(RectangleF bounds)
        {
            var marked = _marker != null && _marker.IsVisible;
            var markerWidth = marked ? _marker.DesiredSize.X : 0f;
            var width = MathHelper.Min(_label.DesiredSize.X + markerWidth, bounds.Width);
            var x = bounds.X + (bounds.Width - width) * 0.5f;
            var words = MathHelper.Max(0f, width - markerWidth);
            _label.Arrange(new RectangleF(x, bounds.Y, words, bounds.Height));
            if (marked)
                _marker.Arrange(new RectangleF(x + words, bounds.Y + (bounds.Height - _marker.DesiredSize.Y) * 0.5f, markerWidth, _marker.DesiredSize.Y));
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            _label.Draw(g);
            if (_marker != null)
                _marker.Draw(g);
        }
    }
}
