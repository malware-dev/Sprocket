using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Layout;
using Sprocket.Host.SpaceEngineers.World;
using VRageMath;
using Sprocket.Host.SpaceEngineers.Ui.Input;

namespace Sprocket.Host.SpaceEngineers.Sequences
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
        readonly List<Field> _fields = new List<Field>();
        readonly List<string> _filled = new List<string>();

        public ValuesSlot(IList<Literal> values, IList<Field> fields, ICommand opens) : base(opens)
        {
            for (var i = 0; i < values.Count; i++)
                _values.Add(values[i]);

            for (var i = 0; i < fields.Count; i++)
                _fields.Add(fields[i]);

            // One per field and not one per value, and worked out where the question that fills a
            // step in works it out, so the two cannot arrive at different words for one value.
            Channels.Shown(_values, _fields, _filled);

            // Only where the whole slot is one colour. A slot showing a colour and two other values
            // has no one thing to draw, and a rectangle standing for part of it would say the rest
            // was not there.
            Color shade;
            byte alpha;
            if (_fields.Count == 1 && _fields[0].Edits == Editor.Colour
                && Channels.Colour(_filled[0], out shade, out alpha))
            {
                _sample = shade;
            }
        }

        readonly Color? _sample;

        /// <summary>
        /// The colour this slot holds, shown as itself rather than as the numbers behind it.
        /// </summary>
        /// <remarks>
        /// Worked out when the slot is made, like everything else here, because a sentence is dressed
        /// on every tick and reading a colour out of three values is not a per-tick job.
        /// </remarks>
        public override Color? Sample
        {
            get { return _sample; }
        }

        public IList<Literal> Values => _values;

        public override bool IsEmpty => _values.Count == 0;

        /// <summary>
        /// What each value asks for, one per value the action wants.
        /// </summary>
        /// <remarks>
        /// From the action rather than from what the step holds: a step whose values were never
        /// filled in still has to be asked with as many fields as the action declared.
        /// </remarks>
        public override IList<Field> Fields => _fields;

        /// <summary>
        /// What opens in each field, one per field rather than one per value.
        /// </summary>
        /// <remarks>
        /// The spelling the program holds, so what opens in the field is what somebody put into it
        /// rather than a rendering of it that would come back different. The exception is a field
        /// standing over several values, where there is no one spelling in the program to show: a
        /// colour's three numbers are shown as the one colour they make, and go back as three.
        /// <para>
        /// Made when the slot is, which is when the sentence is built. Worked out on the way past
        /// instead, it would be worked out every tick, because dressing a chip is a per-tick job.
        /// </para>
        /// </remarks>
        public override IList<string> Filled => _filled;
    }
}
