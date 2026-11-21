using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using System;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Model.Expressions
{
    /// <summary>The negative of a number.</summary>
    public sealed class Negation : Expression
    {
        /// <summary>The negative of <paramref name="operand" />.</summary>
        public Negation(Expression operand)
        {
            Operand = operand;
        }

        /// <summary>What is negated.</summary>
        public Expression Operand { get; }

        /// <inheritdoc />
        public override int Precedence => 3;

        /// <inheritdoc />
        public override ExpressionFailure Evaluate(IExpressionScope scope, out Literal value)
        {
            var failure = Operand.Evaluate(scope, out value);
            if (failure != ExpressionFailure.None)
                return failure;

            if (value.Kind == ValueKind.Integer)
            {
                if (value.Integer == long.MinValue)
                    return ExpressionFailure.Overflowed;

                value = Literal.Of(-value.Integer);
            }
            else if (value.Kind == ValueKind.Real)
                value = Literal.Of(-value.Real);
            else
                return ExpressionFailure.Mistyped;

            return ExpressionFailure.None;
        }

        /// <inheritdoc />
        public override bool TypeOf(ITypeScope scope, out ValueKind? kind)
        {
            return Operand.TypeOf(scope, out kind) && (kind == null || IsNumber(kind.Value));
        }

        /// <inheritdoc />
        public override void NameLists(List<IList<string>> into)
        {
            Operand.NameLists(into);
        }

        /// <inheritdoc />
        public override void Settings(List<string> into)
        {
            Operand.Settings(into);
        }

        /// <inheritdoc />
        public override Expression Renamed(string was, string now, bool state)
        {
            return new Negation(Operand.Renamed(was, now, state));
        }

        /// <inheritdoc />
        public override Expression Copied()
        {
            return new Negation(Operand.Copied());
        }

        /// <inheritdoc />
        public override void Walk(Action<Expression> visit)
        {
            visit(this);
            Operand.Walk(visit);
        }
    }
}
