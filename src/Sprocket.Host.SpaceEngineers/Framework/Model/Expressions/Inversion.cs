using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using System;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Model.Expressions
{
    /// <summary>The other value of a flag: <c>not cycling</c>.</summary>
    public sealed class Inversion : Expression
    {
        /// <summary>The other value of <paramref name="operand" />.</summary>
        public Inversion(Expression operand)
        {
            Operand = operand;
        }

        /// <summary>The flag turned over.</summary>
        public Expression Operand { get; }

        /// <inheritdoc />
        public override int Precedence => Negation.UnaryPrecedence;

        /// <inheritdoc />
        public override ExpressionFailure Evaluate(IExpressionScope scope, out Literal value)
        {
            var failure = Operand.Evaluate(scope, out value);
            if (failure != ExpressionFailure.None)
                return failure;

            if (value.Kind != ValueKind.Flag)
                return ExpressionFailure.Mistyped;

            value = Literal.Of(!value.Flag);
            return ExpressionFailure.None;
        }

        /// <inheritdoc />
        public override bool TypeOf(ITypeScope scope, out ValueKind? kind)
        {
            var fits = Operand.TypeOf(scope, out kind) && (kind == null || kind == ValueKind.Flag);
            kind = ValueKind.Flag;
            return fits;
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
            return new Inversion(Operand.Renamed(was, now, state));
        }

        /// <inheritdoc />
        public override Expression Copied()
        {
            return new Inversion(Operand.Copied());
        }

        /// <inheritdoc />
        public override void Walk(Action<Expression> visit)
        {
            visit(this);
            Operand.Walk(visit);
        }
    }
}
