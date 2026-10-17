using System;

namespace Lumel
{
    /// <summary>Room on each side of something.</summary>
    public struct Thickness : IEquatable<Thickness>
    {
        /// <summary>Room on the left.</summary>
        public readonly float Left;

        /// <summary>Room along the top.</summary>
        public readonly float Top;

        /// <summary>Room on the right.</summary>
        public readonly float Right;

        /// <summary>Room along the bottom.</summary>
        public readonly float Bottom;

        /// <summary>Room of <paramref name="all" /> on every side.</summary>
        public Thickness(float all)
        {
            Left = all;
            Top = all;
            Right = all;
            Bottom = all;
        }

        /// <summary><paramref name="horizontal" /> on the left and right, <paramref name="vertical" /> on the top and bottom.</summary>
        public Thickness(float horizontal, float vertical)
        {
            Left = horizontal;
            Top = vertical;
            Right = horizontal;
            Bottom = vertical;
        }

        /// <summary>Room given side by side.</summary>
        public Thickness(float left, float top, float right, float bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        /// <summary>The room on the left and right together.</summary>
        public float Horizontal => Left + Right;

        /// <summary>The room on the top and bottom together.</summary>
        public float Vertical => Top + Bottom;

        /// <summary>Whether there is no room on any side.</summary>
        public bool IsEmpty => Left == 0f && Top == 0f && Right == 0f && Bottom == 0f;

        /// <summary>Whether <paramref name="a" /> and <paramref name="b" /> hold the same room on every side.</summary>
        public static bool operator ==(Thickness a, Thickness b)
        {
            return a.Equals(b);
        }

        /// <summary>Whether <paramref name="a" /> and <paramref name="b" /> differ on any side.</summary>
        public static bool operator !=(Thickness a, Thickness b)
        {
            return !a.Equals(b);
        }

        /// <summary>Whether <paramref name="other" /> holds the same room on every side.</summary>
        public bool Equals(Thickness other)
        {
            return Left == other.Left && Top == other.Top && Right == other.Right && Bottom == other.Bottom;
        }

        /// <inheritdoc />
        public override bool Equals(object other)
        {
            return other is Thickness && Equals((Thickness)other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hash = Left.GetHashCode();
            hash = (hash * 397) ^ Top.GetHashCode();
            hash = (hash * 397) ^ Right.GetHashCode();
            hash = (hash * 397) ^ Bottom.GetHashCode();
            return hash;
        }
    }
}