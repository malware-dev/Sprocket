using System;
using System.Collections.Generic;
using System.Globalization;
using Lumel;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.World;
using VRage.Input;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Localization;

namespace Sprocket.Host.SpaceEngineers.Features.Fields
{
    /// <summary>The values something takes, one line each: what the value is, and where it is given.</summary>
    /// <remarks>
    /// A line each rather than side by side. Side by side reads as parts of one thing, and that is a claim about what the
    /// values are for, which the game never says: an action declares how many it takes and nothing else. The labels share a
    /// column, so the fields start together after the widest of them, and a field for text takes whatever width is left
    /// where a bounded one keeps to the right-hand edge.
    /// <para>
    /// Nothing is held against what is filled in until finishing is tried: <see cref="Judge" /> starts holding every field to
    /// what it takes, until the form is given something else to ask.
    /// </para>
    /// <para>
    /// A press on a part of it with no box of its own, such as a word or a color, gives the form the keyboard, and Enter
    /// finishes it from anywhere in it that does not take Enter for itself, as it does in a box.
    /// </para>
    /// </remarks>
    public class FieldSet : Grid
    {
        const int Few = 3;

        readonly List<Field> _wanted = new List<Field>();
        readonly List<IField> _fields = new List<IField>();
        readonly List<Spot> _placed = new List<Spot>();
        readonly List<bool> _seen = new List<bool>();
        readonly List<string> _parts = new List<string>();
        readonly List<bool> _texts = new List<bool>();
        readonly Relay _submit;

        bool _judged;
        Texts _language;

        /// <summary>A form asking for nothing yet.</summary>
        public FieldSet()
        {
            Focusable = true;
            ColumnSpacing = 8f;
            RowSpacing = 8f;
            Columns.Add(Length.Auto);
            Columns.Add(Length.Flex(1));
            Columns.Add(Length.Auto);

            _submit = new Relay(delegate
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

        /// <summary>How many values it asks for.</summary>
        public int Count => _fields.Count;

        /// <summary>Where each value is given, in the order they are asked for.</summary>
        public IReadOnlyList<IField> Fields => _fields;

        /// <summary>
        /// What is given so far, as one answer: the values joined the way the language writes a list of them, quoted where a
        /// value holds a space or is text.
        /// </summary>
        /// <remarks>
        /// Only as far as the last value somebody gave. A value left alone is one not given, so the action is handed the
        /// default it declared, where an empty one passed along would be an empty value and usually a worse one.
        /// </remarks>
        public string Answer
        {
            get
            {
                _parts.Clear();
                _texts.Clear();
                for (var i = 0; i < _fields.Count; i++)
                {
                    // A field standing over several values becomes those values again, in the order its description
                    // declared them: the other half of showing three numbers as one color.
                    if (_wanted[i].Edits == Editor.Colour)
                    {
                        Channels.Spell(_fields[i].Text, _wanted[i].Covers, _parts);
                        while (_texts.Count < _parts.Count)
                            _texts.Add(false);

                        continue;
                    }

                    // What the program holds for the word chosen, which is not the word wherever a block shows one
                    // thing and is told another.
                    _parts.Add(_wanted[i].Meaning(_fields[i].Text));
                    _texts.Add(_wanted[i].Entry == Entry.Text);
                }

                if (_parts.Count == 0)
                    return "";

                // Text is quoted whatever it spells, so a 42 typed where text is wanted stays text.
                if (_parts.Count == 1)
                    return _texts[0] && _parts[0].Length > 0 ? Quoted(_parts[0], true) : _parts[0];

                var last = -1;
                for (var i = 0; i < _parts.Count; i++)
                {
                    if (!string.IsNullOrEmpty(_parts[i]))
                        last = i;
                }

                var said = "";
                for (var i = 0; i <= last; i++)
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
                    return Template.Fill(_language.ValueNeeded, "name", Called(0));

                return "";
            }
        }

        /// <summary>Whether a value that has to be given has none.</summary>
        public bool Missing
        {
            get
            {
                for (var i = 0; i < _fields.Count; i++)
                {
                    if (_wanted[i].Needed && string.IsNullOrEmpty(_fields[i].Text))
                        return true;
                }

                return false;
            }
        }

        /// <summary>Whether every value holds something it takes, so there is nothing left to give.</summary>
        public bool Complete
        {
            get
            {
                for (var i = 0; i < _fields.Count; i++)
                {
                    var said = _fields[i].Text;
                    if (said.Length == 0 || _wanted[i].Refuses(_language, Called(i), said).Length > 0)
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
                    if (!string.IsNullOrEmpty(_fields[i].Text))
                        return true;
                }

                return false;
            }
        }

        /// <summary>What a value is known by in <see cref="Spell" />: its name, or its place for a value nobody named.</summary>
        public static string Key(Field field, int at)
        {
            return string.IsNullOrEmpty(field.Name) ? "#" + Ordinal(at) : field.Name;
        }

        /// <summary>Asks for <paramref name="fields" />, one line each, holding <paramref name="filled" /> or what each says is usual, its notes in <paramref name="texts" />.</summary>
        /// <remarks>
        /// Made again whole, because it is asked only when what the values are for changes. <paramref name="colors" /> are
        /// the player's own, offered on any color among them.
        /// </remarks>
        public void Show(IList<Field> fields, IList<string> filled, IList<Hsv> colors, Texts texts)
        {
            _language = texts;
            for (var i = 0; i < _placed.Count; i++)
                Cells[_placed[i].Row, _placed[i].Column, 1, _placed[i].Columns] = null;

            _placed.Clear();
            _wanted.Clear();
            _fields.Clear();
            _seen.Clear();
            _judged = false;

            for (var i = 0; i < fields.Count; i++)
                _wanted.Add(fields[i]);

            for (var i = 0; i < _wanted.Count; i++)
            {
                var field = Make(_wanted[i], colors, texts);
                _fields.Add(field);
                _seen.Add(false);

                var said = i < filled.Count ? filled[i] ?? "" : "";

                // A value somebody has not given yet starts at what it says is usual, where it says.
                if (said.Length == 0 && !string.IsNullOrEmpty(_wanted[i].Default))
                    said = _wanted[i].Default;

                field.Text = _wanted[i].Shown(said);
                field.Edited += Changed;

                Lay(i, field);
            }
        }

        /// <summary>Finishes the form on Enter, while it or a part of it that does not take Enter holds the keyboard.</summary>
        public override bool OnKey(InputRoot input, MyKeys key, bool shift, bool ctrl)
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
                if (said.Length > 0 && _wanted[i].Refuses(_language, Called(i), said).Length == 0)
                    continue;

                first = i;
                break;
            }

            _seen[first] = true;

            var keyboard = _fields[first].Keyboard;
            if (input == null || keyboard == null)
                return;

            input.Focus(keyboard);

            // With what is in it taken, so typing replaces it: somebody who moves to a value means to say something else.
            var box = keyboard as TextBox;
            if (box != null)
                box.SelectAll();
        }

