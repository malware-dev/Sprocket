using Sprocket.Host.SpaceEngineers.Ui.Drawing;
using VRage.Utils;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui.Controls
{
    /// <summary>A picture, drawn from a material across the whole of its bounds.</summary>
    /// <remarks>A picture has no size of its own to ask for, so it takes the size its placement gives it, and none without one.</remarks>
    public class Image : Element
    {
        /// <summary>An image drawing nothing yet, in its own colors.</summary>
        public Image()
        {
            Tint = Color.White;
        }

        /// <summary>What is drawn, or <see cref="MyStringId.NullOrEmpty" /> for nothing.</summary>
        public MyStringId Material { get; set; }

        /// <summary>The color the picture is multiplied by; white draws it as it is.</summary>
        public Color Tint { get; set; }

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            return Vector2.Zero;
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            if (Material != MyStringId.NullOrEmpty)
                g.DrawSprite(Material, Bounds, Tint, 0f);
        }
    }
}
