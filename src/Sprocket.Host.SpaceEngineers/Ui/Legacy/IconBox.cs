using Sprocket.Host.SpaceEngineers.Look;
using Sprocket.Host.SpaceEngineers.Ui.Drawing;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui.Legacy
{
    /// <summary>A picture of a thing, held to a square.</summary>
    public class IconBox : Box
    {
        float _size;

        /// <summary>Makes a square <paramref name="size" /> units on a side.</summary>
        public IconBox(float size)
        {
            Size = size;
        }

        /// <summary>How long each side of the square is.</summary>
        public float Size
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

        /// <summary>The name of the kind of thing to draw, which <see cref="Icons" /> turns into a material.</summary>
        public string Art { get; set; }

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            return new Vector2(Size, Size);
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            g.DrawSprite(Icons.MaterialFor(Art), Bounds, Color.White, 0f);
        }
    }
}