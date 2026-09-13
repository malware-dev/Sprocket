
namespace VRageMath
{
    public struct Rectangle
    {
        public int X;
        public int Y;
        public int Width;
        public int Height;

        public Rectangle(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public int Left => X;

        public int Top => Y;

        public int Right => X + Width;

        public int Bottom => Y + Height;

        public override string ToString()
        {
            return "{X:" + X + " Y:" + Y + " W:" + Width + " H:" + Height + "}";
        }
    }
}
