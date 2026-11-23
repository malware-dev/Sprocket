using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using System;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Model.Expressions
{
    /// <summary>
    /// A member's value read on blocks, <c>gearbox.fill on [ tanks ]</c>, alone or combined across the blocks, or compared on
    /// each of them: <c>any gearbox.status on [ doors ] == open</c>.
    /// </summary>
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

        /// <summary>
        /// A reading of the member <paramref name="key" /> given <paramref name="arguments" />, on <paramref name="targets" />, whose
        /// value on each block is compared with <paramref name="compared" /> as <paramref name="relation" /> says, on any of them or
        /// on all as <paramref name="combine" /> says.
        /// </summary>
        public BlockReading(MemberKey key, Combination combine, IList<Literal> arguments, IList<string> targets, Relation relation, Expression compared)
            : this(key, combine, arguments, targets)
        {
            Relation = relation;
            Compared = compared;
        }

        /// <summary>The member read, as the program holds it.</summary>
        public MemberKey Key { get; }

        /// <summary>How the values of the blocks are combined.</summary>
        public Combination Combine { get; }

        /// <summary>What the member is given, in the order it declares its parameters; each may be an expression.</summary>
        public IList<Literal> Arguments { get; }

        /// <summary>The names of the blocks read: a system's variables, or a system blueprint's slots.</summary>
        public IList<string> Targets { get; }

        /// <summary>How each block's value is compared with <see cref="Compared" />, for a reading that checks any or all of them.</summary>
        public Relation Relation { get; }

        /// <summary>What each block's value is compared with, for a reading that checks any or all of them; null otherwise.</summary>
        public Expression Compared { get; }

        /// <summary>Whether this checks a comparison on any or all of its blocks, which gives a flag.</summary>
        public bool Checks => Combine == Combination.Any || Combine == Combination.All;

        /// <inheritdoc />
        /// <remarks>A reading that checks a comparison binds as the comparison it holds does.</remarks>
        public override int Precedence => Checks ? Comparing.ComparisonPrecedence : AtomPrecedence;

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
                failure = Checks ? Checked(scope, read, out value) : Reckoning.Combine(Combine, read, out value);
            }

            scope.Return(read);
            if (worked != null)
                scope.Return(worked);

            return failure;
        }

        /// <summary>Whether the comparison holds on any or on all of <paramref name="read" />, the values of the blocks, into <paramref name="value" />; over no blocks, any is off and all is on.</summary>
        ExpressionFailure Checked(IExpressionScope scope, List<Literal> read, out Literal value)
        {
            Literal compared;
            var failure = Compared.Evaluate(scope, out compared);
            value = default(Literal);
            if (failure != ExpressionFailure.None)
                return failure;

            var all = Combine == Combination.All;
            for (var i = 0; i < read.Count; i++)
            {
                if (!Comparing.Suits(Relation, read[i].Kind, compared.Kind))
                    return ExpressionFailure.Mistyped;

                if (Relations.Holds(read[i], Relation, compared) == all)
                    continue;

                value = Literal.Of(!all);
                return ExpressionFailure.None;
            }

            value = Literal.Of(all);
            return ExpressionFailure.None;
        }

        /// <inheritdoc />
        public override bool TypeOf(ITypeScope scope, out ValueKind? kind)
        {
            kind = scope.Reading(this);
            if (Checks)
            {
                var member = kind;
                ValueKind? compared;
                kind = ValueKind.Flag;
                if (!Compared.TypeOf(scope, out compared))
                    return false;

                return member == null || compared == null || Comparing.Suits(Relation, member.Value, compared.Value);
            }

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

            Compared?.NameLists(into);
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

            Compared?.Settings(into);
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

            return new BlockReading(Key, Combine, given, Targets, Relation, Compared?.Renamed(was, now, state));
        }

        /// <inheritdoc />
        public override Expression Copied()
        {
            var given = new List<Literal>(Arguments.Count);
            for (var i = 0; i < Arguments.Count; i++)
                given.Add(Arguments[i].Copied);

            return new BlockReading(Key, Combine, given, new List<string>(Targets), Relation, Compared?.Copied());
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

            Compared?.Walk(visit);
        }
    }
}
