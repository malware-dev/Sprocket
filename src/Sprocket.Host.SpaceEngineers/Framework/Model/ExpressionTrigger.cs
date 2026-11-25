using Sprocket.Host.SpaceEngineers.Framework.Model.Expressions;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Model
{
    /// <summary>
    /// A trigger that fires as an expression giving a flag comes to be on:
    /// <c>when average gearbox.fill on [ tanks ] &lt; setting(Low) or alarm == on</c>.
    /// </summary>
    public sealed class ExpressionTrigger : Trigger
    {
        /// <summary>A trigger fired as <paramref name="condition" /> comes to be on.</summary>
        public ExpressionTrigger(Expression condition)
        {
            Condition = condition;
        }

        /// <summary>The expression watched, which gives a flag.</summary>
        public Expression Condition { get; set; }

        /// <summary>How often the blocks it reads are looked at, as the script writes it, or no length where the program leaves it to the default.</summary>
        public Duration Interval { get; set; }

        /// <summary>The setting whose length of time is how often the blocks are looked at, each installation's own, or empty where <see cref="Interval" /> says.</summary>
        public string IntervalSetting { get; set; } = "";

        /// <summary>The expression whose length of time is how often the blocks are looked at, worked out on each look, or null where <see cref="Interval" /> or a setting says.</summary>
        public Expression IntervalExpression { get; set; }

        /// <summary>Whether the condition reads a block, which is what an <c>every</c> paces.</summary>
        /// <remarks>Allocates: asked as a program is read or compiled, never on a look.</remarks>
        public bool ReadsBlocks
        {
            get
            {
                var lists = new List<IList<string>>();
                Condition.NameLists(lists);
                return lists.Count > 0;
            }
        }

        /// <summary>Whether the condition reads a state of the logic system, which a step setting it has it checked again for.</summary>
        public bool ReadsStates => States().Count > 0;

        /// <summary>The states the condition reads, each once.</summary>
        public List<string> States()
        {
            var states = new List<string>();
            Condition.Walk(e =>
            {
                var state = e as StateOperand;
                if (state != null && !states.Contains(state.Name))
                    states.Add(state.Name);
            });
            return states;
        }

        /// <inheritdoc />
        public override void Worked(List<Expression> into)
        {
            into.Add(Condition);
            if (IntervalExpression != null)
                into.Add(IntervalExpression);
        }

        /// <summary>
        /// This as the trigger that fires as it does: where the whole condition is an <c>any</c> check, a <see cref="WhenTrigger" />
        /// firing for each block it comes to hold on; otherwise this.
        /// </summary>
        public Trigger Simplest()
        {
            var check = Condition as BlockReading;
            if (check == null || check.Combine != Combination.Any)
                return this;

            var when = new WhenTrigger(check.Key.Id)
            {
                Key = check.Key,
                Quantifier = Quantifier.Any,
                Relation = check.Relation,
                Value = check.Compared is Operand ? ((Operand)check.Compared).Value : Literal.Worked(check.Compared),
                Interval = Interval,
                IntervalSetting = IntervalSetting,
                IntervalExpression = IntervalExpression,
                Does = Does,
                Annotation = Annotation,
                Category = Category,
                Attributes = Attributes
            };

            foreach (var argument in check.Arguments)
                when.Arguments.Add(argument);

            foreach (var target in check.Targets)
                when.Sources.Add(target);

            return when;
        }

        /// <summary>A copy of this trigger, to change one thing of as an edit; what it starts is the edit's to carry over.</summary>
        public ExpressionTrigger Copy()
        {
            return new ExpressionTrigger(Condition.Copied())
            {
                Interval = Interval,
                IntervalSetting = IntervalSetting,
                IntervalExpression = IntervalExpression
            };
        }
    }
}
