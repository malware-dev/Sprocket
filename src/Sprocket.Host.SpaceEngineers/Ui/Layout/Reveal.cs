using Sprocket.Host.SpaceEngineers.Ui.Drawing;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui.Layout
{
    /// <summary>Shows part of one child, from the bottom up, and cuts off the rest.</summary>
    public class Reveal : Element
    {
        Element _child;
        float _openness;

        /// <summary>A reveal with nothing in it, shut.</summary>
        public Reveal()
        {
            // Required: the child is always arranged at full height, and only the clip hides what is out of view.
            ClipsContent = true;
        }

        /// <summary>The one thing in here.</summary>
        public Element Child
        {
            get { return _child; }
            set
            {
                if (_child == value)
                    return;

                if (_child != null)
                    _child.Parent = null;

                _child = value;

                if (_child != null)
                    _child.Parent = this;

                InvalidateMeasure();
            }
        }

        /// <summary>How much of the child is in view, from none of it to all of it.</summary>
        public float Openness
        {
            get { return _openness; }
            set
            {
                var wanted = value < 0f ? 0f : value > 1f ? 1f : value;
                if (_openness == wanted)
                    return;

                _openness = wanted;
                InvalidateMeasure();
            }
        }

        /// <summary>Whether none of the child is in view, which is when it may be taken away.</summary>
        public bool Shut => _openness <= 0f;

        /// <summary>Nothing inside this can be reached until all of it is in view.</summary>
        internal override bool Passes => _openness >= 1f;

        /// <inheritdoc />
        protected internal override int VisualChildCount => _child == null ? 0 : 1;

        /// <inheritdoc />
        protected internal override Element GetVisualChild(int index)
        {
            return _child;
        }

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            if (_child == null || _child.Collapsed)
                return Vector2.Zero;

            _child.Measure(g, available);

            return new Vector2(_child.DesiredSize.X, _child.DesiredSize.Y * _openness);
        }

        /// <inheritdoc />
        protected override void ArrangeContent(RectangleF bounds)
        {
            if (_child == null || _child.Collapsed)
                return;

            // Full height, bottom-aligned to what is on show, with the hidden part above the top edge.
            var height = _child.DesiredSize.Y;

            _child.Arrange(new RectangleF(bounds.X, bounds.Y + bounds.Height - height,
                bounds.Width, height));
        }

        /// <inheritdoc />
        protected override Element UnderContent(Vector2 point)
        {
            return Passes && _child != null ? _child.Under(point) : null;
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            if (Shut || _child == null || _child.Collapsed)
                return;

            _child.Draw(g);
        }
    }
}