        /// <summary>What each value holds, as its field spells it, by the value it is: see <see cref="Key" />.</summary>
        /// <remarks>By name, so what was given survives the values changing under it: a hinge picked beside a rotor takes the direction away and leaves the angle.</remarks>
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
                _fields[i].IsWrong = Refuses(i).Length > 0 || blank && i == 0;
        }

        void Changed()
        {
            for (var i = 0; i < _fields.Count; i++)
            {
                // A value is seen once somebody has given it anything.
                if (_fields[i].Text.Length > 0)
                    _seen[i] = true;
            }

            Mark();

            var edited = Edited;
            if (edited != null)
                edited();
        }

        /// <summary>The field a value is given in: a switch for two words, a line of them for three, a list for more, a mixer, a dial, or a box.</summary>
        IField Make(Field field, IList<Hsv> colors, Texts texts)
        {
            if (field.Edits == Editor.Colour)
                return new ColorField(colors, _submit, texts);

            if (field.Edits == Editor.Angle)
                return new AngleField(field.Least, field.Most, _submit);

            var words = field.Edits == Editor.Chosen ? field.Words : null;
            if (words != null && words.Count == 2)
                return new SwitchField(words[0], words[1]);

            if (words != null && words.Count > 0 && words.Count <= Few)
                return new WordsField(words);

            if (words != null && words.Count > Few)
                return new ListField(words);

            return new TypedField(Bounded(field), _submit);
        }

        /// <summary>Puts a value's label and field on their line: a color across the whole of it, text and a few words across all the label leaves, anything else against the right.</summary>
        void Lay(int at, IField field)
        {
            var element = (Element)field;
            var color = _wanted[at].Edits == Editor.Colour;

            var label = color ? "" : Label(at);
            var about = _wanted[at].About;
            if (label.Length > 0)
            {
                var named = new Label { Style = Styles.ValueLabel, Text = label };
                if (!string.IsNullOrEmpty(about))
                    named.Tip = Tip.For(named.Tip, about);

                Place(named, at, 0, 1);
            }

            if (!string.IsNullOrEmpty(about))
                element.Tip = Tip.For(element.Tip, about);

            if (color)
                Place(element, at, 0, 3);
            else if (field is TypedField && !Bounded(_wanted[at]) || field is WordsField)
                Place(element, at, 1, 2);
            else
            {
                // Against the right edge whatever its width, so the fields of a form end together.
                element.Align = Align.Right;
                Place(element, at, 2, 1);
            }
        }

        void Place(Element element, int row, int column, int columns)
        {
            Cells[row, column, 1, columns] = element;
            _placed.Add(new Spot { Row = row, Column = column, Columns = columns });
        }

        /// <summary>Whether a typed value says the longest it may be, which makes its box a number's width.</summary>
        static bool Bounded(Field field)
        {
            return Math.Max((field.Least ?? "").Length, (field.Most ?? "").Length) > 0;
        }

        /// <summary>What a value is called beside its field: its name where the field already shows what it takes, its label, or its place where there are several.</summary>
        string Label(int at)
        {
            var edits = _wanted[at].Edits;
            if (edits == Editor.Chosen || edits == Editor.Angle)
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
        /// <remarks>A value nobody has reached is not called wrong for being empty; it still holds the form back through <see cref="Missing" />.</remarks>
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

        /// <summary>One value's text, quoted where it would otherwise read as more than one value, a doubled quote standing for one.</summary>
        static string Quoted(string said, bool always)
        {
            if (said == null)
                return "\"\"";

            if (!always && said.IndexOf(' ') < 0 && said.IndexOf('"') < 0)
                return said;

            return "\"" + said.Replace("\"", "\"\"") + "\"";
        }

        /// <summary>Where a label or a field was put, so it can be emptied again.</summary>
        struct Spot
        {
            public int Row;
            public int Column;
            public int Columns;
        }
    }
}
