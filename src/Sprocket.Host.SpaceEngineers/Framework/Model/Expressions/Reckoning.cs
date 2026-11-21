using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using System;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Model.Expressions
{
    /// <summary>The arithmetic of expressions: what each operator and function makes of values, and of kinds.</summary>
    /// <remarks>Numbers work out without allocating; only joining text does.</remarks>
    public static class Reckoning
    {
        /// <summary>What <paramref name="left" /> and <paramref name="right" /> come to under <paramref name="op" />, or why they come to nothing.</summary>
        public static ExpressionFailure Apply(Operator op, Literal left, Literal right, out Literal value)
        {
            value = default(Literal);
            if (op == Operator.Add && (left.Kind == ValueKind.Text || right.Kind == ValueKind.Text))
            {
                value = Literal.Of(left.Said + right.Said);
                return ExpressionFailure.None;
            }

            if (Expression.IsNumber(left.Kind) && Expression.IsNumber(right.Kind))
                return Numbers(op, left, right, out value);

            if (left.Kind == ValueKind.Duration && right.Kind == ValueKind.Duration && (op == Operator.Add || op == Operator.Subtract))
                return Sum(left.Length, right.Length, op == Operator.Add ? 1 : -1, out value);

            if (left.Kind == ValueKind.Duration && Expression.IsNumber(right.Kind) && (op == Operator.Multiply || op == Operator.Divide))
                return Scaled(left.Length, right.Number, op == Operator.Divide, out value);

            if (op == Operator.Multiply && Expression.IsNumber(left.Kind) && right.Kind == ValueKind.Duration)
                return Scaled(right.Length, left.Number, false, out value);

            return ExpressionFailure.Mistyped;
        }

        static ExpressionFailure Numbers(Operator op, Literal left, Literal right, out Literal value)
        {
            value = default(Literal);
            if (op == Operator.Divide)
            {
                if (right.Number == 0)
                    return ExpressionFailure.DividedByZero;

                return Finite(left.Number / right.Number, out value);
            }

            if (left.Kind == ValueKind.Integer && right.Kind == ValueKind.Integer)
            {
                long whole;
                if (!Whole(op, left.Integer, right.Integer, out whole))
                    return ExpressionFailure.Overflowed;

                value = Literal.Of(whole);
                return ExpressionFailure.None;
            }

            var x = left.Number;
            var y = right.Number;
            return Finite(op == Operator.Add ? x + y : op == Operator.Subtract ? x - y : x * y, out value);
        }

        /// <summary><paramref name="a" /> and <paramref name="b" /> under <paramref name="op" /> into <paramref name="result" />; false where it is too large for an integer.</summary>
        static bool Whole(Operator op, long a, long b, out long result)
        {
            unchecked
            {
                result = op == Operator.Add ? a + b : op == Operator.Subtract ? a - b : a * b;
                if (op == Operator.Add)
                    return ((a ^ result) & (b ^ result)) >= 0;

                if (op == Operator.Subtract)
                    return ((a ^ b) & (a ^ result)) >= 0;

                if (a == 0 || b == 0)
                    return true;

                return !(a == -1 && b == long.MinValue) && !(b == -1 && a == long.MinValue) && result / b == a;
            }
        }

        /// <summary>The real <paramref name="number" /> as a value; overflowed where it is no number a real holds.</summary>
        static ExpressionFailure Finite(double number, out Literal value)
        {
            value = Literal.Of(number);
            return double.IsNaN(number) || double.IsInfinity(number) ? ExpressionFailure.Overflowed : ExpressionFailure.None;
        }

        static ExpressionFailure Sum(Duration left, Duration right, int sign, out Literal value)
        {
            if (left.InSeconds || right.InSeconds)
                return Seconds(left.Seconds + sign * right.Seconds, out value);

            return Ticks(left.Ticks + sign * (long)right.Ticks, out value);
        }

        static ExpressionFailure Seconds(double seconds, out Literal value)
        {
            value = default(Literal);
            var ticks = seconds * Duration.PerSecond;
            if (double.IsNaN(ticks) || ticks > int.MaxValue || ticks < int.MinValue)
                return ExpressionFailure.Overflowed;

            value = Literal.Of(Duration.OfSeconds(seconds));
            return ExpressionFailure.None;
        }

        static ExpressionFailure Ticks(long ticks, out Literal value)
        {
            value = default(Literal);
            if (ticks > int.MaxValue || ticks < int.MinValue)
                return ExpressionFailure.Overflowed;

            value = Literal.Of(Duration.OfTicks((int)ticks));
            return ExpressionFailure.None;
        }

        static ExpressionFailure Scaled(Duration length, double factor, bool divide, out Literal value)
        {
            value = default(Literal);
            if (divide && factor == 0)
                return ExpressionFailure.DividedByZero;

            var by = divide ? 1 / factor : factor;
            if (length.InSeconds)
                return Seconds(length.Seconds * by, out value);

            var scaled = Math.Round(length.Ticks * by, MidpointRounding.AwayFromZero);
            if (double.IsNaN(scaled) || scaled > int.MaxValue || scaled < int.MinValue)
                return ExpressionFailure.Overflowed;

            return Ticks((long)scaled, out value);
        }

        /// <summary>The kind <paramref name="op" /> makes of operands of the kinds <paramref name="left" /> and <paramref name="right" />; false where it takes none such, and null where an operand's kind is not known.</summary>
        public static bool TypeOf(Operator op, ValueKind? left, ValueKind? right, out ValueKind? kind)
        {
            kind = null;
            if (op == Operator.Add && (left == ValueKind.Text || right == ValueKind.Text))
            {
                kind = ValueKind.Text;
                return true;
            }

            if (left == null || right == null)
                return true;

            if (Expression.IsNumber(left.Value) && Expression.IsNumber(right.Value))
            {
                kind = op == Operator.Divide || left == ValueKind.Real || right == ValueKind.Real ? ValueKind.Real : ValueKind.Integer;
                return true;
            }

            if (left == ValueKind.Duration && right == ValueKind.Duration && (op == Operator.Add || op == Operator.Subtract))
            {
                kind = ValueKind.Duration;
                return true;
            }

            var scaling = op == Operator.Multiply || op == Operator.Divide;
            if (scaling && left == ValueKind.Duration && Expression.IsNumber(right.Value)
                || op == Operator.Multiply && Expression.IsNumber(left.Value) && right == ValueKind.Duration)
            {
                kind = ValueKind.Duration;
                return true;
            }

            return false;
        }

        /// <summary>What <paramref name="function" /> makes of <paramref name="given" />, or why it makes nothing.</summary>
        public static ExpressionFailure Apply(FunctionKind function, IList<Literal> given, out Literal value)
        {
            value = default(Literal);
            var integers = true;
            for (var i = 0; i < given.Count; i++)
            {
                if (!Expression.IsNumber(given[i].Kind))
                    return ExpressionFailure.Mistyped;

                integers &= given[i].Kind == ValueKind.Integer;
            }

            switch (function)
            {
                case FunctionKind.Round:
                    if (integers)
                    {
                        value = given[0];
                        return ExpressionFailure.None;
                    }

                    var rounded = Math.Round(given[0].Number, MidpointRounding.AwayFromZero);
                    if (double.IsNaN(rounded) || rounded >= 9223372036854775808d || rounded < -9223372036854775808d)
                        return ExpressionFailure.Overflowed;

                    value = Literal.Of((long)rounded);
                    return ExpressionFailure.None;

                case FunctionKind.Abs:
                    if (integers && given[0].Integer == long.MinValue)
                        return ExpressionFailure.Overflowed;

                    value = integers ? Literal.Of(Math.Abs(given[0].Integer)) : Literal.Of(Math.Abs(given[0].Number));
                    return ExpressionFailure.None;
            }

            if (given.Count == 0)
                return ExpressionFailure.NoValue;

            var largest = function == FunctionKind.Max;
            var best = given[0];
            for (var i = 1; i < given.Count; i++)
            {
                if (largest ? given[i].Number > best.Number : given[i].Number < best.Number)
                    best = given[i];
            }

            value = integers ? best : Literal.Of(best.Number);
            return ExpressionFailure.None;
        }

        /// <summary>The kind <paramref name="function" /> makes of arguments of the kinds <paramref name="given" />; false where they are not numbers, and null where one's kind is not known.</summary>
        public static bool TypeOf(FunctionKind function, IList<ValueKind?> given, out ValueKind? kind)
        {
            kind = null;
            var integers = true;
            var known = true;
            for (var i = 0; i < given.Count; i++)
            {
                if (given[i] == null)
                {
                    known = false;
                    continue;
                }

                if (!Expression.IsNumber(given[i].Value))
                    return false;

                integers &= given[i] == ValueKind.Integer;
            }

            if (function == FunctionKind.Round)
                kind = ValueKind.Integer;
            else if (known)
                kind = integers ? ValueKind.Integer : ValueKind.Real;

            return true;
        }

        /// <summary>What a reading of the values <paramref name="values" /> comes to under <paramref name="combine" />, or why it comes to nothing.</summary>
        public static ExpressionFailure Combine(Combination combine, List<Literal> values, out Literal value)
        {
            value = default(Literal);
            if (combine == Combination.None)
            {
                if (values.Count > 1)
                    return ExpressionFailure.ManyBlocks;

                if (values.Count == 0)
                    return ExpressionFailure.NoValue;

                value = values[0];
                return ExpressionFailure.None;
            }

            if (values.Count == 0)
            {
                if (combine != Combination.Sum)
                    return ExpressionFailure.NoValue;

                value = Literal.Of(0L);
                return ExpressionFailure.None;
            }

            var integers = true;
            var total = 0d;
            var whole = 0L;
            var overflowed = false;
            for (var i = 0; i < values.Count; i++)
            {
                if (!Expression.IsNumber(values[i].Kind))
                    return ExpressionFailure.Mistyped;

                integers &= values[i].Kind == ValueKind.Integer;
                total += values[i].Number;
                if (values[i].Kind != ValueKind.Integer)
                    continue;

                long added;
                if (Whole(Operator.Add, whole, values[i].Integer, out added))
                    whole = added;
                else
                    overflowed = true;
            }

            switch (combine)
            {
                case Combination.Sum:
                    if (integers)
                    {
                        value = Literal.Of(whole);
                        return overflowed ? ExpressionFailure.Overflowed : ExpressionFailure.None;
                    }

                    return Finite(total, out value);

                case Combination.Average:
                    return Finite(total / values.Count, out value);
            }

            return Apply(combine == Combination.Min ? FunctionKind.Min : FunctionKind.Max, values, out value);
        }
    }
}
