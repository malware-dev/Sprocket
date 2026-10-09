using Lumel.Drawing;
using VRageMath;

namespace Lumel.Images
{
    /// <summary>A picture: how big it was made, and how it is drawn.</summary>
    /// <remarks>Never changes once made, so one can be kept and shared for as long as the picture exists.</remarks>
    public interface IImageSource
    {
        /// <summary>How big the picture was made, which gives its shape; zero where that is not known, and it is then taken as square.</summary>
        Vector2 Size { get; }

        /// <summary>Draws the picture across <paramref name="destination" />, multiplied by <paramref name="tint" /> and turned <paramref name="rotation" /> radians clockwise about its center; white draws it as it is.</summary>
        void Draw(IGraphics g, RectangleF destination, Color tint, float rotation);
    }
}
