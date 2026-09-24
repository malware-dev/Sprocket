using VRageMath;
using Sprocket.Host.SpaceEngineers.Ui.Drawing;

namespace Sprocket.Host.SpaceEngineers.Ui.Controls
{
    /// <summary>A rectangle of one colour, shown so that the colour itself is the answer.</summary>
    /// <remarks>
    /// The colour goes in <see cref="Element.Background" />, which the element already draws, so there
    /// is nothing in the content but the size it holds. It is a type rather than a bare box because a
    /// swatch is a thing on the screen that somebody presses, and a box with a background set is not.
    /// </remarks>
    public class Swatch : Element
    {
        Vector2 _size;

        public Swatch(float width, float height)
        {
            _size = new Vector2(width, height);
        }

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

        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            return _size;
        }

        protected override void DrawContent(IGraphics g)
        {
        }
    }
}
