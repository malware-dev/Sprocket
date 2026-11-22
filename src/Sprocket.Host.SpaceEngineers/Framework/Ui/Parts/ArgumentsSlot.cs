using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Model.Expressions;
using Sprocket.Host.SpaceEngineers.Framework.Text;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using System;
using System.Collections.Generic;
using VRageMath;
using Lumel.Input;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Parts
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
            for (var i = 0; i < _filled.Count && i < _fields.Count; i++)
            {
                // A field that opens on a word for "nothing given" shows that word here too, and a sentence says its own.
                if (_fields[i].SaidEmpty != null && (_filled[i].Length == 0 || _filled[i] == _fields[i].Default))
                    _filled[i] = _fields[i].SaidEmpty;
            }

            if (plural && _filled.Count > 0)
                _filled[0] = SentenceText.Plural(_filled[0]);

            Color shade;
            byte alpha;
            if (_fields.Count == 1 && _fields[0].Edits == FieldEditor.Color
                && ColorChannels.TryRead(_filled[0], out shade, out alpha))
            {
                _sample = shade;
            }
        }

        /// <summary>What the arguments are to what holds them.</summary>
        public ArgumentRole Holds => _holds;

        /// <summary>Has each field given an expression show it in the words <paramref name="shows" /> gives it, rather than the script's.</summary>
        public void ShowExpressions(Func<Expression, string> shows)
        {
            var at = 0;
            for (var i = 0; i < _fields.Count && i < _filled.Count; i++)
            {
                if (at < _arguments.Count && _arguments[at].IsExpression)
                    _filled[i] = _fields[i].Wording(shows(_arguments[at].Tree));

                at += _fields[i].Covers;
            }
        }

        readonly Color? _sample;

        /// <summary>
        /// Puts into <paramref name="ahead" /> the values a sentence says ahead of the blocks, in the order said, with their
        /// fields, and into <paramref name="after" /> the rest, in the order given.
        /// </summary>
        /// <remarks>Where nothing is said ahead, <paramref name="after" /> holds everything as it was given.</remarks>
        public static void Split(IList<Literal> arguments, IList<Field> fields, List<Literal> ahead, List<Field> aheadFields,
            List<Literal> after, List<Field> afterFields)
        {
            var places = 0;
            for (var f = 0; f < fields.Count; f++)
                places = fields[f].SaidAhead > places ? fields[f].SaidAhead : places;

            for (var place = 1; place <= places; place++)
            {
                for (var f = 0; f < fields.Count; f++)
                {
                    if (fields[f].SaidAhead != place)
                        continue;

                    ahead.Add(f < arguments.Count ? arguments[f] : Literal.Of(""));
                    aheadFields.Add(WithUnit(fields[f], arguments, fields, f));
                }
            }

            if (places == 0)
            {
                after.AddRange(arguments);
                afterFields.AddRange(fields);
                return;
            }

            for (var f = 0; f < fields.Count; f++)
            {
                if (fields[f].SaidAhead != 0)
                    continue;

                after.Add(f < arguments.Count ? arguments[f] : Literal.Of(""));
                afterFields.Add(fields[f]);
            }
        }

        /// <summary><paramref name="field" />, the one at <paramref name="at" />, with the unit the answer to the field it follows gives it, where it holds a value to say it with.</summary>
        static Field WithUnit(Field field, IList<Literal> arguments, IList<Field> fields, int at)
        {
            if (field.UnitOf == null || at >= arguments.Count || arguments[at].Said.Length == 0)
                return field;

            for (var f = 0; f < fields.Count; f++)
            {
                if (fields[f].Name != field.UnitFollows)
                    continue;

                var said = f < arguments.Count ? arguments[f].Said : "";
                field.Unit = field.UnitOf(said, false);
                field.UnitOfOne = field.UnitOf(said, true);
            }

            return field;
        }

        /// <summary>
        /// Lays out on <paramref name="line" /> what a step gives its action around <paramref name="blocks" />, which
        /// <paramref name="join" /> joins to what comes before them; each part opens with <paramref name="opens" />, and an option
        /// naming slots stands as a part of its own, joined by its own word, as <paramref name="named" /> makes it.
        /// </summary>
        public static void Lay(IList<SentenceSlot> line, EntitiesSlot blocks, string join, IList<Literal> arguments, IList<Field> fields,
            ICommand opens, ArgumentRole holds, Func<IList<string>, EntitiesSlot> named)
        {
            var ahead = new List<Literal>();
            var aheadFields = new List<Field>();
            var after = new List<Literal>();
            var afterFields = new List<Field>();
            Split(arguments, fields, ahead, aheadFields, after, afterFields);

            if (aheadFields.Count > 0)
                line.Add(new ArgumentsSlot(ahead, aheadFields, opens, holds));

            blocks.Joined = join;
            line.Add(blocks);

            for (var f = afterFields.Count - 1; f >= 0; f--)
            {
                if (!afterFields[f].TakesSlots)
                    continue;

                var slots = named(f < after.Count && after[f].Kind == ValueKind.Slots ? after[f].Names : new string[0]);
                slots.IsOption = true;
                slots.Joined = afterFields[f].Lead;
                line.Add(slots);
                if (f < after.Count)
                    after.RemoveAt(f);

                afterFields.RemoveAt(f);
            }

            var given = new ArgumentsSlot(after, afterFields, opens, holds);
            if (afterFields.Count > 0 && !given.Quiet)
                line.Add(given);
        }

        /// <summary>The verb of a step enabling or disabling a state, <paramref name="verb" />, pressed to <paramref name="opens" />, which opens on its new value.</summary>
        public static ArgumentsSlot Switching(string verb, ICommand opens)
        {
            var words = new[] { SentenceText.EnableWord, SentenceText.DisableWord };
            return new ArgumentsSlot(new[] { Literal.Name(verb) }, new[] { Field.Among("state", words, words) }, opens, ArgumentRole.NewValue);
        }

        /// <summary>
        /// What an abort says, <paramref name="message" />, or the setting it says, <paramref name="setting" />, by its name, and
        /// the level it shows at, <paramref name="level" />, pressed to <paramref name="opens" />.
        /// </summary>
        /// <remarks>An abort with neither says "silently", so the part has words to press; one with either always says its level.</remarks>
        public static ArgumentsSlot Aborting(string message, string setting, string expression, AbortLevel level, ICommand opens)
        {
            var named = !string.IsNullOrEmpty(setting);
            var worked = !string.IsNullOrEmpty(expression);
            if (!named && !worked && string.IsNullOrEmpty(message) && level == AbortLevel.Note)
                return new ArgumentsSlot(new[] { Literal.Name(SentenceText.SilentlyWord) }, new[] { Field.Called("") }, opens, ArgumentRole.Aborts);

            var said = new[] { worked ? Literal.Of(expression) : named ? Literal.Setting(setting) : Literal.Of(SentenceText.Quote(message)), Literal.Name(SentenceText.AbortLevels[(int)level]) };
            var fields = new[] { SentenceText.AbortMessageField(""), SentenceText.AbortLevelField("") };
            return new ArgumentsSlot(said, fields, opens, ArgumentRole.Aborts);
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

            if (_holds == ArgumentRole.ReadWith)
                return texts.Codex.SequenceReadWithAbout;

            if (_holds == ArgumentRole.Aborts)
                return texts.Codex.SequenceAbortPartAbout;

            return _holds == ArgumentRole.Checked ? texts.Codex.SequenceCheckedAbout : texts.Codex.SequenceOptionsAbout;
        }
    }
}
