using Sprocket.Host.SpaceEngineers.Ui.Drawing;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui.Viewing
{
    /// <summary>Shows part of its content, from the bottom up, and cuts off the rest.</summary>
    public class Reveal : ContentControl
    {
        float _openness;

        /// <summary>How much of the content is in view, from none of it to all of it.</summary>
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

        /// <summary>Whether none of the content is in view, which is when it may be taken away.</summary>
        public bool Shut => _openness <= 0f;

        /// <summary>Nothing inside this can be reached until all of it is in view.</summary>
        protected override bool Passes => _openness >= 1f;

        /// <inheritdoc />
        /// <remarks>Always: what it holds is shown only as far as it has opened.</remarks>
        protected override bool Clips => true;

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            if (Content == null || !Content.IsVisible)
                return Vector2.Zero;

            Content.Measure(g, available);

            return new Vector2(Content.DesiredSize.X, Content.DesiredSize.Y * _openness);
        }

        /// <inheritdoc />
        protected override void ArrangeContent(RectangleF bounds)
        {
            if (Content == null || !Content.IsVisible)
                return;

            // Full height, bottom-aligned to what is on show, with the hidden part above the top edge.
            var height = Content.DesiredSize.Y;

            Content.Arrange(new RectangleF(bounds.X, bounds.Y + bounds.Height - height,
                bounds.Width, height));
        }

        /// <inheritdoc />
        protected override Element UnderContent(Vector2 point)
        {
            return Passes && Content != null ? Content.Under(point) : null;
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            if (Shut || Content == null || !Content.IsVisible)
                return;

            Content.Draw(g);
        }
    }
}