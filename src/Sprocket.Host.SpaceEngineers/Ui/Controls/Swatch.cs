using Sprocket.Host.SpaceEngineers.Ui.Drawing;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui.Controls
{
    /// <summary>A rectangle of one colour, shown so that the colour itself is the answer.</summary>
    /// <remarks>The colour is its <see cref="Element.Background" />.</remarks>
    public class Swatch : Element
    {
        Vector2 _size;

        /// <summary>Makes a swatch <paramref name="width" /> by <paramref name="height" />.</summary>
        public Swatch(float width, float height)
        {
            _size = new Vector2(width, height);
        }

        /// <summary>How big the swatch is.</summary>
        public Vector2 Size
        {
            get { return _size; }
            set
            {
                if (_size == value)
                    return;

                _size = value;
                InvalidateMeasure();
            }
        }

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            return _size;
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
        }
    }
}