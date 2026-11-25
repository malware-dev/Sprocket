using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model.Expressions;

namespace Sprocket.Host.SpaceEngineers.Framework.Model
{
    /// <summary>
    /// A condition built as an expression, as the test or trigger a program writes it as: one of a state or a setting, or one of
    /// blocks, where the condition is that shape, and an expression otherwise.
    /// </summary>
    public static class ConditionShapes
    {
        /// <summary>What an if of <paramref name="system" /> tests where it tests <paramref name="condition" />.</summary>
        public static BranchTest TestOf(AutomationSystem system, Expression condition)
        {
            return (BranchTest)StateTestOf(system, condition) ?? (BranchTest)ComparisonOf(condition) ?? new ExpressionTest(condition);
        }

        /// <summary>The trigger of <paramref name="system" /> that fires as <paramref name="trigger" /> does, written as a when on a state or on blocks where its condition is that shape.</summary>
        /// <remarks>A when on a state takes no every, so a condition paced by one stays an expression.</remarks>
        public static Trigger TriggerOf(AutomationSystem system, ExpressionTrigger trigger)
        {
            var paced = trigger.Interval.Ticks > 0 || trigger.IntervalSetting.Length > 0 || trigger.IntervalExpression != null;
            var state = paced ? null : StateTestOf(system, trigger.Condition);
            if (state != null)
                return new StateTrigger(state);

            var compared = ComparisonOf(trigger.Condition);
            if (compared == null)
                return trigger;

            var when = new WhenTrigger(compared.Key.Id)
            {
                Key = compared.Key,
                Quantifier = compared.Quantifier,
                Relation = compared.Relation,
                Value = compared.Value,
                Interval = trigger.Interval,
                IntervalSetting = trigger.IntervalSetting,
                IntervalExpression = trigger.IntervalExpression
            };

            foreach (var argument in compared.Arguments)
                when.Arguments.Add(argument);

            foreach (var target in compared.Targets)
                when.Sources.Add(target);

            return when;
        }

        // A state or a setting compared with a fixed value, a setting, or another state of its system, as a test of a state is.
        static StateTest StateTestOf(AutomationSystem system, Expression condition)
        {
            var comparing = condition as Comparing;
            if (comparing == null)
                return null;

            var ordered = comparing.Relation != Relation.Is && comparing.Relation != Relation.IsNot;
            var state = comparing.Left as StateOperand;
            var declared = state == null ? null : system.FindState(state.Name);
            if (declared != null)
            {
                if (ordered && !declared.IsOrdered)
                    return null;

                var other = comparing.Right as StateOperand;
                if (other != null && system.FindState(other.Name) != null)
                    return new StateTest { State = state.Name, Relation = comparing.Relation, Other = other.Name };

                var given = comparing.Right as Operand;
                return given == null ? null : new StateTest { State = state.Name, Relation = comparing.Relation, Value = given.Value };
            }

            var setting = comparing.Left as Operand;
            var value = comparing.Right as Operand;
            if (setting == null || !setting.Value.IsSetting || value == null || value.Value.IsSetting || ordered)
                return null;

            return new StateTest { Setting = setting.Value.Text, Relation = comparing.Relation, Value = value.Value };
        }

        // A reading of blocks compared, on each block or on what they combine to, as a test of blocks is.
        static Comparison ComparisonOf(Expression condition)
        {
            var reading = condition as BlockReading;
            if (reading != null && reading.Checks)
                return Compared(reading, reading.Combine == Combination.All ? Quantifier.All : Quantifier.Any, reading.Relation, reading.Compared);

            var comparing = condition as Comparing;
            reading = comparing?.Left as BlockReading;
            if (reading == null || reading.Checks)
                return null;

            return Compared(reading, QuantifierOf(reading.Combine), comparing.Relation, comparing.Right);
        }

        static Comparison Compared(BlockReading reading, Quantifier quantifier, Relation relation, Expression with)
        {
            var given = with as Operand;
            var test = new Comparison { Key = reading.Key, Quantifier = quantifier, Relation = relation, Value = given != null ? given.Value : Literal.Worked(with) };
            foreach (var target in reading.Targets)
                test.Targets.Add(target);

            foreach (var argument in reading.Arguments)
                test.Arguments.Add(argument);

            return test;
        }

        // How a test of blocks reads the blocks a reading combines as it says; one block read alone is any of one.
        static Quantifier QuantifierOf(Combination combine)
        {
            switch (combine)
            {
                case Combination.Sum: return Quantifier.Sum;
                case Combination.Average: return Quantifier.Average;
                case Combination.Min: return Quantifier.Min;
                case Combination.Max: return Quantifier.Max;
                default: return Quantifier.Any;
            }
        }
    }
}
