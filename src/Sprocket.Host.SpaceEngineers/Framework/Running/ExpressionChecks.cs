using Sprocket.Host.SpaceEngineers.Framework.Game.Grids;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Model.Expressions;
using System;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Running
{
    /// <summary>What a program says is wrong with the expressions its steps and triggers hold: members its blocks do not offer, several blocks read as one, and operands that do not suit their operators or their places.</summary>
    /// <remarks>Asked when the program is checked, and on no tick.</remarks>
    public static class ExpressionChecks
    {
        /// <summary>The first step or trigger of <paramref name="sequence" /> on <paramref name="through" /> holding an expression that is wrong, or none.</summary>
        public static SequenceFault Checked(Bindings bindings, Sequence sequence, SystemInstance through)
        {
            var types = new ExpressionTypes(sequence.System, through, bindings);
            var triggers = sequence.System?.Triggers;
            for (var i = 0; triggers != null && i < triggers.Count; i++)
            {
                if (!triggers[i].IsAbout(sequence))
                    continue;

                var worked = new List<Expression>();
                triggers[i].Worked(worked);
                var found = Held(bindings, types, worked, triggers[i], null, through, SequenceFault.Trigger);
                if (found.Exists)
                    return found;
            }

            for (var i = 0; i < sequence.Steps.Count; i++)
            {
                var step = sequence.Steps[i];
                var worked = new List<Expression>();
                step.Worked(worked);
                var found = Held(bindings, types, worked, null, step, through, i);
                if (found.Exists)
                    return found;
            }

            return SequenceFault.None;
        }

        /// <summary>What is wrong with <paramref name="worked" />, the expressions <paramref name="trigger" /> or <paramref name="step" /> holds, at <paramref name="at" />, or none.</summary>
        static SequenceFault Held(Bindings bindings, ExpressionTypes types, List<Expression> worked, Trigger trigger, Step step,
            SystemInstance through, int at)
        {
            if (worked.Count == 0)
                return SequenceFault.None;

            var readings = new List<BlockReading>();
            foreach (var tree in worked)
                tree.Walk(e => Collect(e, readings));

            foreach (var reading in readings)
            {
                var read = Read(bindings, reading, through, at);
                if (read.Exists)
                    return read;
            }

            return Typed(bindings, types, step, trigger, worked, readings, through, at);
        }

        static void Collect(Expression tree, List<BlockReading> into)
        {
            var reading = tree as BlockReading;
            if (reading != null)
                into.Add(reading);
        }

        /// <summary>What is wrong with <paramref name="reading" /> at <paramref name="step" />: a namespace the host lacks, several blocks read as one, or a block that does not offer the member.</summary>
        static SequenceFault Read(Bindings bindings, BlockReading reading, SystemInstance through, int step)
        {
            var space = reading.Key.Namespace;
            if (space.Length > 0 && !HostNamespaces.Has(space))
                return SequenceFault.Uninstalled(step, space);

            var names = SystemInstance.Through(through, reading.Targets);
            if (reading.Combine == Combination.None && names.Count > 1)
                return new SequenceFault(SequenceProblem.ManyBlocks, step);

            foreach (var name in names)
            {
                GridEntity entity = bindings.Entity(name);
                if (entity != null && Probe.Value(entity.Kind, reading.Key) == null)
                    return new SequenceFault(SequenceProblem.Unoffered, step);
            }

            return SequenceFault.None;
        }

        /// <summary>Whether the operands of each expression suit their operators, and the expression suits its place.</summary>
        static SequenceFault Typed(Bindings bindings, ExpressionTypes types, Step step, Trigger trigger, List<Expression> worked,
            List<BlockReading> readings, SystemInstance through, int at)
        {
            var mistyped = new SequenceFault(SequenceProblem.Mistyped, at, BlockTrouble.None, "");
            ValueKind? kind;
            foreach (var tree in worked)
            {
                if (!tree.TypeOf(types, out kind))
                    return mistyped;
            }

            foreach (var reading in readings)
            {
                foreach (var argument in reading.Arguments)
                {
                    if (argument.IsExpression && !argument.Tree.TypeOf(types, out kind))
                        return mistyped;
                }
            }

            var every = trigger as EveryTrigger;
            if (every != null)
                return Lasting(every.IntervalExpression, types, mistyped);

            var when = trigger as WhenTrigger;
            if (when != null)
            {
                var lasting = Lasting(when.IntervalExpression, types, mistyped);
                if (lasting.Exists)
                    return lasting;

                var compared = Compared(when.Value, when.Relation, when.Quantifier, types, mistyped);
                return compared.Exists ? compared : Given(bindings, types, when.Arguments, when.Sources, when.Names, through, at);
            }

            var wait = step as Wait;
            if (wait != null)
                return Lasting(wait.Expression, types, mistyped);

            var branch = step as Branch;
            if (branch?.Compared != null)
                return Compared(branch.Compared.Value, branch.Compared.Relation, branch.Compared.Quantifier, types, mistyped);

            var abort = step as Abort;
            if (abort != null)
                return Worded(abort.Expression, ValueKind.Text, types, mistyped);

            var sets = step as SetState;
            if (sets != null)
                return Worded(sets.Expression, types.State(sets.State), types, mistyped);

            var act = step as Act;
            return act == null || !act.HasAction ? SequenceFault.None : Given(bindings, types, act.Arguments, act.Targets, act.Names, through, at);
        }

        /// <summary>Whether <paramref name="expression" />, where there is one, comes to a length of time.</summary>
        static SequenceFault Lasting(Expression expression, ExpressionTypes types, SequenceFault mistyped)
        {
            if (expression == null)
                return SequenceFault.None;

            ValueKind? kind;
            expression.TypeOf(types, out kind);
            return kind != null && kind != ValueKind.Duration ? mistyped : SequenceFault.None;
        }

        /// <summary>Whether <paramref name="expression" />, where there is one, comes to the kind <paramref name="wanted" />, where that is known.</summary>
        static SequenceFault Worded(Expression expression, ValueKind? wanted, ExpressionTypes types, SequenceFault mistyped)
        {
            if (expression == null)
                return SequenceFault.None;

            ValueKind? kind;
            expression.TypeOf(types, out kind);
            return kind != null && wanted != null && kind != wanted ? mistyped : SequenceFault.None;
        }

        /// <summary>Whether the expression a test compares with is a number where the test orders or totals, as a literal there would have to be.</summary>
        static SequenceFault Compared(Literal value, Relation relation, Quantifier quantifier, ExpressionTypes types, SequenceFault mistyped)
        {
            if (!value.IsExpression)
                return SequenceFault.None;

            ValueKind? kind;
            value.Tree.TypeOf(types, out kind);
            var ordered = relation != Relation.Is && relation != Relation.IsNot || Quantifiers.Totals(quantifier);
            return ordered && kind != null && !Expression.IsNumber(kind.Value) ? mistyped : SequenceFault.None;
        }

        /// <summary>The first block a member is given an expression of a kind it does not take, as <paramref name="at" />, or none.</summary>
        static SequenceFault Given(Bindings bindings, ExpressionTypes types, IList<Literal> arguments, IList<string> targets,
            Func<IList<EntityAction>, EntityAction> names, SystemInstance through, int at)
        {
            var samples = new List<Literal>(arguments.Count);
            foreach (var argument in SystemInstance.Settled(through, arguments))
            {
                if (!argument.IsExpression)
                {
                    samples.Add(argument);
                    continue;
                }

                ValueKind? kind;
                argument.Tree.TypeOf(types, out kind);
                var sample = ExpressionTypes.Sample(kind);
                if (sample == null)
                    return SequenceFault.None;

                samples.Add(sample.Value);
            }

            foreach (var target in SystemInstance.Through(through, targets))
            {
                GridEntity entity = bindings.Entity(target);
                var refused = entity == null ? null : names(entity.Kind.Actions)?.Refused(samples, false);
                if (refused != null)
                    return new SequenceFault(SequenceProblem.Mistyped, at, BlockTrouble.None, refused);
            }

            return SequenceFault.None;
        }
    }
}
