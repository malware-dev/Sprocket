using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Lumel;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.World;
using VRage.Input;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Localization;

namespace Sprocket.Host.SpaceEngineers.Features.Fields
{
    /// <summary>The values something takes, one line each: what the value is, and where it is given.</summary>
    public class FieldSet : Form
    {
        const int Few = 3;

        readonly List<Field> _wanted = new List<Field>();
        readonly List<IField> _fields = new List<IField>();
        readonly List<bool> _seen = new List<bool>();
        readonly List<string> _parts = new List<string>();
        readonly List<bool> _texts = new List<bool>();
        readonly RelayCommand _submit;

        bool _judged;
        bool _offered;
        Texts _language;

        /// <summary>A form asking for nothing yet.</summary>
        public FieldSet()
        {
            Focusable = true;
            Base.ColumnSpacing = 8f;
            Base.RowSpacing = 8f;

            _submit = new RelayCommand(delegate
            {
                var submitted = Submitted;
                if (submitted != null)
                    submitted();
            });
        }

        /// <summary>Raised when somebody has changed any of the values.</summary>
        public event Action Edited;

        /// <summary>Raised when Enter is pressed in a field, which is finishing the form.</summary>
        public event Action Submitted;

        /// <summary>Raised with the field when text written over several lines is to be written on a page of its own.</summary>
        public event Action<LinesField> Opens;

        /// <summary>How many values it asks for.</summary>
        public int Count => _fields.Count;

        /// <summary>Where each value is given, in the order they are asked for.</summary>
        public IReadOnlyList<IField> Fields => _fields;

        /// <summary>
        /// What is given so far, as one answer: the values joined the way the language writes a list of them, quoted where a
        /// value holds a space or is text.
        /// </summary>
        /// <remarks>
        /// Every value has its place, one left empty or not asked for written as empty text; nothing given at all is empty.
        /// </remarks>
        public string Answer
        {
            get
            {
                _parts.Clear();
                _texts.Clear();
                for (var i = 0; i < _fields.Count; i++)
                {
                    if (_wanted[i].Edits == FieldEditor.Colour)
                    {
                        ColorChannels.Spell(Asked(i) ? _fields[i].Text : "", _wanted[i].Covers, _parts);
                        while (_texts.Count < _parts.Count)
                            _texts.Add(false);

                        continue;
                    }

                    // A member is given every argument, so an empty field is written as its default.
                    var text = Asked(i) ? _fields[i].Text : "";
                    if (string.IsNullOrEmpty(text) && Asked(i) && _wanted[i].Default != null)
                        text = _wanted[i].Default;

                    _parts.Add(_wanted[i].Meaning(text));
                    _texts.Add(_wanted[i].Entry == FieldInput.Text);
                }

                if (_parts.Count == 0)
                    return "";

                // Text is quoted whatever it spells, so a 42 typed where text is wanted stays text.
                if (_parts.Count == 1)
                    return _texts[0] && _parts[0].Length > 0 ? Quoted(_parts[0], true) : _parts[0];

                if (_parts.TrueForAll(string.IsNullOrEmpty))
                    return "";

                var said = "";
                for (var i = 0; i < _parts.Count; i++)
                {
                    if (said.Length > 0)
                        said += " ";

                    said += Quoted(_parts[i], _texts[i] && _parts[i].Length > 0);
                }

                return said;
            }
        }

        /// <summary>Why one of the values will not do as it stands, the first one reading down, or empty where they all will.</summary>
        /// <remarks>Nothing given at all is not an answer, even where every value may be left out.</remarks>
        public string Wrong
        {
            get
            {
                for (var i = 0; i < _fields.Count; i++)
                {
                    var why = Refuses(i);
                    if (why.Length > 0)
                        return why;
                }

                if (_judged && _fields.Count > 0 && !Filled)
                    return Template.Fill(_language.ValueNeeded, "name", SentenceText.Capitalised(Called(0)));

                return "";
            }
        }

        /// <summary>Whether a value has none, and nothing it starts with to stand in for it.</summary>
        public bool Missing
        {
            get
            {
                for (var i = 0; i < _fields.Count; i++)
                {
                    if (Asked(i) && string.IsNullOrEmpty(_fields[i].Text) && !_wanted[i].IsOptional)
                        return true;
                }

                return false;
            }
        }

        /// <summary>Whether every value holds something it takes, so there is nothing left to give.</summary>
        /// <remarks>False while the values only show what the block holds now, offered and not yet given.</remarks>
        public bool Complete
        {
            get
            {
                if (_offered)
                    return false;

                for (var i = 0; i < _fields.Count; i++)
                {
                    var said = _fields[i].Text;
                    if (Asked(i) && (said.Length == 0 || _wanted[i].Refuses(_language, Called(i), said).Length > 0))
                        return false;
                }

                return true;
            }
        }

