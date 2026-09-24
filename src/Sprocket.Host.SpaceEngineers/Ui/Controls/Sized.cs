using Sprocket.Host.SpaceEngineers.Ui.Drawing;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui.Controls
{
    /// <summary>Something held to a width, whatever it would rather be.</summary>
    public class Sized : Element
    {
        readonly ElementCollection _children;

        Element _content;
        float _width;

        /// <summary>Makes a holder <paramref name="width" /> wide, empty until given <see cref="Content" />.</summary>
        public Sized(float width)
        {
            _children = new ElementCollection(this);
            Width = width;
        }

        /// <summary>The width the content is held to.</summary>
        public float Width
        {
            get { return _width; }
            set
            {
                if (_width == value)
                    return;

                _width = value;
                InvalidateMeasure();
            }
        }

        /// <summary>The one thing held.</summary>
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

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            if (_content == null)
                return new Vector2(Width, 0f);

            _content.Measure(g, new Vector2(Width, available.Y));
            return new Vector2(Width, _content.DesiredSize.Y);
        }

        /// <inheritdoc />
        protected override void ArrangeContent(RectangleF bounds)
        {
            _content?.Arrange(new RectangleF(bounds.X, bounds.Y, Width, bounds.Height));
        }

        /// <inheritdoc />
        protected override Element UnderContent(Vector2 point)
        {
            return _content?.Under(point);
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            _content?.Draw(g);
        }
    }
}