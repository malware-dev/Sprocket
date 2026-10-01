using Lumel.Drawing;
using VRage.Utils;
using VRageMath;

namespace Lumel.Images
{
    /// <summary>A picture, drawn from a material across the whole of its bounds inside its padding.</summary>
    /// <remarks>A picture has no size of its own to ask for, so it takes the size its placement gives it, and none without one.</remarks>
    public class Image : Element, IPadded
    {
        Thickness _padding;

        /// <summary>An image drawing nothing yet, in its own colors.</summary>
        public Image()
        {
            Tint = Color.White;
        }

        /// <summary>What is drawn, or <see cref="MyStringId.NullOrEmpty" /> for nothing.</summary>
        public MyStringId Material { get; set; }

        /// <summary>The color the picture is multiplied by; white draws it as it is.</summary>
        public Color Tint { get; set; }

        /// <summary>Room held clear between the edge and the picture, which is drawn across within.</summary>
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
            return Vector2.Zero;
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            if (Material != MyStringId.NullOrEmpty)
                g.DrawSprite(Material, ContentBounds, Tint, 0f);
        }
    }
}
