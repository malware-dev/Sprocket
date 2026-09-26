using Sprocket.Host.SpaceEngineers.Ui.Drawing;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui
{
    /// <summary>A container holding one piece of content, laid out across the room inside its padding unless a kind of control says otherwise.</summary>
    public abstract class ContentControl : Container
    {
        readonly Collection _held;
        Element _content;

        /// <summary>A control holding nothing yet.</summary>
        protected ContentControl()
        {
            _held = Hold();
        }

        /// <summary>What this holds, or null for nothing.</summary>
        public Element Content
        {
            get { return _content; }
            set
            {
                if (_content == value)
                    return;

                if (_content != null)
                    _held.Remove(_content);

                if (value != null)
                    _held.Add(value);

                _content = value;
                InvalidateMeasure();
            }
        }

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            if (_content == null || !_content.IsVisible)
                return Vector2.Zero;

            _content.Measure(g, available);
            return _content.DesiredSize;
        }

        /// <inheritdoc />
        protected override void ArrangeContent(RectangleF bounds)
        {
            if (_content != null && _content.IsVisible)
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
