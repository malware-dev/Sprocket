using System;
using Lumel.Drawing;
using VRageMath;

namespace Lumel.Viewing
{
    /// <summary>Scales its content to fill its room as nearly as it can without spilling out, keeping its shape.</summary>
    /// <remarks>
    ///     The content is laid out without wrapping, and in a room unbounded either way it is drawn at its own size. The
    ///     scale is the content's <see cref="Element.RenderTransform" />, which this sets.
    /// </remarks>
    public class Viewbox : ContentControl
    {
        // How much wider than its widest part the content is laid out again, so that part keeps its line whatever rounding
        // its width went through.
        const float Slack = 0.5f;

        bool _grows = true;
        bool _shrinks = true;
        float _scale = 1f;

        /// <summary>Whether content smaller than the room is scaled up to fill it.</summary>
        public bool Grows
        {
            get { return _grows; }
            set
            {
                if (_grows == value)
                    return;

                _grows = value;
                InvalidateMeasure();
            }
        }

        /// <summary>Whether content larger than the room is scaled down to fit it.</summary>
        public bool Shrinks
        {
            get { return _shrinks; }
            set
            {
                if (_shrinks == value)
                    return;

                _shrinks = value;
                InvalidateMeasure();
            }
        }

        /// <summary>How much the content was scaled when last measured: 1 where it is drawn at its own size.</summary>
        public float Scale => _scale;

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            _scale = 1f;
            var content = Content;
            if (content == null || !content.IsVisible)
                return Vector2.Zero;

            if (!_grows && !_shrinks || available.X >= Unbounded || available.Y >= Unbounded)
            {
                content.Measure(g, available);
                return content.DesiredSize;
            }

            content.Measure(g, new Vector2(Unbounded, Unbounded));
            var size = content.DesiredSize;
            if (size.X <= 0f || size.Y <= 0f)
                return size;

            var scale = Math.Min(available.X / size.X, available.Y / size.Y);
            if (scale > 1f && !_grows || scale < 1f && !_shrinks)
                scale = 1f;

            content.Measure(g, new Vector2(Math.Max(available.X / scale, size.X + Slack), Unbounded));

            _scale = scale;
            return new Vector2(Math.Min(content.DesiredSize.X * scale, available.X), size.Y * scale);
        }

        /// <inheritdoc />
        protected override void ArrangeContent(RectangleF bounds)
        {
            var content = Content;
            if (content == null || !content.IsVisible)
                return;

            var wide = bounds.Width / _scale;
            var tall = bounds.Height / _scale;

            float x;
            float across;
            float y;
            float down;
            Span(content, true, bounds.X, wide, float.NaN, content.DesiredSize.X, out x, out across);
            Span(content, false, bounds.Y, tall, float.NaN, content.DesiredSize.Y, out y, out down);
            content.Arrange(new RectangleF(x, y, across, down));

            // A render transform scales about the content's own corner, so it is moved by as much as its distance from
            // this one's corner grows, which scales the whole room about this corner.
            var grown = _scale - 1f;
            content.RenderTransform = _scale == 1f
                ? RenderTransform.Identity
                : new RenderTransform(new Vector2((x - bounds.X) * grown, (y - bounds.Y) * grown), _scale);
        }
    }
}
