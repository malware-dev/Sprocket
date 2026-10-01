
namespace Lumel.Viewing
{
    /// <summary>One axis of scrolling: how much there is, how much is on show, and where along it the view sits.</summary>
    /// <remarks>
    ///     Answers are returned, never stored: the control assigns them through its own property, which is
    ///     where the invalidation a move needs happens.
    /// </remarks>
    public sealed class Rail
    {
        /// <summary>How long the whole of the content is.</summary>
        public float Extent { get; set; }

        /// <summary>How much of it is on show.</summary>
        public float Viewport { get; set; }

        /// <summary>How far along the content has been moved.</summary>
        /// <remarks>
        ///     Not clamped when set, because it may be set before layout knows the other two; the control
        ///     calls <see cref="Clamp" /> once they are known.
        /// </remarks>
        public float Offset { get; set; }

        /// <summary>How short the thumb is allowed to get.</summary>
        public float MinThumb { get; set; }

        /// <summary>The largest offset there is, zero when everything fits.</summary>
        public float Max
        {
            get
            {
                var max = Extent - Viewport;
                return max > 0f ? max : 0f;
            }
        }

        /// <summary>Whether there is anything out of sight.</summary>
        public bool CanMove => Max > 0f;

        /// <summary>How far along the content is, from nothing to all of it.</summary>
        public float Fraction
        {
            get
            {
                var max = Max;
                return max > 0f ? Offset / max : 0f;
            }
        }

        /// <summary>How long the thumb is.</summary>
        public float ThumbLength
        {
            get
            {
                if (Extent <= 0f || Viewport <= 0f)
                    return Viewport;

                var length = Viewport * Viewport / Extent;

                if (length < MinThumb)
                    length = MinThumb;
                if (length > Viewport)
                    length = Viewport;

                return length;
            }
        }

        /// <summary>How much room the thumb has to move in.</summary>
        public float Travel
        {
            get
            {
                var travel = Viewport - ThumbLength;
                return travel > 0f ? travel : 0f;
            }
        }

        /// <summary>Where the thumb starts, measured from the start of the track.</summary>
        public float ThumbStart => Travel * Fraction;

        /// <summary>The offset it would have after moving by <paramref name="amount" />.</summary>
        public float Moved(float amount)
        {
            return Clamp(Offset + amount);
        }

        /// <summary>The offset it would have if the thumb were dragged <paramref name="distance" />.</summary>
        public float Dragged(float distance)
        {
            var travel = Travel;
            return travel > 0f ? Clamp(Offset + distance * Max / travel) : Offset;
        }

        /// <summary>The offset that would put the middle of the thumb at <paramref name="at" />.</summary>
        /// <param name="at">Where along the axis, in the same units as <paramref name="start" />.</param>
        /// <param name="start">Where the track begins.</param>
        public float ThumbedTo(float at, float start)
        {
            var travel = Travel;
            if (travel <= 0f)
                return Offset;

            return Clamp((at - start - ThumbLength * 0.5f) * Max / travel);
        }

        /// <summary>Whatever is left of a side once a bar this thick has had its share.</summary>
        public static float Room(float whole, float bar)
        {
            var room = whole - bar;
            return room > 0f ? room : 0f;
        }

        /// <summary>Holds a value between nothing and <see cref="Max" />.</summary>
        public float Clamp(float value)
        {
            var max = Max;

            if (value > max)
                value = max;
            if (value < 0f)
                value = 0f;

            return value;
        }
    }
}