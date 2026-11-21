using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Model.Expressions;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Model
{
    /// <summary>
    /// A comparison Sprocket watches for itself, which fires as it comes to hold: "when all of these doors are closed",
    /// which the script writes <c>when all sprocket.status on [ doors ] == closed</c>.
    /// </summary>
    public sealed class WhenTrigger : MomentTrigger
    {
        readonly List<Literal> _arguments = new List<Literal>();

        /// <summary>A trigger started when the value <paramref name="eventId" /> comes to be on.</summary>
        public WhenTrigger(string eventId) : base(eventId)
        {
            Key = new MemberKey(BlockKinds.Own, eventId);
            Value = Literal.Of(true);
        }

        /// <summary>
        /// How the blocks are read: any one of them coming to hold the comparison, all of them, or their sum.
        /// </summary>
        public Quantifier Quantifier { get; set; }

        /// <summary>How the value is compared with <see cref="Value" />.</summary>
        public Relation Relation { get; set; }

        /// <summary>What the value is compared with.</summary>
        public Literal Value { get; set; }

        /// <summary>What the value is given, for a function, in the order it declares its parameters.</summary>
        public IList<Literal> Arguments => _arguments;

        /// <summary>How often the blocks are looked at, as the script writes it, or no length where the program leaves it to the default.</summary>
        public Duration Interval { get; set; }

        /// <summary>How often the blocks are looked at, in ticks, or 0 where the program leaves it to the default.</summary>
        public int IntervalTicks => Interval.Ticks;

        /// <summary>The setting whose length of time is how often the blocks are looked at, each installation's own, or empty where <see cref="Interval" /> says.</summary>
        public string IntervalSetting { get; set; } = "";

        /// <summary>The expression whose length of time is how often the blocks are looked at, worked out on each look, or null where <see cref="Interval" /> or a setting says.</summary>
        public Expression IntervalExpression { get; set; }

        /// <inheritdoc />
        public override void Worked(List<Expression> into)
        {
            if (Value.IsExpression)
                into.Add(Value.Tree);

            foreach (var argument in _arguments)
            {
                if (argument.IsExpression)
                    into.Add(argument.Tree);
            }

            if (IntervalExpression != null)
                into.Add(IntervalExpression);
        }

        /// <summary>The namespace the member is written qualified by, or empty where it is written bare.</summary>
        /// <remarks>Setting it keeps the id; the member is still named by <see cref="MomentTrigger.Key" />.</remarks>
        public string Provider
        {
            get { return Key.Namespace; }
            set { Key = new MemberKey(value, Key.Id); }
        }

        /// <summary>A copy of this trigger, to change one thing of as an edit; what it starts is the edit's to carry over.</summary>
        public WhenTrigger Copy()
        {
            var copy = new WhenTrigger(EventId)
            {
                Quantifier = Quantifier,
                Relation = Relation,
                Value = Value.Copied,
                Interval = Interval,
                IntervalSetting = IntervalSetting,
                IntervalExpression = IntervalExpression,
                Key = Key
            };

            for (var i = 0; i < Sources.Count; i++)
                copy.Sources.Add(Sources[i]);

            foreach (var argument in _arguments)
                copy._arguments.Add(argument.Copied);
            return copy;
        }

        /// <summary>The value among <paramref name="actions" /> that the member names; null where it names none, or a bare name meets more than one.</summary>
        public EntityAction Names(IList<EntityAction> actions)
        {
            return EntityAction.Value(actions, Key);
        }

        /// <summary>Whether this trigger's bare name meets more than one value among <paramref name="actions" />, so it names none of them.</summary>
        public bool Ambiguous(IList<EntityAction> actions)
        {
            return EntityAction.Ambiguous(actions, Key, true);
        }
    }
}
