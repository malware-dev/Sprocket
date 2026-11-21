using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using System;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Model.Expressions
{
    /// <summary>A function of the expression language applied to its arguments: <c>min</c>, <c>max</c>, <c>round</c> or <c>abs</c>.</summary>
    public sealed class Function : Expression
    {
        /// <summary>The function <paramref name="kind" /> of <paramref name="arguments" />.</summary>
        public Function(FunctionKind kind, IList<Expression> arguments)
        {
            Kind = kind;
            Arguments = arguments;
        }

        /// <summary>Which function.</summary>
        public FunctionKind Kind { get; }

        /// <summary>What it is applied to.</summary>
        public IList<Expression> Arguments { get; }

        /// <inheritdoc />
        public override int Precedence => AtomPrecedence;

        /// <summary>How many arguments <paramref name="kind" /> takes at least, and whether it takes more.</summary>
        public static int Least(FunctionKind kind, out bool more)
        {
            more = kind == FunctionKind.Min || kind == FunctionKind.Max;
            return more ? 2 : 1;
        }

        /// <summary>How the function <paramref name="kind" /> is written.</summary>
        public static string Spelling(FunctionKind kind)
        {
            switch (kind)
            {
                case FunctionKind.Min: return "min";
                case FunctionKind.Max: return "max";
                case FunctionKind.Round: return "round";
            }

            return "abs";
        }

        /// <inheritdoc />
        public override ExpressionFailure Evaluate(IExpressionScope scope, out Literal value)
        {
            var given = scope.Borrow();
            var failure = ExpressionFailure.None;
            value = default(Literal);
            for (var i = 0; i < Arguments.Count && failure == ExpressionFailure.None; i++)
            {
                Literal one;
                failure = Arguments[i].Evaluate(scope, out one);
                given.Add(one);
            }

            if (failure == ExpressionFailure.None)
                failure = Reckoning.Apply(Kind, given, out value);

            scope.Return(given);
            return failure;
        }

        /// <inheritdoc />
        public override bool TypeOf(ITypeScope scope, out ValueKind? kind)
        {
            kind = null;
            var given = new List<ValueKind?>(Arguments.Count);
            for (var i = 0; i < Arguments.Count; i++)
            {
                ValueKind? one;
                if (!Arguments[i].TypeOf(scope, out one))
                    return false;

                given.Add(one);
            }

            return Reckoning.TypeOf(Kind, given, out kind);
        }

        /// <inheritdoc />
        public override void NameLists(List<IList<string>> into)
        {
            for (var i = 0; i < Arguments.Count; i++)
                Arguments[i].NameLists(into);
        }

        /// <inheritdoc />
        public override void Settings(List<string> into)
        {
            for (var i = 0; i < Arguments.Count; i++)
                Arguments[i].Settings(into);
        }

        /// <inheritdoc />
        public override Expression Renamed(string was, string now, bool state)
        {
            return new Function(Kind, Mapped(e => e.Renamed(was, now, state)));
        }

        /// <inheritdoc />
        public override Expression Copied()
        {
            return new Function(Kind, Mapped(e => e.Copied()));
        }

        IList<Expression> Mapped(Func<Expression, Expression> map)
        {
            var mapped = new List<Expression>(Arguments.Count);
            for (var i = 0; i < Arguments.Count; i++)
                mapped.Add(map(Arguments[i]));

            return mapped;
        }

        /// <inheritdoc />
        public override void Walk(Action<Expression> visit)
        {
            visit(this);
            for (var i = 0; i < Arguments.Count; i++)
                Arguments[i].Walk(visit);
        }
    }
}
