using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.World;
using VRageMath;
using Lumel.Input;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.Localization;

namespace Sprocket.Host.SpaceEngineers.Features.Sentences
{
    /// <summary>
    /// What an action was given: its options, or the new value a step sets.
    /// </summary>
    /// <remarks>
    /// One slot for all of them rather than one each, because the action says how many it wants and
    /// nothing whatever about what they are for: three slots with no names on them would be three
    /// questions somebody cannot tell apart. They are asked for and shown together, in order, which
    /// is the only thing the game declares about them.
    /// </remarks>
    public sealed class ArgumentsSlot : SentenceSlot
    {
        readonly List<Literal> _arguments = new List<Literal>();
        readonly List<Field> _fields = new List<Field>();
        readonly List<string> _filled = new List<string>();
        readonly Given _holds;

        /// <summary>
        /// The arguments <paramref name="arguments" />, asked for by <paramref name="fields" />, pressed to <paramref name="opens" />,
        /// which are <paramref name="holds" /> to what holds them; the first said for several blocks together where
        /// <paramref name="plural" /> says so: "are open".
        /// </summary>
        public ArgumentsSlot(IList<Literal> arguments, IList<Field> fields, ICommand opens, Given holds = Given.Options,
            bool plural = false) : base(opens)
        {
            _holds = holds;

            for (var i = 0; i < arguments.Count; i++)
                _arguments.Add(arguments[i]);

            for (var i = 0; i < fields.Count; i++)
                _fields.Add(fields[i]);

            // One per field and not one per argument, and worked out where the question that fills a
            // step in works it out, so the two cannot arrive at different words for one argument.
            Channels.Shown(_arguments, _fields, _filled);
            if (plural && _filled.Count > 0)
                _filled[0] = SentenceText.Plural(_filled[0]);

            // Only where the whole slot is one colour. A slot showing a colour and two other arguments
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

        /// <summary>The verb of a step enabling or disabling a state, <paramref name="verb" />, pressed to <paramref name="opens" />, which opens on its new value.</summary>
        public static ArgumentsSlot Switching(string verb, ICommand opens)
        {
            var words = new[] { SentenceText.EnableWord, SentenceText.DisableWord };
            return new ArgumentsSlot(new[] { Literal.Name(verb) }, new[] { Field.Among("state", words, words) }, opens, Given.NewValue);
        }

        /// <summary>
        /// The colour this slot holds, shown as itself rather than as the numbers behind it.
        /// </summary>
        /// <remarks>
        /// Worked out when the slot is made, like everything else here, because a sentence is dressed
        /// on every tick and reading a colour out of three arguments is not a per-tick job.
        /// </remarks>
        public override Color? Sample => _sample;

        /// <summary>What the action was given, in order.</summary>
        public IList<Literal> Arguments => _arguments;

        /// <summary>Whether every field shows the word a sentence leaves out, so the slot says nothing the action does not anyway.</summary>
        public bool Quiet
        {
            get
            {
                for (var i = 0; i < _filled.Count; i++)
                {
                    if (_fields[i].Quiet == null || _filled[i] != _fields[i].Quiet)
                        return false;
                }

                return _filled.Count > 0;
            }
        }

        /// <inheritdoc />
        public override bool IsEmpty
        {
            get
            {
                foreach (var text in _filled)
                {
                    if (text.Length > 0)
                        return false;
                }

                return true;
            }
        }

        /// <summary>
        /// What each argument asks for, one per argument the action wants.
        /// </summary>
        /// <remarks>
        /// From the action rather than from what the step holds: a step whose arguments were never
        /// filled in still has to be asked with as many fields as the action declared.
        /// </remarks>
        public override IList<Field> Fields => _fields;

        /// <summary>
        /// What opens in each field, one per field rather than one per argument.
        /// </summary>
        /// <remarks>
        /// The spelling the program holds, so what opens in the field is what somebody put into it
        /// rather than a rendering of it that would come back different. The exception is a field
        /// standing over several arguments, where there is no one spelling in the program to show: a
        /// colour's three numbers are shown as the one colour they make, and go back as three.
        /// <para>
        /// Made when the slot is, which is when the sentence is built. Worked out on the way past
        /// instead, it would be worked out every tick, because dressing a chip is a per-tick job.
        /// </para>
        /// </remarks>
        public override IList<string> Filled => _filled;

        /// <summary>What pressing this part does, worded for what it holds.</summary>
        public string About(Texts texts)
        {
            if (_holds == Given.NewValue)
                return texts.SequenceNewValueAbout;

            if (_holds == Given.Compared)
                return texts.SequenceComparedAbout;

            if (_holds == Given.Read)
                return texts.SequenceReadAbout;

            return _holds == Given.Checked ? texts.SequenceCheckedAbout : texts.SequenceOptionsAbout;
        }
    }
}
