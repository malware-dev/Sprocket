using Sprocket.Host.SpaceEngineers.Framework.Model;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Members
{
    /// <summary>
    /// Which relations mean anything against which readings, and what they answer.
    /// </summary>
    public static class Relations
    {
        /// <summary>Whether a relation means anything against a reading of this sort.</summary>
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
        /// Two integers are compared as integers, never as doubles: past 2^53 a double cannot tell two ids
        /// apart.
        /// </remarks>
        public static bool Holds(
            Values values, ValueKind kind, EntityValue reading, Relation relation, EntityValue operand)
        {
            if (kind == ValueKind.Text)
            {
                return Same(
                    string.Equals(values.AsText(kind, reading), values.AsText(kind, operand)), relation);
            }

            // Only sameness means anything on a color.
            if (kind == ValueKind.Color)
                return Same(reading.Bits == operand.Bits, relation);

            long here, there;

            if (values.TryInteger(kind, reading, out here) && values.TryInteger(kind, operand, out there))
                return Integer(here, relation, there);

            return Real(values.AsNumber(kind, reading), relation, values.AsNumber(kind, operand));
        }

        /// <summary>
        /// Measures a reading against an operand, each carrying its own kind.
        /// </summary>
        /// <remarks>
        /// Two integers are compared as integers and any other two numbers as reals. Two lengths of time are compared by their ticks. A flag, a color and text answer
        /// only sameness, and two of different kinds are never the same.
        /// </remarks>
        public static bool Holds(Literal reading, Relation relation, Literal operand)
        {
            if (reading.Kind == ValueKind.Integer && operand.Kind == ValueKind.Integer)
                return Integer(reading.Integer, relation, operand.Integer);

            var numbers = (reading.Kind == ValueKind.Integer || reading.Kind == ValueKind.Real)
                          && (operand.Kind == ValueKind.Integer || operand.Kind == ValueKind.Real);
            if (numbers)
                return Real(reading.Number, relation, operand.Number);

            if (reading.Kind == ValueKind.Duration && operand.Kind == ValueKind.Duration)
                return Integer(reading.Length.Ticks, relation, operand.Length.Ticks);

            if (reading.Kind != operand.Kind)
                return Same(false, relation);

            if (reading.Kind == ValueKind.Text || reading.Kind == ValueKind.Name)
                return Same(string.Equals(reading.Text, operand.Text), relation);

            return Same(reading.Value.Bits == operand.Value.Bits, relation);
        }

        static bool Integer(long here, Relation relation, long there)
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

        static bool Real(double left, Relation relation, double right)
        {
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
        /// <remarks>An ordering asked of something with no ordering answers false, and never throws.</remarks>
        static bool Same(bool same, Relation relation)
        {
            if (relation == Relation.Is)
                return same;

            return relation == Relation.IsNot && !same;
        }
    }
}
