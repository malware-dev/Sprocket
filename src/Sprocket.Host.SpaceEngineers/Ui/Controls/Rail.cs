namespace Sprocket.Host.SpaceEngineers.Ui.Controls
{
    /// <summary>One axis of scrolling: how much there is, how much is on show, and where along it the view sits.</summary>
    /// <remarks>
    ///     The arithmetic only, with no idea which way it runs or where on the screen it is. A control says
    ///     how long the content is and how much room it has, and asks where the thumb goes and where a
    ///     gesture lands; turning a distance along the axis into a rectangle is the control's own business,
    ///     because only it knows whether this is the one that runs down or the one that runs across.
    ///     <para>
    ///         <b>Nothing here moves anything.</b> Every answer is returned rather than stored, so the control
    ///         assigns through its own property and the invalidation that a move needs happens in the one place
    ///         that knows about it.
    ///     </para>
    /// </remarks>
    public sealed class Rail
    {
        /// <summary>How long the whole of the content is.</summary>
        public float Extent { get; set; }

        /// <summary>How much of it is on show.</summary>
        public float Viewport { get; set; }

        /// <summary>
        ///     How far along the content has been moved.
        /// </summary>
        /// <remarks>
        ///     Not held inside the ends on the way in. A control may be told where to sit before it has been
        ///     laid out and therefore before either of the other two is known, and clamping against an extent
        ///     of nothing would quietly answer zero. <see cref="Clamp" /> is called once the room is known.
        /// </remarks>
        public float Offset { get; set; }

        /// <summary>
        ///     How short the thumb is allowed to get.
        /// </summary>
        /// <remarks>
        ///     A thumb is the same fraction of the track that the viewport is of the content, which on a very
        ///     long list is a few units of nothing. It stops shrinking here and starts lying instead, which is
        ///     the right way round: a thumb nobody can grab says less than one slightly out of place.
        /// </remarks>
        public float MinThumb { get; set; }

        /// <summary>How far it could still be moved. Zero when everything already fits.</summary>
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

        /// <summary>
        ///     The offset it would have if the thumb were dragged <paramref name="distance" />.
        /// </summary>
        /// <remarks>
        ///     The conversion is the whole reason this is not simply the distance: a thumb travels its track
        ///     while the content travels its own length, and the two are the same only on a list one screen
        ///     long.
        /// </remarks>
        public float Dragged(float distance)
        {
            var travel = Travel;
            return travel > 0f ? Clamp(Offset + distance * Max / travel) : Offset;
        }

        /// <summary>
        ///     The offset that would put the middle of the thumb at <paramref name="at" />.
        /// </summary>
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