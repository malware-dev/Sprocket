using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using System;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Model.Expressions
{
    /// <summary>A member's value read on blocks, <c>gearbox.fill on [ tanks ]</c>, alone or combined across the blocks.</summary>
    public sealed class BlockReading : Expression
    {
        /// <summary>A reading of the member <paramref name="key" /> given <paramref name="arguments" />, on <paramref name="targets" />, combined as <paramref name="combine" />.</summary>
        public BlockReading(MemberKey key, Combination combine, IList<Literal> arguments, IList<string> targets)
        {
            Key = key;
            Combine = combine;
            Arguments = arguments;
            Targets = targets;
        }

        /// <summary>The member read, as the program holds it.</summary>
        public MemberKey Key { get; }

        /// <summary>How the values of the blocks are combined.</summary>
        public Combination Combine { get; }

        /// <summary>What the member is given, in the order it declares its parameters; each may be an expression.</summary>
        public IList<Literal> Arguments { get; }

        /// <summary>The names of the blocks read: a system's variables, or a system blueprint's slots.</summary>
        public IList<string> Targets { get; }

        /// <inheritdoc />
        public override int Precedence => AtomPrecedence;

        /// <summary>Whether any argument is an expression, so it is worked out each time the reading is.</summary>
        public bool HasWorkedArguments
        {
            get
            {
                for (var i = 0; i < Arguments.Count; i++)
                {
                    if (Arguments[i].IsExpression)
                        return true;
                }

                return false;
            }
        }

        /// <inheritdoc />
        public override ExpressionFailure Evaluate(IExpressionScope scope, out Literal value)
        {
            value = default(Literal);
            IList<Literal> given = Arguments;
            List<Literal> worked = null;
            var failure = ExpressionFailure.None;
            if (HasWorkedArguments)
            {
                worked = scope.Borrow();
                for (var i = 0; i < Arguments.Count && failure == ExpressionFailure.None; i++)
                {
                    Literal one;
                    failure = Operand.Of(Arguments[i]).Evaluate(scope, out one);
                    worked.Add(one);
                }

                given = worked;
            }

            var read = scope.Borrow();
            if (failure == ExpressionFailure.None)
            {
                scope.Read(this, given, read);
                failure = Reckoning.Combine(Combine, read, out value);
            }

            scope.Return(read);
            if (worked != null)
                scope.Return(worked);

            return failure;
        }

        /// <inheritdoc />
        public override bool TypeOf(ITypeScope scope, out ValueKind? kind)
        {
            kind = scope.Reading(this);
            if (kind == null)
                return true;

            if (Combine == Combination.None)
                return true;

            if (!IsNumber(kind.Value))
                return false;

            if (Combine == Combination.Average)
                kind = ValueKind.Real;

            return true;
        }

        /// <inheritdoc />
        public override void NameLists(List<IList<string>> into)
        {
            into.Add(Targets);
            for (var i = 0; i < Arguments.Count; i++)
            {
                if (Arguments[i].IsExpression)
                    Arguments[i].Tree.NameLists(into);
                else if (Arguments[i].Kind == ValueKind.Slots)
                    into.Add(Arguments[i].Names);
            }
        }

        /// <inheritdoc />
        public override void Settings(List<string> into)
        {
            for (var i = 0; i < Arguments.Count; i++)
            {
                if (Arguments[i].IsExpression)
                    Arguments[i].Tree.Settings(into);
                else if (Arguments[i].IsSetting && !into.Contains(Arguments[i].Text))
                    into.Add(Arguments[i].Text);
            }
        }

        /// <inheritdoc />
        public override Expression Renamed(string was, string now, bool state)
        {
            var given = new List<Literal>(Arguments.Count);
            for (var i = 0; i < Arguments.Count; i++)
            {
                var one = Arguments[i];
                if (one.IsExpression)
                    one = Literal.Worked(one.Tree.Renamed(was, now, state));
                else if (!state && one.IsSetting && one.Text == was)
                    one = Literal.Setting(now, one.IsKinded);

                given.Add(one);
            }

            return new BlockReading(Key, Combine, given, Targets);
        }

        /// <inheritdoc />
        public override Expression Copied()
        {
            var given = new List<Literal>(Arguments.Count);
            for (var i = 0; i < Arguments.Count; i++)
                given.Add(Arguments[i].Copied);

            return new BlockReading(Key, Combine, given, new List<string>(Targets));
        }

        /// <inheritdoc />
        public override void Walk(Action<Expression> visit)
        {
            visit(this);
            for (var i = 0; i < Arguments.Count; i++)
            {
                if (Arguments[i].IsExpression)
                    Arguments[i].Tree.Walk(visit);
            }
        }
    }
}
