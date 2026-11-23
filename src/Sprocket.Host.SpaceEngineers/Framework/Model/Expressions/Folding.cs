using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Model.Expressions
{
    /// <summary>
    /// An expression worked out as far as it can be while it comes to the same: every part of only fixed values becomes the value it
    /// comes to, a number times 0, times 1, plus 0, minus 0 or divided by 1 drops what makes no difference, and a flag joined
    /// to off by and, or to on by or, comes to that fixed flag, and joined to on by and, or to off by or, to itself.
    /// </summary>
    public static class Folding
    {
        /// <summary>
        /// <paramref name="expression" /> folded, what its settings, states and readings hold told by <paramref name="types" />;
        /// a part that fails, such as a division by zero, is left as written so it fails where it runs.
        /// </summary>
        public static Expression Fold(Expression expression, ITypeScope types)
        {
            var arithmetic = expression as Arithmetic;
            if (arithmetic != null)
                return Folded(new Arithmetic(arithmetic.Op, Fold(arithmetic.Left, types), Fold(arithmetic.Right, types)), types);

            var negation = expression as Negation;
            if (negation != null)
                return Worked(new Negation(Fold(negation.Operand, types)));

            var comparing = expression as Comparing;
            if (comparing != null)
                return Worked(new Comparing(comparing.Relation, Fold(comparing.Left, types), Fold(comparing.Right, types)));

            var inversion = expression as Inversion;
            if (inversion != null)
                return Worked(new Inversion(Fold(inversion.Operand, types)));

            var logical = expression as Logical;
            if (logical != null)
                return Joined(new Logical(logical.Joins, Fold(logical.Left, types), Fold(logical.Right, types)), types);

            var reading = expression as BlockReading;
            if (reading != null && reading.Checks)
                return new BlockReading(reading.Key, reading.Combine, reading.Arguments, reading.Targets, reading.Relation, Fold(reading.Compared, types));

            var function = expression as Function;
            if (function == null)
                return expression;

            var arguments = new List<Expression>();
            foreach (var argument in function.Arguments)
                arguments.Add(Fold(argument, types));

            return Worked(new Function(function.Kind, arguments));
        }

        /// <summary>Whether <paramref name="expression" /> is a fixed value alone, which no setting, state or reading decides.</summary>
        public static bool IsFixed(Expression expression)
        {
            var operand = expression as Operand;
            return operand != null && !operand.Value.IsSetting && !operand.Value.IsExpression;
        }

        // A flag joined to a fixed flag: off by and, or on by or, decides alone; on by and, or off by or, leaves the other flag.
        static Expression Joined(Logical logical, ITypeScope types)
        {
            var worked = Worked(logical);
            if (worked != logical)
                return worked;

            var deciding = logical.Joins == Connective.Or;
            foreach (var pair in new[] { new[] { logical.Left, logical.Right }, new[] { logical.Right, logical.Left } })
            {
                bool flag;
                if (!IsFlag(pair[0], out flag) || Kind(pair[1], types) != ValueKind.Flag)
                    continue;

                return flag == deciding ? pair[0] : pair[1];
            }

            return logical;
        }

        // Whether the expression is a fixed flag, into flag.
        static bool IsFlag(Expression expression, out bool flag)
        {
            flag = false;
            if (!IsFixed(expression) || ((Operand)expression).Value.Kind != ValueKind.Flag)
                return false;

            flag = ((Operand)expression).Value.Flag;
            return true;
        }

        static Expression Folded(Arithmetic arithmetic, ITypeScope types)
        {
            var worked = Worked(arithmetic);
            if (worked != arithmetic)
                return worked;

            var left = arithmetic.Left;
            var right = arithmetic.Right;
            switch (arithmetic.Op)
            {
                case Operator.Multiply:
                    if (Is(right, 0) && IsNumber(left, types))
                        return Zero(left, right, types);
                    if (Is(left, 0) && IsNumber(right, types))
                        return Zero(right, left, types);
                    if (Is(right, 1) && Keeps(left, right, types))
                        return left;
                    if (Is(left, 1) && Keeps(right, left, types))
                        return right;
                    break;
                case Operator.Add:
                    if (Is(right, 0) && Keeps(left, right, types))
                        return left;
                    if (Is(left, 0) && Keeps(right, left, types))
                        return right;
                    break;
                case Operator.Subtract:
                    if (Is(right, 0) && Keeps(left, right, types))
                        return left;
                    break;
                case Operator.Divide:
                    // Division always gives a real, so only a real divided by 1 is what it was.
                    if (Is(right, 1) && Kind(left, types) == ValueKind.Real)
                        return left;
                    break;
            }

            return arithmetic;
        }

        // The value an expression of only fixed values comes to, or the expression where it has other parts or fails.
        static Expression Worked(Expression expression)
        {
            var parts = new List<Expression>();
            expression.Walk(parts.Add);
            for (var i = 1; i < parts.Count; i++)
            {
                if (!(parts[i] is Operand) && !(parts[i] is Arithmetic) && !(parts[i] is Negation) && !(parts[i] is Function)
                    && !(parts[i] is Comparing) && !(parts[i] is Logical) && !(parts[i] is Inversion))
                    return expression;

                var operand = parts[i] as Operand;
                if (operand != null && !IsFixed(operand))
                    return expression;
            }

            Literal value;
            return expression.Evaluate(null, out value) == ExpressionFailure.None ? new Operand(value) : expression;
        }

        // Whether the expression is the fixed number given.
        static bool Is(Expression expression, long number)
        {
            if (!IsFixed(expression))
                return false;

            var value = ((Operand)expression).Value;
            return value.Kind == ValueKind.Integer ? value.Integer == number : value.Kind == ValueKind.Real && value.Real == number;
        }

        static bool IsNumber(Expression expression, ITypeScope types)
        {
            var kind = Kind(expression, types);
            return kind == ValueKind.Integer || kind == ValueKind.Real;
        }

        // Whether dropping the fixed number leaves the same kind of number: an integer 0 or 1 with any number, a real one with a real.
        static bool Keeps(Expression kept, Expression dropped, ITypeScope types)
        {
            var kind = Kind(kept, types);
            if (kind == ValueKind.Real)
                return true;

            return kind == ValueKind.Integer && ((Operand)dropped).Value.Kind == ValueKind.Integer;
        }

        // Zero, of the kind the product would have been.
        static Expression Zero(Expression other, Expression zero, ITypeScope types)
        {
            var real = Kind(other, types) == ValueKind.Real || ((Operand)zero).Value.Kind == ValueKind.Real;
            return new Operand(real ? Literal.Of(0.0) : Literal.Of(0L));
        }

        static ValueKind? Kind(Expression expression, ITypeScope types)
        {
            ValueKind? kind;
            return expression.TypeOf(types, out kind) ? kind : null;
        }
    }
}
