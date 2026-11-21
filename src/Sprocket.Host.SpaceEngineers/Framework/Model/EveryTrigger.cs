

using Sprocket.Host.SpaceEngineers.Framework.Model.Expressions;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Model
{
    /// <summary>
    /// "Every ___" - a recurring trigger.
    /// </summary>
    public sealed class EveryTrigger : Trigger
    {
        /// <summary>A trigger every <paramref name="intervalTicks" /> ticks.</summary>
        public EveryTrigger(int intervalTicks) : this(Duration.OfTicks(intervalTicks))
        {
        }

        /// <summary>A trigger every <paramref name="interval" />.</summary>
        public EveryTrigger(Duration interval)
        {
            Interval = interval;
        }

        /// <summary>How long between one start and the next, as the script writes it.</summary>
        public Duration Interval { get; set; }

        /// <summary>How long between one start and the next, in ticks.</summary>
        public int IntervalTicks => Interval.Ticks;

        /// <summary>The setting whose length of time is how long between one start and the next, each installation's own, or empty where <see cref="Interval" /> says.</summary>
        public string IntervalSetting { get; set; } = "";

        /// <summary>The expression whose length of time is how long between one start and the next, worked out on each look, or null where <see cref="Interval" /> or a setting says.</summary>
        public Expression IntervalExpression { get; set; }

        /// <inheritdoc />
        public override void Worked(List<Expression> into)
        {
            if (IntervalExpression != null)
                into.Add(IntervalExpression);
        }
    }
}
