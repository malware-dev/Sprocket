namespace Reactor.Host.SpaceEngineers.World
{
    /// <summary>
    /// Which relations mean anything against which readings, and what they answer.
    /// </summary>
    /// <remarks>
    /// The table is core's because it is a property of the type system rather than of any game:
    /// below is meaningless on a colour in Space Engineers and in anything else. A host declares what
    /// sort of reading a property gives; core says what may be asked of it.
    /// <para>
    /// <see cref="Suits" /> saying no is an answer rather than an absence. It is what lets a target
    /// explain why a relation is not on offer instead of silently showing a shorter list.
    /// </para>
    /// <para>
    /// This is the comparison half. Arithmetic is a second axis and does not exist yet; when it does,
    /// which operators apply to which kinds has to answer for both.
    /// </para>
    /// </remarks>
    public static class Relations
    {
        /// <summary>Whether a relation means anything against a reading of this sort.</summary>
        /// <remarks>
        /// Everything can be equal to something. Only a number can be above it - a colour has no
        /// ordering, and asking for one is a question worth refusing rather than answering.
        /// </remarks>
        public static bool Suits(Relation relation, ValueKind kind)
        {
            if (relation == Relation.Is || relation == Relation.IsNot)
                return true;

            return kind == ValueKind.Integer || kind == ValueKind.Real;
        }

        /// <summary>
        /// Measures a reading against an operand of the same kind.
        /// </summary>
        /// <remarks>
        /// Two whole numbers are compared as whole numbers. Anything else numeric falls back to
        /// doubles, which is the best available - but two integers must never go that way, because
        /// past 2^53 a double cannot tell two ids apart and would call them equal.
        /// </remarks>
        public static bool Holds(
            Values values, ValueKind kind, EntityValue reading, Relation relation, EntityValue operand)
        {
            if (kind == ValueKind.Text)
            {
                return Same(
                    string.Equals(values.AsText(kind, reading), values.AsText(kind, operand)), relation);
            }

            // Only equality means anything on a colour, and falling through to a numeric compare would
            // answer an ordering rather than refusing it.
            if (kind == ValueKind.Color)
                return Same(reading.Bits == operand.Bits, relation);

            long here, there;

            if (values.TryInteger(kind, reading, out here) && values.TryInteger(kind, operand, out there))
            {
                switch (relation)
                {
                    case Relation.Is: return here == there;
                    case Relation.IsNot: return here != there;
                    case Relation.Above: return here > there;
                    case Relation.AtLeast: return here >= there;
                    case Relation.Below: return here < there;
                    case Relation.AtMost: return here <= there;
                }

                return false;
            }

            double left = values.AsNumber(kind, reading);
            double right = values.AsNumber(kind, operand);

            switch (relation)
            {
                case Relation.Is: return left == right;
                case Relation.IsNot: return left != right;
                case Relation.Above: return left > right;
                case Relation.AtLeast: return left >= right;
                case Relation.Below: return left < right;
                case Relation.AtMost: return left <= right;
            }

            return false;
        }

        /// <summary>
        /// The answer to a relation that can only tell sameness apart from difference.
        /// </summary>
        /// <remarks>
        /// An ordering asked of something with no ordering answers false rather than throwing.
        /// <see cref="Suits" /> is where that combination is refused; by the time a reading is being
        /// measured, refusing again would only turn a question nobody should have asked into a crash
        /// in the middle of somebody's airlock.
        /// </remarks>
        static bool Same(bool same, Relation relation)
        {
            if (relation == Relation.Is)
                return same;

            return relation == Relation.IsNot && !same;
        }
    }
}
