namespace Reactor.World
{
    public static class Relations
    {
        /// <summary>Whether a relation means anything against a reading of this sort.</summary>
        public static bool Suits(Relation relation, ValueKind kind)
        {
            if (relation == Relation.Is || relation == Relation.IsNot)
                return true;

            return kind == ValueKind.Number;
        }

        /// <summary>
        /// Measures a reading against an operand.
        /// </summary>
        /// <remarks>
        /// Text compares as text and everything else as a number, which is what makes a flag
        /// fall out for free: it reads one or zero, so "is true" is a relation like any
        /// other rather than a case of its own.
        /// </remarks>
        public static bool Holds(EntityValue reading, Relation relation, EntityValue operand)
        {
            if (reading.Kind == ValueKind.Text || operand.Kind == ValueKind.Text)
            {
                var same = string.Equals(reading.Text, operand.Text);
                return relation == Relation.IsNot ? !same : same;
            }

            var left = reading.Number;
            var right = operand.Number;

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
    }
}
