using Sprocket.Host.SpaceEngineers.Framework.Model.Expressions;
using System;

namespace Sprocket.Host.SpaceEngineers.Framework.Model
{
    /// <summary>Every place a logic system's steps and triggers hold an expression, each rewritten where it stands.</summary>
    public static class ExpressionSites
    {
        /// <summary>
        /// Gives every expression <paramref name="system" /> holds to <paramref name="rewrite" />, with the declared type the place
        /// takes where it is one, the type of the state a set step sets, and empty elsewhere; the expression it gives back
        /// stands in its place, where it is another.
        /// </summary>
        /// <remarks>Asked as the program is edited, never on a tick.</remarks>
        public static void Rewrite(AutomationSystem system, Func<Expression, string, Expression> rewrite)
        {
            foreach (var sequence in system.Sequences)
            {
                foreach (var step in sequence.Steps)
                    Rewrite(system, step, rewrite);
            }

            foreach (var trigger in system.Triggers)
                Rewrite(trigger, rewrite);
        }

        /// <summary>Gives every expression <paramref name="system" /> holds to <paramref name="visit" />, with the declared type its place takes as <see cref="Rewrite(AutomationSystem, Func{Expression, string, Expression})" /> says, and changes none.</summary>
        public static void Each(AutomationSystem system, Action<Expression, string> visit)
        {
            Rewrite(system, (tree, wanted) =>
            {
                visit(tree, wanted);
                return tree;
            });
        }

        static void Rewrite(AutomationSystem system, Step step, Func<Expression, string, Expression> rewrite)
        {
            var given = step.Arguments;
            for (var i = 0; i < given.Count; i++)
            {
                if (given[i].IsExpression)
                    given[i] = Worked(given[i], rewrite);
            }

            var branch = step as Branch;
            if (branch?.Compared != null && branch.Compared.Value.IsExpression)
                branch.Compared.Value = Worked(branch.Compared.Value, rewrite);

            var tested = branch?.Test as ExpressionTest;
            if (tested != null)
                tested.Condition = Of(tested.Condition, "", rewrite);

            var wait = step as Wait;
            if (wait != null)
                wait.Expression = Of(wait.Expression, "", rewrite);

            var abort = step as Abort;
            if (abort != null)
                abort.Expression = Of(abort.Expression, "", rewrite);

            var sets = step as SetState;
            if (sets != null)
                sets.Expression = Of(sets.Expression, system.FindState(sets.State)?.Type ?? "", rewrite);
        }

        static void Rewrite(Trigger trigger, Func<Expression, string, Expression> rewrite)
        {
            var when = trigger as WhenTrigger;
            if (when != null)
            {
                if (when.Value.IsExpression)
                    when.Value = Worked(when.Value, rewrite);

                for (var i = 0; i < when.Arguments.Count; i++)
                {
                    if (when.Arguments[i].IsExpression)
                        when.Arguments[i] = Worked(when.Arguments[i], rewrite);
                }

                when.IntervalExpression = Of(when.IntervalExpression, "", rewrite);
            }

            var every = trigger as EveryTrigger;
            if (every != null)
                every.IntervalExpression = Of(every.IntervalExpression, "", rewrite);

            var expressed = trigger as ExpressionTrigger;
            if (expressed != null)
            {
                expressed.Condition = Of(expressed.Condition, "", rewrite);
                expressed.IntervalExpression = Of(expressed.IntervalExpression, "", rewrite);
            }
        }

        // The literal holding the expression rewritten, the same literal where the expression is unchanged.
        static Literal Worked(Literal value, Func<Expression, string, Expression> rewrite)
        {
            var tree = rewrite(value.Tree, "");
            return ReferenceEquals(tree, value.Tree) ? value : Literal.Worked(tree);
        }

        static Expression Of(Expression tree, string wanted, Func<Expression, string, Expression> rewrite)
        {
            return tree == null ? null : rewrite(tree, wanted);
        }
    }
}
