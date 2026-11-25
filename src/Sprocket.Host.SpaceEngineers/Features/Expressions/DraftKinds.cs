using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model.Expressions;

namespace Sprocket.Host.SpaceEngineers.Features.Expressions
{
    /// <summary>
    /// What a part of an expression being built can still come to, its empty places counted as any kind, as the
    /// language's rules for each operator and function say (docs/Language.md §4.4).
    /// </summary>
    public static class DraftKinds
    {
        /// <summary>
        /// The kinds <paramref name="piece" /> can come to, what its values are read as told by <paramref name="types" />;
        /// none where a box in it can never work, whatever fills its empty places.
        /// </summary>
        public static KindClass Of(DraftPiece piece, ITypeScope types)
        {
            var value = piece as DraftValue;
            if (value != null)
                return OfValue(value.Atom, types);

            var join = piece as DraftJoin;
            if (join == null)
                return KindClass.Any;

            var first = Of(join.Parts[0], types);
            var second = join.Parts.Count > 1 ? Of(join.Parts[1], types) : KindClass.None;
            if (first == KindClass.None || (join.Parts.Count > 1 && second == KindClass.None))
                return KindClass.None;

            if (JoinKinds.Compares(join.Kind))
                return Compared(join.Kind, first, second);

            switch (join.Kind)
            {
                case JoinKind.Not:
                    return first & KindClass.Flag;
                case JoinKind.And:
                case JoinKind.Or:
                    return Both(first, second, KindClass.Flag);
                case JoinKind.Add:
                    return Both(first, second, KindClass.Number) | Both(first, second, KindClass.Duration) | Joined(first, second);
                case JoinKind.Subtract:
                    return Both(first, second, KindClass.Number) | Both(first, second, KindClass.Duration);
                case JoinKind.Multiply:
                    return Both(first, second, KindClass.Number) | Scaled(first, second) | Scaled(second, first);
                case JoinKind.Divide:
                    return Both(first, second, KindClass.Number) | Scaled(first, second);
                case JoinKind.Lowest:
                case JoinKind.Highest:
                    return AllNumbers(join, types) ? KindClass.Number : KindClass.None;
                default:
                    return first & KindClass.Number;
            }
        }

        /// <summary>The kinds the language names <paramref name="wanted" /> stands for, or any kind where it is null.</summary>
        public static KindClass Wanted(string wanted)
        {
            switch (wanted)
            {
                case null: return KindClass.Any;
                case "integer":
                case "real": return KindClass.Number;
                case "text": return KindClass.Text;
                case "duration": return KindClass.Duration;
                case "flag": return KindClass.Flag;
            }

            return KindClass.Choice;
        }

        /// <summary>Whether <paramref name="root" />, as a whole expression, can still come to one of <paramref name="wanted" />.</summary>
        public static bool Suits(DraftPiece root, KindClass wanted, ITypeScope types)
        {
            return (Of(root, types) & wanted) != KindClass.None;
        }

        static KindClass OfValue(Expression atom, ITypeScope types)
        {
            ValueKind? kind;
            if (!atom.TypeOf(types, out kind))
                return KindClass.None;

            if (kind == null)
                return KindClass.Any;

            switch (kind.Value)
            {
                case ValueKind.Integer:
                case ValueKind.Real: return KindClass.Number;
                case ValueKind.Text: return KindClass.Text;
                case ValueKind.Duration: return KindClass.Duration;
                case ValueKind.Flag: return KindClass.Flag;
                case ValueKind.Name: return KindClass.Choice;
            }

            return KindClass.None;
        }

        // A flag where the two sides can be of one kind, numbers counting as one, and for an ordering where both can be numbers or both timespans.
        static KindClass Compared(JoinKind kind, KindClass first, KindClass second)
        {
            var shared = JoinKinds.Orders(kind) ? (first & second & (KindClass.Number | KindClass.Duration)) : first & second;
            return shared != KindClass.None ? KindClass.Flag : KindClass.None;
        }

        // The kind where both sides can be it.
        static KindClass Both(KindClass first, KindClass second, KindClass kind)
        {
            return (first & kind) != 0 && (second & kind) != 0 ? kind : KindClass.None;
        }

        // A text where either side can be one, the other side written as text.
        static KindClass Joined(KindClass first, KindClass second)
        {
            return (first & KindClass.Text) != 0 || (second & KindClass.Text) != 0 ? KindClass.Text : KindClass.None;
        }

        // A length of time where the first can be one and the second a number.
        static KindClass Scaled(KindClass length, KindClass by)
        {
            return (length & KindClass.Duration) != 0 && (by & KindClass.Number) != 0 ? KindClass.Duration : KindClass.None;
        }

        static bool AllNumbers(DraftJoin join, ITypeScope types)
        {
            foreach (var part in join.Parts)
            {
                if ((Of(part, types) & KindClass.Number) == 0)
                    return false;
            }

            return true;
        }
    }
}
