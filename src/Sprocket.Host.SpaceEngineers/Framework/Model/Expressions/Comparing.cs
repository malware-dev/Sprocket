using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using System;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Model.Expressions
{
    /// <summary>Two values compared, which gives a flag: <c>count &gt;= 5</c>.</summary>
    public sealed class Comparing : Expression
    {
        /// <summary>How tightly a comparison binds its operands, looser than arithmetic and tighter than <c>and</c>.</summary>
        public const int ComparisonPrecedence = 3;

        /// <summary><paramref name="left" /> compared with <paramref name="right" /> as <paramref name="relation" /> says.</summary>
        public Comparing(Relation relation, Expression left, Expression right)
        {
            Relation = relation;
            Left = left;
            Right = right;
        }

        /// <summary>How the two are compared.</summary>
        public Relation Relation { get; }

        /// <summary>The value before the comparison.</summary>
        public Expression Left { get; }

        /// <summary>The value after the comparison.</summary>
        public Expression Right { get; }

        /// <inheritdoc />
        public override int Precedence => ComparisonPrecedence;

        /// <inheritdoc />
        public override ExpressionFailure Evaluate(IExpressionScope scope, out Literal value)
        {
            value = default(Literal);
            Literal left, right;
            var failure = Left.Evaluate(scope, out left);
            if (failure != ExpressionFailure.None)
                return failure;

            failure = Right.Evaluate(scope, out right);
            if (failure != ExpressionFailure.None)
                return failure;

            if (!Suits(Relation, left.Kind, right.Kind))
                return ExpressionFailure.Mistyped;

            value = Literal.Of(Relations.Holds(left, Relation, right));
            return ExpressionFailure.None;
        }

        /// <inheritdoc />
        public override bool TypeOf(ITypeScope scope, out ValueKind? kind)
        {
            kind = ValueKind.Flag;
            ValueKind? left, right;
            if (!Left.TypeOf(scope, out left) || !Right.TypeOf(scope, out right))
                return false;

            return left == null || right == null || Suits(Relation, left.Value, right.Value);
        }

        /// <summary>
        /// Whether <paramref name="relation" /> compares a value of <paramref name="left" /> with one of <paramref name="right" />:
        /// sameness between two of one kind, numbers counting as one kind, and an ordering between numbers or between durations.
        /// </summary>
        public static bool Suits(Relation relation, ValueKind left, ValueKind right)
        {
            var numbers = IsNumber(left) && IsNumber(right);
            if (relation == Relation.Is || relation == Relation.IsNot)
                return numbers || left == right;

            return numbers || left == ValueKind.Duration && right == ValueKind.Duration;
        }

        /// <inheritdoc />
        public override void NameLists(List<IList<string>> into)
        {
            Left.NameLists(into);
            Right.NameLists(into);
        }

        /// <inheritdoc />
        public override void Settings(List<string> into)
        {
            Left.Settings(into);
            Right.Settings(into);
        }

        /// <inheritdoc />
        public override Expression Renamed(string was, string now, bool state)
        {
            return new Comparing(Relation, Left.Renamed(was, now, state), Right.Renamed(was, now, state));
        }

        /// <inheritdoc />
        public override Expression Copied()
        {
            return new Comparing(Relation, Left.Copied(), Right.Copied());
        }

        /// <inheritdoc />
        public override void Walk(Action<Expression> visit)
        {
            visit(this);
            Left.Walk(visit);
            Right.Walk(visit);
        }
    }
}