        /// <summary>Whether anything has been given to any of the values.</summary>
        bool Filled
        {
            get
            {
                for (var i = 0; i < _fields.Count; i++)
                {
                    if (Asked(i) && !string.IsNullOrEmpty(_fields[i].Text))
                        return true;
                }

                return false;
            }
        }

        /// <summary>What a value is known by in <see cref="Spell" />: its name, or its place for a value nobody named.</summary>
        public static string Key(Field field, int at)
        {
            return string.IsNullOrEmpty(field.Name) ? $"#{Ordinal(at)}" : field.Name;
        }

        /// <summary>Asks for <paramref name="fields" />, one line each, holding <paramref name="filled" /> or what each says is usual, its notes in <paramref name="texts" />.</summary>
        /// <remarks>
        /// Makes every field again, so it is for calling only when what the values are for changes; <paramref name="colors" /> are
        /// the player's own.
        /// </remarks>
        public void Show(IList<Field> fields, IList<string> filled, ColorOffer colors, Texts texts)
        {
            _language = texts;
            Rows.Clear();
            _wanted.Clear();
            _fields.Clear();
            _seen.Clear();
            _judged = false;
            _offered = false;

            for (var i = 0; i < fields.Count; i++)
                _wanted.Add(fields[i]);

            for (var i = 0; i < _wanted.Count; i++)
            {
                var field = Make(_wanted[i], colors, texts);
                _fields.Add(field);
                _seen.Add(false);

                var said = i < filled.Count ? filled[i] ?? "" : "";

                if (said.Length == 0 && !string.IsNullOrEmpty(_wanted[i].Default))
                    said = _wanted[i].Default;

                field.Text = _wanted[i].Shown(said);
                field.Edited += Changed;

                Lay(i, field);
            }

            Follow();
        }

        /// <summary>Whether the value at <paramref name="at" /> is asked for: always, or while the value it follows holds one of its words.</summary>
        bool Asked(int at)
        {
            var with = _wanted[at].AskedWith;
            if (string.IsNullOrEmpty(with))
                return true;

            for (var i = 0; i < _wanted.Count; i++)
            {
                if (_wanted[i].Name == with)
                    return Asked(i) && _wanted[at].AskedFor != null && _wanted[at].AskedFor.Contains(_fields[i].Text);
            }

            return false;
        }

        /// <summary>Shows each value that is asked for as things stand, and hides the rest, keeping what they hold.</summary>
        void Follow()
        {
            for (var i = 0; i < _fields.Count; i++)
                ((Element)_fields[i]).IsVisible = Asked(i);
        }

        /// <summary>Says the values it holds are what the block holds now, offered rather than given, until somebody changes one.</summary>
        public void Offer()
        {
            _offered = true;
        }

        /// <summary>Finishes the form on Enter, while it or a part of it that does not take Enter holds the keyboard.</summary>
        protected override bool OnKeyDown(InputRoot input, MyKeys key, bool shift, bool ctrl)
        {
            if (key != MyKeys.Enter)
                return false;

            _submit.Execute();
            return true;
        }

        /// <summary>Holds every value to what it takes from now on, marking each that fails.</summary>
        public void Judge()
        {
            _judged = true;
            for (var i = 0; i < _seen.Count; i++)
                _seen[i] = true;

            Mark();
        }

        /// <summary>Gives the keyboard to the first value still to be given, or the first that is wrong, where it is typed into.</summary>
        public void Take(InputRoot input)
        {
            if (_fields.Count == 0)
                return;

            var first = 0;
            for (var i = 0; i < _fields.Count; i++)
            {
                var said = _fields[i].Text;
                if (!Asked(i) || said.Length > 0 && _wanted[i].Refuses(_language, Called(i), said).Length == 0)
                    continue;

                first = i;
                break;
            }

            _seen[first] = true;

            var keyboard = _fields[first].Keyboard;
            if (input == null || keyboard == null)
                return;

            input.Focus(keyboard);

            var box = keyboard as TextBox;
            if (box != null)
                box.SelectAll();
        }

        /// <summary>What each value holds, as its field spells it, by the value it is: see <see cref="Key" />.</summary>
        public void Spell(IDictionary<string, string> into)
        {
            for (var i = 0; i < _fields.Count; i++)
                into[Key(_wanted[i], i)] = _fields[i].Text;
        }

        /// <summary>Marks each value that fails what it takes, and the first where nothing at all is given.</summary>
        void Mark()
        {
            var blank = _judged && !Filled;
            for (var i = 0; i < _fields.Count; i++)
                _fields[i].IsWrong = Asked(i) && (Refuses(i).Length > 0 || blank && i == 0);
        }

        void Changed()
        {
            _offered = false;
            Follow();
            for (var i = 0; i < _fields.Count; i++)
            {
                if (_fields[i].Text.Length > 0)
                    _seen[i] = true;
            }

            Mark();

            var edited = Edited;
            if (edited != null)
                edited();
        }

