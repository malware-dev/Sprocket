using System.Collections.Generic;
using Reactor.Host.SpaceEngineers.World;

namespace Reactor.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// What an action was given, where it will not do anything without values.
    /// </summary>
    /// <remarks>
    /// One slot for all of them rather than one each, because the action says how many it wants and
    /// nothing whatever about what they are for: three slots with no names on them would be three
    /// questions somebody cannot tell apart. They are asked for and shown together, in order, which
    /// is the only thing the game declares about them.
    /// </remarks>
    public sealed class ValuesSlot : SentenceSlot
    {
        readonly List<Literal> _values = new List<Literal>();
        readonly List<string> _names = new List<string>();
        readonly List<string> _filled = new List<string>();

        public ValuesSlot(IList<Literal> values, IList<string> names, int command) : base(command)
        {
            for (var i = 0; i < values.Count; i++)
            {
                _values.Add(values[i]);
                _filled.Add(values[i].Said);
            }

            for (var i = 0; i < names.Count; i++)
                _names.Add(names[i]);
        }

        public IList<Literal> Values => _values;

        public override bool IsEmpty => _values.Count == 0;

        /// <summary>
        /// What each value is called, one per value the action wants.
        /// </summary>
        /// <remarks>
        /// From the action rather than from what the step holds: a step whose values were never
        /// filled in still has to be asked with as many fields as the action declared.
        /// </remarks>
        public override IList<string> Names => _names;

        /// <summary>
        /// Each value as it is written, one per field.
        /// </summary>
        /// <remarks>
        /// The same spelling the sentence shows and the program holds, so what opens in the field is
        /// what somebody typed into it - not a rendering of it that would come back different.
        /// <para>
        /// Made when the slot is, which is when the sentence is built. Worked out on the way past
        /// instead, it would be worked out every tick, because dressing a chip is a per-tick job.
        /// </para>
        /// </remarks>
        public override IList<string> Filled => _filled;
    }
}
