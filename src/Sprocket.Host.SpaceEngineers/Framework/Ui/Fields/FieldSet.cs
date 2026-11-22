using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Language.Reading;
using Sprocket.Host.SpaceEngineers.Framework.Language.Writing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Text;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Lumel;
using Lumel.Controls;
using Lumel.Images;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using VRage.Input;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Fields
{
    /// <summary>The values something takes, one line each: what the value is, and where it is given.</summary>
    public partial class FieldSet : Form
    {
        // Past this many a list gets a row of its own, since its longest names need the room.
        const int Long = 20;

        const int Few = 3;

        readonly List<Field> _wanted = new List<Field>();
        readonly List<IField> _fields = new List<IField>();
        readonly List<bool> _seen = new List<bool>();
        readonly List<string> _parts = new List<string>();
        readonly List<bool> _texts = new List<bool>();
        readonly List<bool> _raws = new List<bool>();
        readonly List<string> _settings = new List<string>();
        readonly List<Literal> _held = new List<Literal>();
        readonly List<string> _heldShown = new List<string>();
        readonly List<Element> _rows = new List<Element>();
        readonly List<Extra> _extras = new List<Extra>();
        InputRoot _input;
        readonly List<SettingChip> _chips = new List<SettingChip>();
        readonly List<Glyph> _marks = new List<Glyph>();
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

            _submit = new RelayCommand(() => Submitted?.Invoke());
        }

        /// <summary>Whether Enter in the last value finishes the form; where not, Enter only moves on, for values given among other choices still to make.</summary>
        public bool FinishesOnEnter { get; set; } = true;

        /// <summary>Raised when somebody has changed any of the values.</summary>
        public event Action Edited;

        /// <summary>Raised when Enter is pressed in a field, which is finishing the form.</summary>
        public event Action Submitted;

        /// <summary>Raised with the field when the slots a value names are to be chosen on a selector.</summary>
        public event Action<SlotsField> Selects;

        /// <summary>Raised with the field when text written over several lines is to be written on a page of its own.</summary>
        public event Action<LinesField> Opens;

        /// <summary>Raised with the place of a value and its mark when where the value comes from is to be chosen: a value of its own or a setting.</summary>
        public event Action<int, Element> Sourcing;

        /// <summary>Whether a value asked for by a field may come from a setting, which gives it a mark to choose that by; none may where it is null.</summary>
        /// <remarks>Read as the values are shown, so it is set before <see cref="Show" />.</remarks>
        public Func<Field, bool> Sourced { get; set; }

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
                _raws.Clear();
                for (var i = 0; i < _fields.Count; i++)
                {
                    // A setting standing in for the value is written as the language writes it, as one value.
                    if (Asked(i) && _settings[i].Length > 0)
                    {
                        _parts.Add(ScriptWriter.KindSpelling(Kinds.Setting, _settings[i]));
                        _texts.Add(false);
                        _raws.Add(true);
                        continue;
                    }

                    // An expression the field still shows is written as the language writes it, as one value.
                    if (Asked(i) && Holds(i))
                    {
                        _parts.Add(_held[i].Tree.Written);
                        _texts.Add(false);
                        _raws.Add(true);
                        continue;
                    }

                    if (_wanted[i].Edits == FieldEditor.Color)
                    {
                        ColorChannels.Spell(Asked(i) ? _fields[i].Text : "", _wanted[i].Covers, _parts);
                        while (_texts.Count < _parts.Count)
                            _texts.Add(false);

                        while (_raws.Count < _parts.Count)
                            _raws.Add(false);

                        continue;
                    }

                    // A member is given every argument, so an empty field is written as its default.
                    var text = Asked(i) ? _fields[i].Text : "";
                    if (_wanted[i].Fixed)
                        text = _wanted[i].Default;

                    if (string.IsNullOrEmpty(text) && Asked(i) && _wanted[i].Default != null)
                        text = _wanted[i].Default;

                    _parts.Add(_wanted[i].Meaning(text));
                    _texts.Add(_wanted[i].Entry == FieldInput.Text);
                    _raws.Add(false);
                }

                if (_parts.Count == 0)
                    return "";

                // Text is quoted whatever it spells, so a 42 typed where text is wanted stays text.
                if (_parts.Count == 1)
                    return _raws[0] ? _parts[0] : _texts[0] && _parts[0].Length > 0 ? Quoted(_parts[0], true) : _parts[0];

                if (_parts.TrueForAll(string.IsNullOrEmpty))
                    return "";

                var said = "";
                for (var i = 0; i < _parts.Count; i++)
                {
                    if (said.Length > 0)
                        said += " ";

                    said += _raws[i] ? _parts[i] : Quoted(_parts[i], _texts[i] && _parts[i].Length > 0);
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
                    if (Asked(i) && _settings[i].Length == 0 && string.IsNullOrEmpty(_fields[i].Text) && !_wanted[i].IsOptional)
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
                    if (Asked(i) && _settings[i].Length == 0 && !Holds(i) && (said.Length == 0 || _wanted[i].Refuses(_language, Called(i), said).Length > 0))
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
                    if (Asked(i) && (_settings[i].Length > 0 || !string.IsNullOrEmpty(_fields[i].Text)))
                        return true;
                }

                return false;
            }
        }

        /// <summary>The word standing in a line for the <paramref name="at" />th expression held while the line is read.</summary>
        static string Held(int at)
        {
            return $"~held{at.ToString(CultureInfo.InvariantCulture)}~";
        }

        /// <summary>What a value is known by in <see cref="Spell" />: its name, or its place for a value nobody named.</summary>
        public static string Key(Field field, int at)
        {
            return string.IsNullOrEmpty(field.Name) ? $"#{Ordinal(at)}" : field.Name;
        }

        /// <summary>
        /// The values <paramref name="said" />, written as a line of values, gives for <paramref name="fields" />: a word chosen
        /// where a name is wanted as that name, and everything else as the line reads.
        /// </summary>
        /// <remarks>The answer comes as a line of text, which carries a chosen word as a bare word, and a bare word reads as text.</remarks>
        public static List<Literal> Read(string said, IList<Field> fields, IList<Literal> held = null)
        {
            // An expression the line still says as it was written is one value, which the words of a line would split.
            var expressions = new List<Literal>();
            for (var i = 0; held != null && i < held.Count; i++)
            {
                if (held[i].IsExpression && said.Contains(held[i].Tree.Written))
                {
                    said = said.Replace(held[i].Tree.Written, Held(expressions.Count));
                    expressions.Add(held[i]);
                }
            }

            var values = new List<Literal>();
            ValueLine.Read(said, values);
            for (var i = 0; i < values.Count; i++)
            {
                for (var k = 0; k < expressions.Count; k++)
                {
                    if (values[i].Text == Held(k) && values[i].Kind != ValueKind.Slots)
                        values[i] = expressions[k];
                }
            }

            var at = 0;
            for (var f = 0; f < fields.Count && at < values.Count; f++)
            {
                if (!fields[f].TakesSlots && fields[f].Entry == FieldInput.Name && values[at].Kind == ValueKind.Text)
                    values[at] = Literal.Name(values[at].Text);

                Duration length;
                if (fields[f].Edits == FieldEditor.Duration && values[at].Kind == ValueKind.Text && DurationField.Read(values[at].Text, out length))
                    values[at] = Literal.Of(length);

                at += fields[f].Covers;
            }

            return values;
        }

        /// <summary>
        /// Shows <paramref name="element" /> under <paramref name="label" /> after the value at <paramref name="at" />, each time the values
        /// are shown; it is not one of the values, and gives none.
        /// </summary>
        public void After(int at, string label, Element element)
        {
            _extras.Add(new Extra(at, label, element));
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
            _settings.Clear();
            _held.Clear();
            _heldShown.Clear();
            _rows.Clear();
            _chips.Clear();
            _marks.Clear();
            _judged = false;
            _offered = false;

            for (var i = 0; i < fields.Count; i++)
                _wanted.Add(fields[i]);

            for (var i = 0; i < _wanted.Count; i++)
            {
                var field = Make(_wanted[i], colors, texts);
                _fields.Add(field);
                _seen.Add(false);
                _settings.Add("");
                _held.Add(default(Literal));
                _heldShown.Add("");

                var said = i < filled.Count ? filled[i] ?? "" : "";

                if (said.Length == 0 && !string.IsNullOrEmpty(_wanted[i].Default))
                    said = _wanted[i].Default;

                field.Text = _wanted[i].Shown(said);
                field.Edited += Changed;

                Lay(i, field);
                LayExtras(i);
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

        /// <summary>Shows beside the number at <paramref name="at" /> the unit the field it follows decides, where it follows one.</summary>
        void Measure(int at)
        {
            var wanted = _wanted[at];
            var number = _fields[at] as NumberField;
            if (wanted.UnitOf == null || number == null)
                return;

            for (var i = 0; i < _wanted.Count; i++)
            {
                if (_wanted[i].Name == wanted.UnitFollows)
                    number.Unit = wanted.UnitOf(_wanted[i].Meaning(_fields[i].Text), false);
            }
        }

        /// <summary>Shows each value that is asked for as things stand, and hides the rest, keeping what they hold.</summary>
        void Follow()
        {
            for (var i = 0; i < _fields.Count; i++)
            {
                _rows[i].IsVisible = Asked(i) && !_wanted[i].Fixed;
                if (_rows[i] != _fields[i])
                    ((Element)_fields[i]).IsVisible = _settings[i].Length == 0;
                if (_chips[i] != null)
                    _chips[i].IsVisible = _settings[i].Length > 0;

                if (_marks[i] != null)
                    _marks[i].Set(LookStates.Chosen, _settings[i].Length > 0);
                Measure(i);
            }
        }

        /// <summary>Says the values it holds are what the block holds now, offered rather than given, until somebody changes one.</summary>
        public void Offer()
        {
            _offered = true;
        }

        /// <summary>Moves on to the next value on Enter, or finishes the form from the last, while it or a part of it that does not take Enter holds the keyboard, and no list of it is open.</summary>
        protected override bool OnKeyDown(InputRoot input, MyKeys key, bool shift, bool ctrl)
        {
            if (key != MyKeys.Enter || IsListOpen())
                return false;

            _input = input;
            Enter();
            return true;
        }

        // An open list's Enter chooses what its search found, and reaches the form on its way up from the list.
        bool IsListOpen()
        {
            for (var i = 0; i < _fields.Count; i++)
            {
                var list = _fields[i] as DropDown;
                if (list != null && list.IsOpen)
                    return true;
            }

            return false;
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
            _input = input;
            if (_fields.Count == 0)
                return;

            var first = 0;
            for (var i = 0; i < _fields.Count; i++)
            {
                var said = _fields[i].Text;
                if (!Asked(i) || _settings[i].Length > 0 || Holds(i) || said.Length > 0 && _wanted[i].Refuses(_language, Called(i), said).Length == 0)
                    continue;

                first = i;
                break;
            }

            _seen[first] = true;
            Give(first);
        }

        /// <summary>Gives the keyboard to the field of the value at <paramref name="at" />, its text chosen where it is typed into.</summary>
        void Give(int at)
        {
            var keyboard = _fields[at].Keyboard;
            if (_input == null || keyboard == null)
                return;

            _input.Focus(keyboard);

            var box = keyboard as TextBox;
            if (box != null)
                box.SelectAll();
        }

        /// <summary>Moves the keyboard on to the next value asked for after the one holding it, and from the last finishes the form where it <see cref="FinishesOnEnter" />.</summary>
        void Enter()
        {
            var at = Keyed();
            for (var next = at + 1; at >= 0 && next < _fields.Count; next++)
            {
                if (!Asked(next) || _wanted[next].Fixed || _settings[next].Length > 0 || !Takes(next))
                    continue;

                _seen[next] = true;
                Give(next);
                return;
            }

            if (FinishesOnEnter)
                Submitted?.Invoke();
        }

        /// <summary>Whether the field of the value at <paramref name="at" /> is shown and can take the keyboard.</summary>
        bool Takes(int at)
        {
            var keyboard = _fields[at].Keyboard;
            return keyboard != null && keyboard.Focusable && keyboard.IsEnabled && _rows[at].IsVisible;
        }

        /// <summary>The value whose field holds the keyboard, or -1 where none does.</summary>
        int Keyed()
        {
            var focused = _input?.Focused;
            for (var i = 0; focused != null && i < _fields.Count; i++)
            {
                for (var at = focused; at != null; at = at.Parent)
                {
                    if (at == _fields[i].Keyboard || at == (Element)_fields[i])
                        return i;
                }
            }

            return -1;
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

        /// <summary>The field a value is given in: a switch for on or off, a line of words for a few others, a list for more, a mixer, a dial, a length, a box of several lines, a slider for a bounded number, a number's box, or a box.</summary>
        IField Make(Field field, ColorOffer colors, Texts texts)
        {
            if (field.Edits == FieldEditor.Color)
                return new ColorField(colors, texts);

            if (field.Edits == FieldEditor.Angle)
                return new AngleField(field.Least, field.Most, null);

            if (field.Edits == FieldEditor.Duration)
                return new DurationField(texts, null);

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

            if (field.TakesSlots)
            {
                var slots = new SlotsField(texts.PickSelectSlots, MarkText.OfSlot(false, HostTypes.Named(field.SlotsHold))) { Label = field.Name ?? "" };
                slots.Selecting += selecting => Selects?.Invoke(selecting);
                return slots;
            }

            var words = field.Edits == FieldEditor.Chosen ? field.Words : null;
            if (field.IsOnOff)
                return new SwitchField(field.Words[0], field.Words[1], _submit);

            if (words != null && words.Count > 0 && (field.Searched || words.Count > Long))
                return new ListField(words, texts.PickSearch);

            if (words != null && words.Count > 0 && words.Count <= Few)
                return new WordsField(words, _submit);

            if (words != null && words.Count > Few)
                return new ListField(words);

            if (SliderField.Takes(field))
                return new SliderField(field, null);

            return NumberField.Takes(field) ? (IField)new NumberField(field, null) : new TypedField(null) { Longest = field.Longest, Hint = field.Hint };
        }

        /// <summary>Puts a value's label and field in a row: a color alone across the whole width, text under its label, anything else beside it where it fits.</summary>
        bool IsLong(int at, IField field)
        {
            return field is ListField && (_wanted[at].Searched || _wanted[at].Words.Count > Long);
        }

        /// <summary>Lays out each element <see cref="After" /> shows after the value at <paramref name="at" />.</summary>
        void LayExtras(int at)
        {
            foreach (var extra in _extras)
            {
                if (extra.At == at)
                    Rows.Add(new Label { Style = Styles.ValueLabel, Text = extra.Label }, extra.Element, FormLabelPlacement.Above);
            }
        }

        void Lay(int at, IField field)
        {
            var element = (Element)field;
            var color = _wanted[at].Edits == FieldEditor.Color;

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

                Rows.Add(named, Sourceable(at, element), field is TypedField || field is LinesField || field is SliderField || _wanted[at].Stacked || IsLong(at, field) ? FormLabelPlacement.Above : FormLabelPlacement.Auto);
                return;
            }

            var row = Sourceable(at, element);
            if (!color && !(field is TypedField) && !(field is LinesField) && !(field is SliderField) && !(field is WordsField))
                row.Align = Align.Right;

            Rows.Add(row);
        }

        /// <summary>
        /// <paramref name="field" />, the field of the value at <paramref name="at" />, with the mark after it that chooses where the value
        /// comes from, and the chip that stands in its place while a setting gives it, where the value may come from one; the field
        /// alone otherwise.
        /// </summary>
        Element Sourceable(int at, Element field)
        {
            if (Sourced == null || !Sourced(_wanted[at]))
            {
                _rows.Add(field);
                _chips.Add(null);
                _marks.Add(null);
                return field;
            }

            var chip = new SettingChip { IsVisible = false };
            var mark = new Glyph { Style = Styles.Source, Symbol = Symbols.Link, Tip = CodexTip.For(null, _language.ValueSourceAbout) };
            mark.Command = new RelayCommand(() => Sourcing?.Invoke(at, mark));

            // The field, or the chip in its place, takes the room the mark leaves, as the field alone took the whole row.
            if (field.Placement == null)
                field.Placement = Flex.Grow;

            chip.Placement = Flex.Grow;
            var row = new HStack { Base = { Spacing = Gaps.Parts }, Children = { field, chip, mark } };
            _rows.Add(row);
            _chips.Add(chip);
            _marks.Add(mark);
            return row;
        }

        /// <summary>Has the setting <paramref name="setting" /> give the value at <paramref name="at" />, its default said as <paramref name="said" />, or the value its own again where <paramref name="setting" /> is empty.</summary>
        public void Choose(int at, string setting, string said)
        {
            if (at < 0 || at >= _settings.Count || _chips[at] == null)
                return;

            _settings[at] = setting ?? "";
            _chips[at].Setting = _settings[at];
            _chips[at].Default = string.IsNullOrEmpty(said) ? "" : Template.Fill(_language.ValueByDefault, "value", said);
            Changed();
        }

        /// <summary>
        /// Has the value at <paramref name="at" /> hold <paramref name="expression" />, its field saying it as
        /// <paramref name="shown" />, which passes as it stands.
        /// </summary>
        /// <remarks>An expression is no value its field's editor takes, so it is held to nothing while the field still says it; typing over it makes it a value like any other.</remarks>
        public void Hold(int at, Literal expression, string shown)
        {
            if (at < 0 || at >= _held.Count || !expression.IsExpression)
                return;

            _held[at] = expression;
            _heldShown[at] = shown;
            _fields[at].Text = shown;
            Changed();
        }

        /// <summary>The expression the value at <paramref name="at" /> holds while its field still says it, or none.</summary>
        public Literal HeldAt(int at)
        {
            return at >= 0 && at < _held.Count && Holds(at) ? _held[at] : default(Literal);
        }

        /// <summary>Has the value at <paramref name="at" /> be <paramref name="said" />, its own, as its field spells it.</summary>
        public void Write(int at, string said)
        {
            if (at < 0 || at >= _fields.Count)
                return;

            Choose(at, "", "");
            _fields[at].Text = said ?? "";
            Changed();
        }

        /// <summary>What the value at <paramref name="at" /> is called beside its field, or empty where nothing is.</summary>
        public string LabelOf(int at)
        {
            return at >= 0 && at < _wanted.Count ? Label(at) : "";
        }

        /// <summary>The expressions the fields hold and still say, to read a line of their words back with.</summary>
        public List<Literal> Holding()
        {
            var holding = new List<Literal>();
            for (var i = 0; i < _held.Count; i++)
            {
                if (Holds(i))
                    holding.Add(_held[i]);
            }

            return holding;
        }

        /// <summary>Whether the field at <paramref name="at" /> still says the expression it was given to hold.</summary>
        bool Holds(int at)
        {
            return _held[at].IsExpression && _fields[at].Text == _heldShown[at];
        }

        /// <summary>The mark choosing where the value at <paramref name="at" /> comes from, or null where it may come from nowhere else.</summary>
        public Glyph MarkOf(int at)
        {
            return _marks[at];
        }

        /// <summary>The field asking for the value at <paramref name="at" />.</summary>
        public Field Field(int at)
        {
            return _wanted[at];
        }

        /// <summary>What the value at <paramref name="at" /> holds of its own, as its field spells it.</summary>
        public string ValueAt(int at)
        {
            return _fields[at].Text;
        }

        /// <summary>The setting giving the value at <paramref name="at" />, or empty where the value is its own.</summary>
        public string SettingAt(int at)
        {
            return at >= 0 && at < _settings.Count ? _settings[at] : "";
        }

        /// <summary>What a value is called beside its field: its name where the field already shows what it takes, its label, or its place where there are several.</summary>
        string Label(int at)
        {
            var edits = _wanted[at].Edits;
            if (edits == FieldEditor.Chosen || edits == FieldEditor.Angle || SliderField.Takes(_wanted[at]))
                return _wanted[at].Title(_language);

            var said = _wanted[at].Label(_language);
            if (said.Length > 0)
                return said;

            return _wanted.Count > 1 ? Ordinal(at) : "";
        }

        /// <summary>What a refusal calls a value: its name, or its place for a value nobody named.</summary>
        string Called(int at)
        {
            var name = _wanted[at].Title(_language);
            return name.Length > 0 ? name : Template.Fill(_language.ValueNumbered, "number", Ordinal(at));
        }

        /// <summary>Why the value at <paramref name="at" /> will not do, or empty, where it is being held to anything at all.</summary>
        string Refuses(int at)
        {
            if (!_judged || _settings[at].Length > 0 || Holds(at))
                return "";

            var said = _fields[at].Text;
            if (!_seen[at] && string.IsNullOrEmpty(said))
                return "";

            return _wanted[at].Refuses(_language, SentenceText.Capitalised(Called(at)), said);
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

                TextEntities.Write(quoted, said, i);
            }

            return quoted.Append('"').ToString();
        }
    }
}