        /// <summary>The field a value is given in: a switch for on or off, a line of words for a few others, a list for more, a mixer, a dial, a box of several lines, a slider for a bounded number, a number's box, or a box.</summary>
        IField Make(Field field, ColorOffer colors, Texts texts)
        {
            if (field.Edits == FieldEditor.Colour)
                return new ColorField(colors, texts);

            if (field.Edits == FieldEditor.Angle)
                return new AngleField(field.Least, field.Most, _submit);

            if (field.Edits == FieldEditor.Lines)
            {
                var lines = new LinesField(field.Longest, texts);
                lines.Opened += delegate
                {
                    var opens = Opens;
                    if (opens != null)
                        opens(lines);
                };

                return lines;
            }

            var words = field.Edits == FieldEditor.Chosen ? field.Words : null;
            if (field.IsOnOff)
                return new SwitchField(field.Words[0], field.Words[1], _submit);

            if (words != null && words.Count > 0 && words.Count <= Few)
                return new WordsField(words, _submit);

            if (words != null && words.Count > Few)
                return new ListField(words);

            if (SliderField.Takes(field))
                return new SliderField(field, _submit);

            return NumberField.Takes(field) ? (IField)new NumberField(field, _submit) : new TypedField(_submit);
        }

        /// <summary>Puts a value's label and field in a row: a color alone across the whole width, text under its label, anything else beside it where it fits.</summary>
        void Lay(int at, IField field)
        {
            var element = (Element)field;
            var color = _wanted[at].Edits == FieldEditor.Colour;

            var label = Label(at);
            var about = _wanted[at].About;
            // A field of words fills its room so it can fold into a list; its tip goes on the words it shows.
            var words = field as WordsField;
            if (words != null && !string.IsNullOrEmpty(about))
                words.Explain(about);
            else if (!string.IsNullOrEmpty(about))
                element.Tip = CodexTip.For(element.Tip, about);

            if (field is SliderField)
                ((SliderField)field).Keyboard.Tip = CodexTip.For(((SliderField)field).Keyboard.Tip, _wanted[at].Takes(_language).Trim('(', ')'));

            if (label.Length > 0)
            {
                var named = new Label { Style = Styles.ValueLabel, Text = label };
                if (!string.IsNullOrEmpty(about))
                    named.Tip = CodexTip.For(named.Tip, about);

                Rows.Add(named, element, field is TypedField || field is LinesField || field is SliderField ? FormLabelPlacement.Above : FormLabelPlacement.Auto);
                return;
            }

            if (!color && !(field is TypedField) && !(field is LinesField) && !(field is SliderField) && !(field is WordsField))
                element.Align = Align.Right;

            Rows.Add(element);
        }

        /// <summary>What a value is called beside its field: its name where the field already shows what it takes, its label, or its place where there are several.</summary>
        string Label(int at)
        {
            var edits = _wanted[at].Edits;
            if (edits == FieldEditor.Chosen || edits == FieldEditor.Angle || SliderField.Takes(_wanted[at]))
                return _wanted[at].Name ?? "";

            var said = _wanted[at].Label(_language);
            if (said.Length > 0)
                return said;

            return _wanted.Count > 1 ? Ordinal(at) : "";
        }

        /// <summary>What a refusal calls a value: its name, or its place for a value nobody named.</summary>
        string Called(int at)
        {
            var name = _wanted[at].Name ?? "";
            return name.Length > 0 ? name : Template.Fill(_language.ValueNumbered, "number", Ordinal(at));
        }

        /// <summary>Why the value at <paramref name="at" /> will not do, or empty, where it is being held to anything at all.</summary>
        string Refuses(int at)
        {
            if (!_judged)
                return "";

            var said = _fields[at].Text;
            if (!_seen[at] && string.IsNullOrEmpty(said))
                return "";

            return _wanted[at].Refuses(_language, Called(at), said);
        }

        static string Ordinal(int at)
        {
            return (at + 1).ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// One value's text, quoted where it would otherwise read as more than one value or as none, a doubled quote standing
        /// for one.
        /// </summary>
        static string Quoted(string said, bool always)
        {
            if (string.IsNullOrEmpty(said))
                return "\"\"";

            if (!always && said.IndexOf(' ') < 0 && said.IndexOf('"') < 0 && said.IndexOf('\n') < 0)
                return said;

            // Spelled as a program writes text, so a line break goes as an entity and the answer reads back whole.
            var quoted = new StringBuilder("\"");
            for (var i = 0; i < said.Length; i++)
            {
                if (said[i] == '"')
                    quoted.Append('"');

                Storage.TextEntities.Write(quoted, said, i);
            }

            return quoted.Append('"').ToString();
        }
    }
}
