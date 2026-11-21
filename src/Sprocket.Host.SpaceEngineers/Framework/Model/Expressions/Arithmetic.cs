using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using System;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Model.Expressions
{
    /// <summary>Two operands joined by an operator.</summary>
    public sealed class Arithmetic : Expression
    {
        /// <summary><paramref name="left" /> and <paramref name="right" /> joined by <paramref name="op" />.</summary>
        public Arithmetic(Operator op, Expression left, Expression right)
        {
            Op = op;
            Left = left;
            Right = right;
        }

        /// <summary>What is done to the operands.</summary>
        public Operator Op { get; }

        /// <summary>The operand before the operator.</summary>
        public Expression Left { get; }

        /// <summary>The operand after the operator.</summary>
        public Expression Right { get; }

        /// <inheritdoc />
        public override int Precedence => Op == Operator.Add || Op == Operator.Subtract ? 1 : 2;

        /// <inheritdoc />
        public override ExpressionFailure Evaluate(IExpressionScope scope, out Literal value)
        {
            Literal left;
            var failure = Left.Evaluate(scope, out left);
            if (failure == ExpressionFailure.None)
            {
                Literal right;
                failure = Right.Evaluate(scope, out right);
                if (failure == ExpressionFailure.None)
                    return Reckoning.Apply(Op, left, right, out value);
            }

            value = default(Literal);
            return failure;
        }

        /// <inheritdoc />
        public override bool TypeOf(ITypeScope scope, out ValueKind? kind)
        {
            kind = null;
            ValueKind? left, right;
            return Left.TypeOf(scope, out left) && Right.TypeOf(scope, out right) && Reckoning.TypeOf(Op, left, right, out kind);
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
            return new Arithmetic(Op, Left.Renamed(was, now, state), Right.Renamed(was, now, state));
        }

        /// <inheritdoc />
        public override Expression Copied()
        {
            return new Arithmetic(Op, Left.Copied(), Right.Copied());
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
