using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.World;


namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// A comparison Sprocket watches for itself, which fires as it comes to hold: "when all of these doors are closed",
    /// which the script writes <c>when all ext.status on [ doors ] == closed</c>.
    /// </summary>
    /// <remarks>
    /// The value's id is <see cref="MomentTrigger.EventId" /> and its blocks are <see cref="MomentTrigger.Sources" />, so
    /// everything that resolves, renames or reads a trigger's blocks works on the base and never has to know which. A
    /// signal is a <see cref="SignalTrigger" />.
    /// </remarks>
    public sealed class WhenTrigger : MomentTrigger
    {
        readonly List<Literal> _arguments = new List<Literal>();

        /// <summary>A trigger started when the value <paramref name="eventId" /> comes to be on.</summary>
        public WhenTrigger(string eventId) : base(eventId)
        {
            Value = Literal.Of(true);
        }

        /// <summary>
        /// How the blocks are read: any one of them coming to hold the comparison, all of them, or their sum.
        /// </summary>
        /// <remarks>
        /// "Any" is the default because it is what people mean most of the time - any of these doors opening should turn
        /// the lights on. "All" is a genuinely different sentence: "when tank A <em>and</em> tank B are empty" fires at the
        /// moment the last of them empties, not when the first one does.
        /// <para>
        /// So "all" and "sum" are an edge on the whole condition rather than on any one block, which is why the runtime
        /// tracks them separately instead of just requiring several edges at once - they would almost never land on the
        /// same tick.
        /// </para>
        /// </remarks>
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

        /// <summary>The namespace the member is qualified by: <c>ext</c> for Sprocket's own, or empty for one of the block's.</summary>
        /// <remarks>Kept as the program says it, so a member added by something this host does not have is never taken for one it does.</remarks>
        public string Provider { get; set; } = BlockKinds.Own;

        /// <summary>The same trigger, to change one thing of as an edit; what it starts is the edit's to carry over.</summary>
        public WhenTrigger Copy()
        {
            var copy = new WhenTrigger(EventId)
            {
                Quantifier = Quantifier,
                Relation = Relation,
                Value = Value,
                Interval = Interval,
                Provider = Provider
            };

            for (var i = 0; i < Sources.Count; i++)
                copy.Sources.Add(Sources[i]);

            copy._arguments.AddRange(_arguments);
            return copy;
        }

        /// <summary>The value among <paramref name="actions" /> that the member names: Sprocket's own where it is qualified by <c>ext</c>, the block's own where it is bare; null where it names none.</summary>
        public EntityAction Names(IList<EntityAction> actions)
        {
            return EntityAction.Value(actions, Provider, EventId);
        }
    }
}
