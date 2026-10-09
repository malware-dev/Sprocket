using Lumel.Drawing;
using VRage.Utils;
using VRageMath;

namespace Lumel.Images
{
    /// <summary>A picture the game draws from a material.</summary>
    public sealed class Sprite : IImageSource
    {
        /// <summary>The picture <paramref name="material" /> draws, made <paramref name="size" /> big, or of a size not known where that is zero.</summary>
        public Sprite(MyStringId material, Vector2 size)
        {
            Material = material;
            Size = size;
        }

        /// <summary>The material the picture is drawn from.</summary>
        public MyStringId Material { get; }

        /// <inheritdoc />
        public Vector2 Size { get; }

        /// <inheritdoc />
        public void Draw(IGraphics g, RectangleF destination, Color tint, float rotation)
        {
            g.DrawSprite(Material, destination, tint, rotation);
        }
    }
}
