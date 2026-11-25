using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Model.Expressions;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Features.Expressions
{
    /// <summary>What the expression builder's tray offers: its calculations and kinds of value, and which of them can go somewhere in the value being built.</summary>
    public static class ExpressionOffers
    {
        /// <summary>The calculations, in the order the tray shows them.</summary>
        public static readonly IReadOnlyList<JoinKind> Calculations = new[]
        {
            JoinKind.Add, JoinKind.Subtract, JoinKind.Multiply, JoinKind.Divide, JoinKind.Negate,
            JoinKind.Lowest, JoinKind.Highest, JoinKind.Rounded, JoinKind.WithoutSign
        };

        /// <summary>The comparisons and what joins and turns over flags, in the order the tray shows them, offered only where a condition is built.</summary>
        public static readonly IReadOnlyList<JoinKind> Conditions = new[]
        {
            JoinKind.Is, JoinKind.IsNot, JoinKind.Above, JoinKind.AtLeast, JoinKind.Below, JoinKind.AtMost, JoinKind.Not, JoinKind.And, JoinKind.Or
        };

        /// <summary>The kinds of value typed in, in the order the tray shows them.</summary>
        public static readonly IReadOnlyList<KindClass> Typables = new[] { KindClass.Number, KindClass.Duration, KindClass.Text, KindClass.Flag };

        /// <summary>The values chosen once they are dropped, in the order the tray shows them.</summary>
        public static readonly IReadOnlyList<ValueSource> Sources = new[] { ValueSource.Reading, ValueSource.Setting, ValueSource.State };

        /// <summary>The place of <paramref name="item" /> in <paramref name="list" />, or -1 where it is not in it.</summary>
        public static int PlaceOf<T>(IReadOnlyList<T> list, T item)
        {
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i].Equals(item))
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// The kinds that can stand anywhere in a value of one of <paramref name="wanted" />, what values are read as told by
        /// <paramref name="types" />: those kinds, and every kind a calculation coming to one of them takes in any of its places.
        /// </summary>
        public static KindClass Usable(KindClass wanted, ITypeScope types)
        {
            var usable = wanted;
            var grown = true;
            while (grown)
            {
                grown = false;
                foreach (var kind in Typables)
                {
                    if ((usable & kind) != KindClass.None || !TakenBySome(kind, usable, types))
                        continue;

                    usable |= kind;
                    grown = true;
                }
            }

            return usable;
        }

        /// <summary>Whether the calculation <paramref name="kind" /> can come to one of <paramref name="usable" />.</summary>
        public static bool Offers(JoinKind kind, KindClass usable, ITypeScope types)
        {
            return (DraftKinds.Of(new DraftJoin(kind), types) & usable) != KindClass.None;
        }

        /// <summary>The names of the settings of <paramref name="system" /> whose values are of one of <paramref name="usable" />.</summary>
        public static List<string> Settings(AutomationSystem system, KindClass usable, ITypeScope types)
        {
            var named = new List<string>();
            foreach (var part in system.Parts)
            {
                if (part.IsSetting && (DraftKinds.Of(new DraftValue(new Operand(Literal.Setting(part.Id))), types) & usable) != KindClass.None)
                    named.Add(part.Id);
            }

            return named;
        }

        /// <summary>The names of the states of <paramref name="system" /> whose values are of one of <paramref name="usable" />.</summary>
        public static List<string> States(AutomationSystem system, KindClass usable, ITypeScope types)
        {
            var named = new List<string>();
            foreach (var state in system.States)
            {
                if ((DraftKinds.Of(new DraftValue(new StateOperand(state.Name)), types) & usable) != KindClass.None)
                    named.Add(state.Name);
            }

            return named;
        }

        // Every box the tray can offer: the calculations, then the comparisons and what joins flags.
        static IEnumerable<JoinKind> Joins()
        {
            foreach (var kind in Calculations)
                yield return kind;

            foreach (var kind in Conditions)
                yield return kind;
        }

        // Whether some calculation, a value of kind in one of its places, can come to one of usable.
        static bool TakenBySome(KindClass kind, KindClass usable, ITypeScope types)
        {
            foreach (var calculation in Joins())
            {
                var places = new DraftJoin(calculation).Parts.Count;
                for (var place = 0; place < places; place++)
                {
                    var tried = new DraftJoin(calculation);
                    tried.Set(place, new DraftValue(new Operand(TypedValue.Neutral(kind))));
                    if ((DraftKinds.Of(tried, types) & usable) != KindClass.None)
                        return true;
                }
            }

            return false;
        }
    }
}
