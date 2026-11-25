using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model.Expressions;

namespace Sprocket.Host.SpaceEngineers.Features.Expressions
{
    /// <summary>The box of the expression builder each of the language's calculations is shown as.</summary>
    public static class JoinKinds
    {
        /// <summary>The box the operator <paramref name="op" /> is shown as.</summary>
        public static JoinKind Of(Operator op)
        {
            switch (op)
            {
                case Operator.Add: return JoinKind.Add;
                case Operator.Subtract: return JoinKind.Subtract;
                case Operator.Multiply: return JoinKind.Multiply;
                default: return JoinKind.Divide;
            }
        }

        /// <summary>The box the function <paramref name="function" /> is shown as.</summary>
        public static JoinKind Of(FunctionKind function)
        {
            switch (function)
            {
                case FunctionKind.Min: return JoinKind.Lowest;
                case FunctionKind.Max: return JoinKind.Highest;
                case FunctionKind.Round: return JoinKind.Rounded;
                default: return JoinKind.WithoutSign;
            }
        }

        /// <summary>The box the comparison <paramref name="relation" /> is shown as.</summary>
        public static JoinKind Of(Relation relation)
        {
            switch (relation)
            {
                case Relation.IsNot: return JoinKind.IsNot;
                case Relation.Above: return JoinKind.Above;
                case Relation.AtLeast: return JoinKind.AtLeast;
                case Relation.Below: return JoinKind.Below;
                case Relation.AtMost: return JoinKind.AtMost;
                default: return JoinKind.Is;
            }
        }

        /// <summary>The box <paramref name="joins" />, and or or, is shown as.</summary>
        public static JoinKind Of(Connective joins) => joins == Connective.And ? JoinKind.And : JoinKind.Or;

        /// <summary>
        /// Whether <paramref name="join" />, a box written between its two parts, is put in brackets where it stands, as a formula
        /// writes it: where the box it stands in binds tighter, or as tightly with it on the right of a subtraction or a division,
        /// beside another comparison, or under a sign or a not.
        /// </summary>
        public static bool Bracketed(DraftJoin join)
        {
            var own = Binding(join.Kind);
            var parent = join.Parent;
            if (own == 0 || parent == null)
                return false;

            if (parent.Kind == JoinKind.Negate || parent.Kind == JoinKind.Not)
                return true;

            var outer = Binding(parent.Kind);
            if (outer == 0 || own > outer)
                return false;

            if (own < outer || Compares(join.Kind))
                return true;

            return parent.IndexOf(join) == 1 && (parent.Kind == JoinKind.Subtract || parent.Kind == JoinKind.Divide);
        }

        // How tightly a box written between its two parts binds them, as the language reads them, loosest first; 0 for a box
        // that is not written between two parts.
        static int Binding(JoinKind kind)
        {
            switch (kind)
            {
                case JoinKind.Or: return 1;
                case JoinKind.And: return 2;
                case JoinKind.Add:
                case JoinKind.Subtract: return 4;
                case JoinKind.Multiply:
                case JoinKind.Divide: return 5;
            }

            return Compares(kind) ? 3 : 0;
        }

        /// <summary>Whether a box of <paramref name="kind" /> compares two values, which gives a flag.</summary>
        public static bool Compares(JoinKind kind) => kind >= JoinKind.Is && kind <= JoinKind.AtMost;

        /// <summary>Whether a box of <paramref name="kind" /> compares two values by their order, as only numbers and timespans have.</summary>
        public static bool Orders(JoinKind kind) => kind >= JoinKind.Above && kind <= JoinKind.AtMost;

        /// <summary>Whether a box of <paramref name="kind" /> takes flags and gives a flag: not, and, or or.</summary>
        public static bool JoinsFlags(JoinKind kind) => kind == JoinKind.Not || kind == JoinKind.And || kind == JoinKind.Or;

        /// <summary>The comparison a box of <paramref name="kind" />, one that <see cref="Compares" />, makes.</summary>
        public static Relation RelationOf(JoinKind kind)
        {
            switch (kind)
            {
                case JoinKind.IsNot: return Relation.IsNot;
                case JoinKind.Above: return Relation.Above;
                case JoinKind.AtLeast: return Relation.AtLeast;
                case JoinKind.Below: return Relation.Below;
                case JoinKind.AtMost: return Relation.AtMost;
                default: return Relation.Is;
            }
        }
    }
}
