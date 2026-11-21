using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using System;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Model.Expressions
{
    /// <summary>What one of the logic system's states holds, as an operand.</summary>
    public sealed class StateOperand : Expression
    {
        /// <summary>An operand reading the state called <paramref name="name" />.</summary>
        public StateOperand(string name)
        {
            Name = name;
        }

        /// <summary>The state's name.</summary>
        public string Name { get; }

        /// <inheritdoc />
        public override int Precedence => AtomPrecedence;

        /// <inheritdoc />
        public override ExpressionFailure Evaluate(IExpressionScope scope, out Literal value)
        {
            return scope.State(Name, out value) ? ExpressionFailure.None : ExpressionFailure.Unanswered;
        }

        /// <inheritdoc />
        public override bool TypeOf(ITypeScope scope, out ValueKind? kind)
        {
            kind = scope.State(Name);
            return true;
        }

        /// <inheritdoc />
        public override void NameLists(List<IList<string>> into)
        {
        }

        /// <inheritdoc />
        public override void Settings(List<string> into)
        {
        }

        /// <inheritdoc />
        public override Expression Renamed(string was, string now, bool state)
        {
            return state && Name == was ? new StateOperand(now) : this;
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
