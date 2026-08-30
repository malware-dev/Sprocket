
namespace Phosphor.Ui
{
    /// <summary>Room on each side of something.</summary>
    public struct Thickness
    {
        public float Left;
        public float Top;
        public float Right;
        public float Bottom;

        public Thickness(float all)
        {
            Left = all;
            Top = all;
            Right = all;
            Bottom = all;
        }

        public Thickness(float horizontal, float vertical)
        {
            Left = horizontal;
            Top = vertical;
            Right = horizontal;
            Bottom = vertical;
        }

        public Thickness(float left, float top, float right, float bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public float Horizontal
        {
            get { return Left + Right; }
        }

        public float Vertical
        {
            get { return Top + Bottom; }
        }

        public bool IsEmpty
        {
            get { return Left == 0f && Top == 0f && Right == 0f && Bottom == 0f; }
        }
    }
}
