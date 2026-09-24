using System;

namespace Sprocket.Host.SpaceEngineers.Ui
{
    /// <summary>Room on each side of something.</summary>
    /// <remarks>
    ///     <b>Equality is written out rather than inherited, and that is not tidiness.</b> The one a struct
    ///     gets for free takes an <c>object</c>, so every comparison boxes - and because these are floats,
    ///     the runtime cannot take its bitwise shortcut and walks the fields by reflection instead. The
    ///     layout asks this question to decide whether a change is worth re-measuring for, so the inherited
    ///     version costs more than the work it exists to avoid.
    /// </remarks>
    public struct Thickness : IEquatable<Thickness>
    {
        public readonly float Left;
        public readonly float Top;
        public readonly float Right;
        public readonly float Bottom;

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

        public float Horizontal => Left + Right;

        public float Vertical => Top + Bottom;

        public bool IsEmpty => Left == 0f && Top == 0f && Right == 0f && Bottom == 0f;

        public static bool operator ==(Thickness a, Thickness b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Thickness a, Thickness b)
        {
            return !a.Equals(b);
        }

        public bool Equals(Thickness other)
        {
            return Left == other.Left && Top == other.Top && Right == other.Right && Bottom == other.Bottom;
        }

        public override bool Equals(object other)
        {
            return other is Thickness && Equals((Thickness)other);
        }

        /// <summary>
        ///     Never a key, and here because overriding <see cref="Equals(object)" /> without it is a
        ///     warning and a trap.
        /// </summary>
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