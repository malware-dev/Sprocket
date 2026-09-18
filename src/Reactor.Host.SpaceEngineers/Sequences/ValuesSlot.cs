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
        readonly int _wants;

        public ValuesSlot(IList<Literal> values, int wants, int command) : base(command)
        {
            for (var i = 0; i < values.Count; i++)
                _values.Add(values[i]);

            _wants = wants < 1 ? 1 : wants;
        }

        public IList<Literal> Values => _values;

        public override bool IsEmpty => _values.Count == 0;

        /// <summary>
        /// One field per value the action wants, which is what it was asked with.
        /// </summary>
        /// <remarks>
        /// Not the count it holds: a step whose values were never filled in still has to be asked
        /// with as many fields as the action declared.
        /// </remarks>
        public override int Fields => _wants;

        /// <summary>
        /// Each value as it is written, one per field.
        /// </summary>
        /// <remarks>
        /// The same spelling the sentence shows and the program holds, so what opens in the field is
        /// what somebody typed into it - not a rendering of it that would come back different.
        /// </remarks>
        public override IList<string> Filled
        {
            get
            {
                var said = new List<string>();
                for (var i = 0; i < _values.Count; i++)
                    said.Add(_values[i].Said);

                return said;
            }
        }
    }
}
