using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model.Expressions;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Model
{
    /// <summary>
    /// The choices of a logic system's declared types an expression names: a choice compared with a state or a setting of the
    /// type, or one standing where the place takes the type, as the value a set step gives a state of it.
    /// </summary>
    public static class ExpressionChoices
    {
        /// <summary>
        /// Whether <paramref name="tree" />, standing where the type <paramref name="wanted" /> is taken, or empty, names the choice
        /// <paramref name="choice" /> of the type <paramref name="type" /> of <paramref name="system" />.
        /// </summary>
        public static bool Names(AutomationSystem system, Expression tree, string wanted, string type, string choice)
        {
            var named = false;
            Visit(system, tree, wanted, type, choice, operand =>
            {
                named = true;
                return operand;
            });
            return named;
        }

        /// <summary>
        /// <paramref name="tree" />, standing where the type <paramref name="wanted" /> is taken, or empty, with the choice
        /// <paramref name="was" /> of the type <paramref name="type" /> of <paramref name="system" /> named <paramref name="now" />;
        /// the same tree where it names none.
        /// </summary>
        public static Expression Renamed(AutomationSystem system, Expression tree, string wanted, string type, string was, string now)
        {
            return Visit(system, tree, wanted, type, was, operand => new Operand(Literal.Name(now)));
        }

        // The tree with each operand naming the choice of the type, where its place takes that type, given to found; a part with
        // nothing changed in it is the same part.
        static Expression Visit(AutomationSystem system, Expression tree, string wanted, string type, string choice, System.Func<Operand, Expression> found)
        {
            var operand = tree as Operand;
            if (operand != null)
                return wanted == type && operand.Value.Kind == ValueKind.Name && operand.Value.Text == choice ? found(operand) : tree;

            var comparing = tree as Comparing;
            if (comparing != null)
            {
                // A choice takes the type of what it is compared with.
                var left = Visit(system, comparing.Left, TypeOf(system, comparing.Right), type, choice, found);
                var right = Visit(system, comparing.Right, TypeOf(system, comparing.Left), type, choice, found);
                return ReferenceEquals(left, comparing.Left) && ReferenceEquals(right, comparing.Right) ? tree : new Comparing(comparing.Relation, left, right);
            }

            var logical = tree as Logical;
            if (logical != null)
            {
                var left = Visit(system, logical.Left, "", type, choice, found);
                var right = Visit(system, logical.Right, "", type, choice, found);
                return ReferenceEquals(left, logical.Left) && ReferenceEquals(right, logical.Right) ? tree : new Logical(logical.Joins, left, right);
            }

            var inversion = tree as Inversion;
            if (inversion != null)
            {
                var inner = Visit(system, inversion.Operand, "", type, choice, found);
                return ReferenceEquals(inner, inversion.Operand) ? tree : new Inversion(inner);
            }

            var arithmetic = tree as Arithmetic;
            if (arithmetic != null)
            {
                var left = Visit(system, arithmetic.Left, "", type, choice, found);
                var right = Visit(system, arithmetic.Right, "", type, choice, found);
                return ReferenceEquals(left, arithmetic.Left) && ReferenceEquals(right, arithmetic.Right) ? tree : new Arithmetic(arithmetic.Op, left, right);
            }

            var negation = tree as Negation;
            if (negation != null)
            {
                var inner = Visit(system, negation.Operand, "", type, choice, found);
                return ReferenceEquals(inner, negation.Operand) ? tree : new Negation(inner);
            }

            var function = tree as Function;
            if (function != null)
            {
                var changed = false;
                var arguments = new List<Expression>(function.Arguments.Count);
                foreach (var argument in function.Arguments)
                {
                    var now = Visit(system, argument, "", type, choice, found);
                    changed |= !ReferenceEquals(now, argument);
                    arguments.Add(now);
                }

                return changed ? new Function(function.Kind, arguments) : tree;
            }

            // A reading compares and is given values of the host's types, never of a logic system's own.
            return tree;
        }

        // The declared type of the logic system a state or setting operand holds, or empty for anything else.
        static string TypeOf(AutomationSystem system, Expression tree)
        {
            var state = tree as StateOperand;
            if (state != null)
                return system.FindState(state.Name)?.Type ?? "";

            var operand = tree as Operand;
            if (operand == null || !operand.Value.IsSetting)
                return "";

            return system.FindPart(operand.Value.Text)?.Setting ?? "";
        }
    }
}
