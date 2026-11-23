using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using System;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Model.Expressions
{
    /// <summary>Two flags joined by <c>and</c> or <c>or</c>, which gives a flag.</summary>
    public sealed class Logical : Expression
    {
        /// <summary><paramref name="left" /> and <paramref name="right" /> joined as <paramref name="joins" /> says.</summary>
        public Logical(Connective joins, Expression left, Expression right)
        {
            Joins = joins;
            Left = left;
            Right = right;
        }

        /// <summary>How the two flags are joined.</summary>
        public Connective Joins { get; }

        /// <summary>The flag before the word.</summary>
        public Expression Left { get; }

        /// <summary>The flag after the word, worked out only where the one before does not decide.</summary>
        public Expression Right { get; }

        /// <inheritdoc />
        public override int Precedence => Joins == Connective.And ? 2 : 1;

        /// <inheritdoc />
        public override ExpressionFailure Evaluate(IExpressionScope scope, out Literal value)
        {
            var failure = Left.Evaluate(scope, out value);
            if (failure != ExpressionFailure.None)
                return failure;

            if (value.Kind != ValueKind.Flag)
                return ExpressionFailure.Mistyped;

            // The side after the word is not worked out where the one before already decides, so its readings are not taken.
            if (value.Flag == (Joins == Connective.Or))
                return ExpressionFailure.None;

            failure = Right.Evaluate(scope, out value);
            if (failure != ExpressionFailure.None)
                return failure;

            return value.Kind == ValueKind.Flag ? ExpressionFailure.None : ExpressionFailure.Mistyped;
        }

        /// <inheritdoc />
        public override bool TypeOf(ITypeScope scope, out ValueKind? kind)
        {
            kind = ValueKind.Flag;
            ValueKind? left, right;
            return Left.TypeOf(scope, out left) && Right.TypeOf(scope, out right)
                && (left == null || left == ValueKind.Flag) && (right == null || right == ValueKind.Flag);
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
            return new Logical(Joins, Left.Renamed(was, now, state), Right.Renamed(was, now, state));
        }

        /// <inheritdoc />
        public override Expression Copied()
        {
            return new Logical(Joins, Left.Copied(), Right.Copied());
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
