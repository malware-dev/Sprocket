using VRageMath;

namespace Lumel.Images
{
    /// <summary>One side of a picture from the other, keeping the shape it was made in.</summary>
    public static class AspectRatio
    {
        /// <summary>How tall a picture made <paramref name="size" /> big is when <paramref name="width" /> wide; as tall as wide where its size is not known.</summary>
        public static float HeightFor(Vector2 size, float width)
        {
            return size.X > 0f && size.Y > 0f ? width * size.Y / size.X : width;
        }

        /// <summary>How wide a picture made <paramref name="size" /> big is when <paramref name="height" /> tall; as wide as tall where its size is not known.</summary>
        public static float WidthFor(Vector2 size, float height)
        {
            return size.X > 0f && size.Y > 0f ? height * size.X / size.Y : height;
        }
    }
}
