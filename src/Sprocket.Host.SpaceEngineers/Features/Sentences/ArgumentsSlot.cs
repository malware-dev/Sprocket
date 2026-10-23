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
    public sealed class ArgumentsSlot : SentenceSlot
    {
        readonly List<Literal> _arguments = new List<Literal>();
        readonly List<Field> _fields = new List<Field>();
        readonly List<string> _filled = new List<string>();
        readonly ArgumentRole _holds;

        /// <summary>
        /// The arguments <paramref name="arguments" />, asked for by <paramref name="fields" />, pressed to <paramref name="opens" />,
        /// which are <paramref name="holds" /> to what holds them; the first said for several blocks together where
        /// <paramref name="plural" /> says so: "are open".
        /// </summary>
        public ArgumentsSlot(IList<Literal> arguments, IList<Field> fields, ICommand opens, ArgumentRole holds = ArgumentRole.Options,
            bool plural = false) : base(opens)
        {
            _holds = holds;

            for (var i = 0; i < arguments.Count; i++)
                _arguments.Add(arguments[i]);

            for (var i = 0; i < fields.Count; i++)
                _fields.Add(fields[i]);

            // The question that fills a step words its fields here too, so both say the same.
            ColorChannels.Shown(_arguments, _fields, _filled);
            if (plural && _filled.Count > 0)
                _filled[0] = SentenceText.Plural(_filled[0]);

            Color shade;
            byte alpha;
            if (_fields.Count == 1 && _fields[0].Edits == FieldEditor.Colour
                && ColorChannels.Colour(_filled[0], out shade, out alpha))
            {
                _sample = shade;
            }
        }

        readonly Color? _sample;

        /// <summary>The verb of a step enabling or disabling a state, <paramref name="verb" />, pressed to <paramref name="opens" />, which opens on its new value.</summary>
        public static ArgumentsSlot Switching(string verb, ICommand opens)
        {
            var words = new[] { SentenceText.EnableWord, SentenceText.DisableWord };
            return new ArgumentsSlot(new[] { Literal.Name(verb) }, new[] { Field.Among("state", words, words) }, opens, ArgumentRole.NewValue);
        }

        /// <summary>
        /// The color this slot shows as a swatch, or null where it holds none.
        /// </summary>
        /// <remarks>
        /// Made with the slot, because a sentence is dressed every tick.
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
        public override IList<Field> Fields => _fields;

        /// <summary>
        /// What opens in each field, one per field.
        /// </summary>
        /// <remarks>
        /// The spelling the program holds, with a color's three numbers shown as one color. Made with the slot,
        /// because dressing a chip is a per-tick job.
        /// </remarks>
        public override IList<string> Filled => _filled;

        /// <summary>What pressing this part does, worded for what it holds.</summary>
        public string About(Texts texts)
        {
            if (_holds == ArgumentRole.NewValue)
                return texts.Codex.SequenceNewValueAbout;

            if (_holds == ArgumentRole.Compared)
                return texts.Codex.SequenceComparedAbout;

            if (_holds == ArgumentRole.Read)
                return texts.Codex.SequenceReadAbout;

            return _holds == ArgumentRole.Checked ? texts.Codex.SequenceCheckedAbout : texts.Codex.SequenceOptionsAbout;
        }
    }
}
