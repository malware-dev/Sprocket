using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using System;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Model.Expressions
{
    /// <summary>A literal, or a setting standing in for one, as an operand.</summary>
    public sealed class Operand : Expression
    {
        /// <summary>An operand holding <paramref name="value" />, which is no expression itself.</summary>
        public Operand(Literal value)
        {
            Value = value;
        }

        /// <summary>The literal or setting.</summary>
        public Literal Value { get; }

        /// <inheritdoc />
        public override int Precedence => AtomPrecedence;

        /// <summary>The expression <paramref name="value" /> is, or an operand holding it where it is a plain literal.</summary>
        public static Expression Of(Literal value)
        {
            return value.IsExpression ? value.Tree : new Operand(value);
        }

        /// <inheritdoc />
        public override ExpressionFailure Evaluate(IExpressionScope scope, out Literal value)
        {
            value = Value;
            if (Value.IsExpression)
                return Value.Tree.Evaluate(scope, out value);

            if (Value.IsSetting)
                return scope.Setting(Value.Text, out value) ? ExpressionFailure.None : ExpressionFailure.Unanswered;

            return Value.Kind == ValueKind.Slots ? ExpressionFailure.Mistyped : ExpressionFailure.None;
        }

        /// <inheritdoc />
        public override bool TypeOf(ITypeScope scope, out ValueKind? kind)
        {
            kind = Value.IsSetting ? scope.Setting(Value.Text) : Value.Kind;
            return Value.Kind != ValueKind.Slots;
        }

        /// <inheritdoc />
        public override void NameLists(List<IList<string>> into)
        {
        }

        /// <inheritdoc />
        public override void Settings(List<string> into)
        {
            if (Value.IsSetting && !into.Contains(Value.Text))
                into.Add(Value.Text);
        }

        /// <inheritdoc />
        public override Expression Renamed(string was, string now, bool state)
        {
            return !state && Value.IsSetting && Value.Text == was ? new Operand(Literal.Setting(now, Value.IsKinded)) : this;
        }

        /// <inheritdoc />
        public override Expression Copied()
        {
            return this;
        }

        /// <inheritdoc />
        public override void Walk(Action<Expression> visit)
        {
            visit(this);
        }
    }
}
