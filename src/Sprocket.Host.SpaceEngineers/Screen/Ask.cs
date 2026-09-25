using System;
using System.Collections.Generic;
using System.Globalization;
using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Drawing;
using Sprocket.Host.SpaceEngineers.Look;
using Sprocket.Host.SpaceEngineers.Ui.Text;
using Sprocket.Host.SpaceEngineers.Layout;
using VRageMath;
using Sprocket.Host.SpaceEngineers.Controls;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Host.SpaceEngineers.Ui.Legacy;
using TextBox = Sprocket.Host.SpaceEngineers.Ui.Legacy.TextBox;

namespace Sprocket.Host.SpaceEngineers.Screen
{
    /// <summary>
    /// A question hanging over the screen, under whatever was pressed to raise it.
    /// </summary>
    /// <remarks>
    /// A popup rather than a panel in the flow: it hangs over the screen instead of shoving it down,
    /// and it is anchored to whatever opened it. Core knows none of that and should not.
    /// <para>
    /// It owns what it looks like and where it sits, and nothing else. Whether the answer is any good,
    /// and who to send it to, belong to whoever raised it.
    /// </para>
    /// </remarks>
    public sealed class Ask
    {
        private readonly Stack _popup = Widgets.Column(Space.Item);
        private readonly Label _label = Widgets.Label("", Palette.Muted, Lettering.Tiny);
        private readonly Bar _entry = new Bar();
        private readonly Stack _fields = Widgets.Column(Space.Item);
        private readonly List<TextBox> _boxes = new List<TextBox>();
        private readonly List<Mixer> _mixers = new List<Mixer>();
        private readonly List<Dial> _dials = new List<Dial>();
        private readonly List<Switch> _switches = new List<Switch>();
        private readonly List<Stack> _holders = new List<Stack>();
        private readonly List<Choices> _lists = new List<Choices>();
        private readonly List<Hsv> _colours = new List<Hsv>();
        private readonly List<string> _parts = new List<string>();

        /// <summary>Which of <see cref="_parts" /> are text, and so quoted whatever they spell.</summary>
        private readonly List<bool> _texts = new List<bool>();
        private readonly List<Bar> _lines = new List<Bar>();
        private readonly List<Label> _numbers = new List<Label>();
        private readonly Label _confirm = Widgets.Label("Continue", Palette.Ink, Lettering.Small);
        private readonly Label _cancel = Widgets.Label("Cancel", Palette.Muted, Lettering.Small);

        /// <summary>Why what is filled in cannot be sent, beside the way out rather than over the question.</summary>
        private readonly Label _why = Widgets.Label("", Palette.Problem, Lettering.Small);

        /// <summary>Which fields the caret has been in, so an empty one is not called wrong before it is reached.</summary>
        private readonly List<bool> _seen = new List<bool>();

        private readonly Sized _form = new Sized(FormWidth);

        private Element _anchor;
        private int _command;
        private string _question = "";
        private bool _up;
        private int _at;

        /// <summary>Whether this is a column of a screen rather than a question hanging over one.</summary>
        private readonly bool _column;

        /// <summary>Whether a field has the keyboard, which a question always has and a column only once a field is pressed.</summary>
        private bool _typing = true;

        /// <summary>Whether what is filled in is being held to what it takes, which a column only is once finishing it has been tried.</summary>
        private bool _judged = true;

        public Ask() : this(false, FormWidth)
        {
        }

        /// <summary>
        /// The fields as a column of a screen, as wide as <paramref name="width" />, finished by its own Done.
        /// </summary>
        /// <remarks>
        /// Always there rather than raised, so it takes the keyboard only when one of its fields is
        /// pressed, and holds nothing against what is filled in until finishing it is tried. Its parts
        /// are laid out by the column holding it - see <see cref="Form" />, <see cref="Done" /> and
        /// <see cref="Why" />.
        /// </remarks>
        public static Ask Column(float width)
        {
            return new Ask(true, width);
        }

        Ask(bool column, float width)
        {
            _column = column;
            _form.Width = width;

            if (column)
            {
                _typing = false;
                _judged = false;
                _confirm.Text = "Done";
            }

            var ways = Widgets.RowOf(Space.Item);
            if (!column)
            {
                ways.Children.Add(_cancel);
                ways.Children.Add(_confirm);
            }

            _cancel.Padding = new Thickness(Space.ChipX, Space.ChipY);
            _cancel.Press = Deed.Cancels();

            // Lit, because everything a press reaches has to be - and quietly, because it is the way
            // out rather than the way on.
            _cancel.Highlight = Palette.Panel;

            _entry.Spacing = Space.Item;
            _entry.Right = ways;
            _why.Padding = new Thickness(Space.ChipX, Space.ChipY);
            _why.Wraps = true;

            _popup.Background = Palette.PanelHot;
            _popup.Edge = Palette.Line;
            _popup.Padding = new Thickness(Space.Region, Space.Item);
            _form.Content = _fields;

            // The form above the line that finishes it, so reading down the dialog is the order it
            // is filled in: what it wants, the fields, then the way out. A column lays its fields, its
            // Done and its reason out itself.
            if (!column)
            {
                _popup.Children.Add(_label);
                _popup.Children.Add(_form);
                _popup.Children.Add(_entry);
            }

            Fields(1, false);
        }

        /// <summary>The fields, as a column lays them out.</summary>
        public Element Form
        {
            get { return _form; }
        }

        /// <summary>The way to finish, as a column lays it out.</summary>
        public Element Done
        {
            get { return _confirm; }
        }

        /// <summary>Why nothing can be finished yet, as a column lays it out.</summary>
        public Element Why
        {
            get { return _why; }
        }

        /// <summary>Whether one of the fields has the keyboard.</summary>
        public bool Typing
        {
            get { return _typing; }
        }

        /// <summary>Whether the way to finish can be pressed. Only a column is told.</summary>
        public bool Finishable { get; set; }

        /// <summary>
        /// Gives the question one field per value it is asking for, side by side.
        /// </summary>
        /// <remarks>
        /// One field for three values means somebody typing three things into one box and a
        /// separator to get wrong. Side by side rather than stacked because they are one answer in
        /// several parts - a colour's three numbers - and a column of them reads as three questions.
        /// <para>
        /// The boxes are kept and reused. A question opens when somebody presses something and lives
        /// for one answer, and the count almost never changes between two of them.
        /// </para>
        /// </remarks>
        /// <summary>
        /// Lays it out as a name or as a form, and empties every field.
        /// </summary>
        /// <remarks>
        /// A name is one box on the question's own line with the way on beside it, which is what
        /// this has always been. A form is a column of fields under the question with a way out as
        /// well, and it is that whether it asks for one value or six - the two are told apart by
        /// what is being asked for, never by how many there are.
        /// </remarks>
        void Fields(int count, bool form)
        {
            while (_boxes.Count < count)
                _boxes.Add(Widgets.Box(Lettering.Small, Palette.Page));

            _fields.Children.Clear();
            for (var i = 0; i < count; i++)
            {
                // A field with a control of its own puts that control in instead of a line, and the
                // box it is answered in belongs to the control rather than to the row of boxes.
                if (Colourful(i))
                {
                    Mixed(i).Written.Text = "";
                    _fields.Children.Add(Mixed(i).Root);
                    continue;
                }

                if (Dialled(i))
                    Turned(i).Written.Text = "";
                else
                    _boxes[i].Text = "";

                _fields.Children.Add(Line(i, count));
            }

            // The reason sits on the same line as the way out, where somebody who has just pressed a
            // dead button is looking. A name question has its box there instead.
            _entry.Fill = _column ? null : form ? (Element)_why : _boxes[0];

            _seen.Clear();
            _form.Collapsed = !form;

            // The way out is the form's, and not every question's. A name is left by pressing away
            // or by Escape, the way it always has been, and a Cancel beside it would take width
            // from the box somebody is typing in. A column is left by pressing anything else.
            _cancel.Collapsed = !form || _column;

            // The caret belongs to the field it is in, and to no other. Every box editing meant
            // every box drawing one, with only the real one blinking - which reads as three carets
            // rather than as one place to type.
            for (var i = 0; i < _boxes.Count; i++)
                _boxes[i].IsEditing = false;

            for (var i = 0; i < _mixers.Count; i++)
                _mixers[i].Written.IsEditing = false;

            for (var i = 0; i < _dials.Count; i++)
                _dials[i].Written.IsEditing = false;

            _at = 0;
            _measure = true;
            Editing();
        }

        /// <summary>
        /// How wide the form is, in screen units.
        /// </summary>
        /// <remarks>
        /// A width of its own, because the fields in it are empty when it opens and a box is as
        /// wide as what is in it: sized to their contents they would open as slivers nobody would
        /// take for somewhere to type. Wide enough for a sentence, since a value can be text.
        /// </remarks>
        const float FormWidth = 520f;

        /// <summary>
        /// How wide every number box in a column is: a sign, six digits, a point and two places, and
        /// the caret after them.
        /// </summary>
        const float Numbers = 11f * Sizes.Digit;

        /// <summary>
        /// The least room a label takes, in screen units, before anything is measured.
        /// </summary>
        /// <remarks>
        /// Enough for a digit, which is what a value nobody has named shows. A named one is wider
        /// than this and says so - see <see cref="Inset" />.
        /// </remarks>
        const float LeastLabel = 28f;

        /// <summary>
        /// One line of the form: which value it is, and the box for it.
        /// </summary>
        /// <remarks>
        /// A line each rather than side by side. Side by side reads as several parts of one thing -
        /// a colour's three channels - and that is a claim about what the values are for, which is
        /// the one thing the game never says: an action declares how many it takes and nothing
        /// else. A column says only what is true, which is that there are this many and they are in
        /// this order. It also takes any number of them, where a row of five would not.
        /// <para>
        /// Numbered for the same reason. The number is the whole of what can honestly be put beside
        /// a field, so it is put there quietly and nothing else is invented.
        /// </para>
        /// </remarks>
        Bar Line(int at, int count)
        {
            while (_lines.Count <= at)
            {
                _lines.Add(new Bar());
                _numbers.Add(Widgets.Label("", Palette.Faint, Lettering.Small));
            }

            // What it is called and what it will take, where anybody has said, and its place in
            // the order otherwise. A value nobody has described has only its place to say, and that
            // is worth saying where there are several and worth nothing where there is one.
            _numbers[at].Text = Label(at, count);

            Bar line = _lines[at];
            line.Spacing = Space.Item;
            line.Left = _numbers[at].Text.Length > 0 ? Numbered(at) : null;
            if (Dialled(at))
                Turned(at).Reach(_wanted[at].Least, _wanted[at].Most);

            Element control = Dialled(at) ? Turned(at).Root : Chosen(at) ? Picking(at) : Boxed(at);

            // A column puts its controls against its right-hand edge, the edge its Done is on, so they
            // read as one line down the column whatever the labels beside them say. A box for text
            // still takes whatever the label leaves.
            if (_column && !Stretches(at))
            {
                line.Fill = null;
                line.Right = control;
            }
            else
            {
                line.Right = null;
                line.Fill = control;
            }

            return line;
        }

        /// <summary>Whether a field's control takes the width its line has left rather than its own.</summary>
        bool Stretches(int at)
        {
            return !Dialled(at) && !Chosen(at) && Room(at) <= 0f;
        }

        /// <summary>
        /// The box a field is typed into, no wider than the longest value it will take.
        /// </summary>
        /// <remarks>
        /// A box given the width of the form invites a sentence, where a field bounded at thirty holds
        /// two digits. A field that says nothing about what it takes keeps the width, because anything
        /// could go in it.
        /// </remarks>
        Element Boxed(int at)
        {
            var wide = Room(at);
            if (wide <= 0f)
            {
                _boxes[at].Trails = false;
                return _boxes[at];
            }

            // A number sits against the right-hand end of a box made to its own width. In a column every
            // number box is one width, so the boxes under each other start as well as end together.
            _boxes[at].Trails = true;

            while (_sized.Count <= at)
                _sized.Add(new Sized(0f));

            _sized[at].Width = _column ? Numbers : wide;
            _sized[at].Content = _boxes[at];
            return _sized[at];
        }

        /// <summary>How much room the longest value a field will take needs, or nought where it says nothing.</summary>
        static float Room(Field field)
        {
            var longest = Math.Max((field.Least ?? "").Length, (field.Most ?? "").Length);
            if (longest < 1)
                return 0f;

            // The longest value it will take - its whole part, and a point and the places after it where
            // it takes a fraction - and room for the caret after it.
            var places = field.Entry == Entry.Decimal ? Field.Places + 1 : 0;
            return (longest + places + 1) * Sizes.Digit;
        }

        float Room(int at)
        {
            return at < _wanted.Count ? Room(_wanted[at]) : 0f;
        }

        readonly List<Sized> _sized = new List<Sized>();

        string Label(int at, int count)
        {
            // The name alone where the control beside it already shows what it will take. A switch
            // has both of its words written on it, and "On on or off" is the label saying them again;
            // a dial is a whole turn either way, drawn.
            if (Chosen(at) || Dialled(at))
                return _wanted[at].Name ?? "";

            string said = at < _wanted.Count ? _wanted[at].Label : "";
            if (said.Length > 0)
                return said;

            return count > 1 ? Ordinal(at) : "";
        }

        /// <summary>
        /// What a refusal calls the field it is about.
        /// </summary>
        /// <remarks>
        /// The name on its own and not the label, because the label carries the bounds as well and
        /// the sentence is about to say those itself. A value nobody named is its place in the
        /// order, which is what the words beside it say too.
        /// </remarks>
        string Called(int at)
        {
            string name = at < _wanted.Count ? _wanted[at].Name ?? "" : "";
            return name.Length > 0 ? name : "Value " + Ordinal(at);
        }

        static string Ordinal(int at)
        {
            return (at + 1).ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>The number, held at a width so the fields under each other line up.</summary>
        Sized Numbered(int at)
        {
            while (_held.Count <= at)
                _held.Add(new Sized(LeastLabel));

            _held[at].Content = _numbers[at];
            return _held[at];
        }

        readonly List<Sized> _held = new List<Sized>();

        /// <summary>
        /// Holds every label as wide as the widest of them, so the fields line up after it.
        /// </summary>
        /// <remarks>
        /// A label is a number where nobody has named the value and a word where somebody has, and
        /// a word does not fit the room a digit needs: at one width each, the longest name ran over
        /// the field beside it. So the inset is the widest label there is, and every field starts
        /// after it.
        /// <para>
        /// Measured once, when the question opens, rather than while it is drawn. A form lives for
        /// one answer and its labels do not change while it is up, so measuring per frame would be
        /// paying sixty times a second for an answer that cannot move.
        /// </para>
        /// </remarks>
        void Inset(IGraphics g, float width)
        {
            if (!_measure)
                return;

            _measure = false;

            var widest = LeastLabel;
            for (var i = 0; i < Count && i < _numbers.Count; i++)
            {
                _numbers[i].Measure(g, new Vector2(width, width));

                if (_numbers[i].DesiredSize.X > widest)
                    widest = _numbers[i].DesiredSize.X;
            }

            for (var i = 0; i < Count && i < _held.Count; i++)
                _held[i].Width = widest;
        }

        bool _measure;

        /// <summary>Whether a question is open.</summary>
        public bool IsUp
        {
            get { return _up; }
        }

        /// <summary>The question itself, for the screen to walk while it owns the screen.</summary>
        public Element Root
        {
            get { return _popup; }
        }

        /// <summary>The box being typed into, which is one of several while several are asked for.</summary>
        public TextBox Box
        {
            get { return At(_at); }
        }

        /// <summary>
        /// Where a field's answer is typed, which is not always one of this form's own boxes.
        /// </summary>
        /// <remarks>
        /// A field with a control of its own carries the box that control shows, so that typing, the
        /// caret and the answer all go where somebody is looking. Everything that reads or writes what
        /// is in a field goes through here rather than through the row of boxes.
        /// </remarks>
        TextBox At(int at)
        {
            if (Colourful(at))
                return Mixed(at).Written;

            return Dialled(at) ? Turned(at).Written : _boxes[at];
        }

        /// <summary>
        /// Whether the field at <paramref name="at" /> is answered by choosing one of two words.
        /// </summary>
        /// <remarks>
        /// Both halves of it: the field has to say it wants a switch and to say which two words the
        /// switch offers. A field that says the first and not the second has nothing to put on the
        /// two halves, and gets a box like anything else.
        /// </remarks>
        bool Chosen(int at)
        {
            return at < _wanted.Count && _wanted[at].Edits == Editor.Chosen
                && _wanted[at].Words != null && _wanted[at].Words.Count > 0;
        }

        /// <summary>
        /// Whether that field's few are exactly two, which is the one a switch can take.
        /// </summary>
        /// <remarks>
        /// A question about what fits rather than about the value: two words are a switch because a
        /// switch is what two mutually exclusive words look like everywhere in this game, and three
        /// are a row to pick from because a switch has nowhere to put the third.
        /// </remarks>
        bool Switched(int at)
        {
            return Chosen(at) && _wanted[at].Words.Count == 2;
        }

        /// <summary>
        /// The switch for a field, made the first time that field needs one.
        /// </summary>
        /// <remarks>
        /// Inside a row of its own, because a switch is as wide as its two words and the form's lines
        /// hand the middle whatever width is left. Given that width directly it would stretch across
        /// the dialog, which is not what a switch looks like anywhere else on these screens.
        /// </remarks>
        Element Picking(int at)
        {
            if (!Switched(at))
            {
                while (_lists.Count <= at)
                    _lists.Add(new Choices(Lettering.Small));

                _lists[at].Says(_wanted[at].Words);
                return _lists[at];
            }

            while (_switches.Count <= at)
            {
                var made = new Switch(Lettering.Small, true);
                Stack holder = Widgets.RowOf(0f);
                holder.Children.Add(made);

                _switches.Add(made);
                _holders.Add(holder);
            }

            _switches[at].Says(_wanted[at].Words[0], _wanted[at].Words[1]);
            return _holders[at];
        }

        /// <summary>Whether the field at <paramref name="at" /> is answered by choosing a colour.</summary>
        bool Colourful(int at)
        {
            return at < _wanted.Count && _wanted[at].Edits == Editor.Colour;
        }

        /// <summary>The colour control for a field, made the first time that field needs one.</summary>
        Mixer Mixed(int at)
        {
            while (_mixers.Count <= at)
                _mixers.Add(new Mixer());

            return _mixers[at];
        }

        /// <summary>Whether the field at <paramref name="at" /> is answered by turning a dial.</summary>
        bool Dialled(int at)
        {
            return at < _wanted.Count && _wanted[at].Edits == Editor.Angle;
        }

        /// <summary>The dial for a field, made the first time that field needs one.</summary>
        Dial Turned(int at)
        {
            while (_dials.Count <= at)
                _dials.Add(new Dial());

            return _dials[at];
        }

        /// <summary>How many values it is asking for.</summary>
        public int Count
        {
            get { return _fields.Children.Count; }
        }

        /// <summary>
        /// Moves the caret to the field under a press, and answers whether one was there.
        /// </summary>
        /// <remarks>
        /// So three fields can be filled in any order. Pressing the one that already has the caret
        /// is not a move, and is answered yes all the same - the press landed on a field either way.
        /// </remarks>
        public bool Touch(Vector2 point)
        {
            for (var i = 0; i < Count; i++)
            {
                // The control's own parts first: a swatch is a press that answers the field, and it
                // is not a place to type, so it must not also move the caret there and stop.
                if (Colourful(i) && Mixed(i).Touch(point))
                {
                    Enter(i);
                    return true;
                }

                // The half that was pressed, not a toggle. The control shows both of its positions,
                // so pressing the word somebody wants is what it looks like it does - and a switch
                // nobody has set yet has no position to toggle away from.
                bool isOn;
                if (Switched(i) && i < _switches.Count && _switches[i].Chose(point, out isOn))
                {
                    _boxes[i].Text = _wanted[i].Words[isOn ? 0 : 1];
                    Enter(i);
                    return true;
                }

                // The control takes the press whether or not it chose anything with it, because
                // opening or shutting its list is a thing that happened to it and must not also mean
                // something to whatever is behind.
                string word;
                if (Chosen(i) && !Switched(i) && i < _lists.Count && _lists[i].Took(point, out word))
                {
                    if (word.Length > 0)
                        _boxes[i].Text = word;

                    Enter(i);
                    return true;
                }

                if (!At(i).Holds(point))
                    continue;

                Enter(i);
                return true;
            }

            Shut();
            return false;
        }

        /// <summary>
        /// Offers the press to anything on the question that is dragged rather than clicked.
        /// </summary>
        /// <remarks>
        /// Asked before the press is treated as a click, the same way the screen behind asks its own
        /// views. A slider is the only thing here that answers a drag, and it answers the press that
        /// starts one as well, so a press on a track is a way of setting it.
        /// </remarks>
        public bool Grab(Vector2 point)
        {
            for (var i = 0; i < Count; i++)
            {
                var grabbed = Colourful(i) ? Mixed(i).Grab(point) : Dialled(i) && Turned(i).Grab(point);
                if (!grabbed)
                    continue;

                Enter(i);
                return true;
            }

            return false;
        }

        public void Carry(Vector2 point)
        {
            for (var i = 0; i < _mixers.Count; i++)
                _mixers[i].Carry(point);

            for (var i = 0; i < _dials.Count; i++)
                _dials[i].Carry(point);
        }

        public void Drop()
        {
            for (var i = 0; i < _mixers.Count; i++)
                _mixers[i].Drop();

            for (var i = 0; i < _dials.Count; i++)
                _dials[i].Drop();
        }

        /// <summary>Whether something on the question is being dragged right now.</summary>
        public bool IsDragging
        {
            get
            {
                for (var i = 0; i < _mixers.Count; i++)
                {
                    if (_mixers[i].IsDragging)
                        return true;
                }

                for (var i = 0; i < _dials.Count; i++)
                {
                    if (_dials[i].IsDragging)
                        return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Whether the field with the keyboard is answered by choosing one of its words, so what is
        /// pressed on the keyboard chooses rather than types.
        /// </summary>
        public bool Chooses
        {
            get { return _typing && Chosen(_at); }
        }

        /// <summary>Whether the field with the keyboard is a dial, which is typed into and turned as well.</summary>
        public bool Turns
        {
            get { return _typing && Dialled(_at); }
        }

        /// <summary>How far one step turns a dial, in degrees.</summary>
        const float Notch = 5f;

        /// <summary>
        /// Moves the field with the keyboard on by <paramref name="by" /> steps, and answers whether it
        /// moved.
        /// </summary>
        /// <remarks>
        /// A step is the next word along, or a notch of a dial. Words stop at either end rather than
        /// going round, so holding a key comes to rest on the last one.
        /// </remarks>
        public bool Step(int by)
        {
            if (Chooses)
            {
                var words = _wanted[_at].Words;
                var now = words.IndexOf(_boxes[_at].Text);
                var next = now < 0
                    ? by > 0 ? 0 : words.Count - 1
                    : Math.Max(0, Math.Min(words.Count - 1, now + by));

                if (next == now)
                    return false;

                _boxes[_at].Text = words[next];
                return true;
            }

            if (!Turns)
                return false;

            // From what is written, which is what the dial shows, and within what the field will take.
            float degrees;
            var written = Turned(_at).Written;
            if (!float.TryParse(written.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out degrees))
                degrees = 0f;

            degrees = Bounded(_wanted[_at], (float)Math.Round(degrees / Notch) * Notch + by * Notch);
            written.Text = degrees.ToString("0.0", CultureInfo.InvariantCulture);
            written.MoveCaretEnd(false);
            Turned(_at).Reread();
            return true;
        }

        /// <summary>Holds an angle within the bounds a field writes, where it writes any.</summary>
        static float Bounded(Field field, float degrees)
        {
            float bound;
            if (float.TryParse(field.Least, NumberStyles.Float, CultureInfo.InvariantCulture, out bound))
                degrees = Math.Max(bound, degrees);

            if (float.TryParse(field.Most, NumberStyles.Float, CultureInfo.InvariantCulture, out bound))
                degrees = Math.Min(bound, degrees);

            return degrees;
        }

        /// <summary>
        /// Chooses the next word of the field with the keyboard that starts with
        /// <paramref name="letter" />, and answers whether there was one.
        /// </summary>
        /// <remarks>
        /// After the one chosen, and round to the start, so pressing the same letter again walks through
        /// every word that starts with it.
        /// </remarks>
        public bool Letter(char letter)
        {
            if (!Chooses)
                return false;

            var words = _wanted[_at].Words;
            var now = words.IndexOf(_boxes[_at].Text);
            for (var i = 1; i <= words.Count; i++)
            {
                var word = words[(now + i + words.Count) % words.Count];
                if (word.Length == 0 || char.ToUpperInvariant(word[0]) != char.ToUpperInvariant(letter))
                    continue;

                if (word == _boxes[_at].Text)
                    return false;

                _boxes[_at].Text = word;
                return true;
            }

            return false;
        }

        /// <summary>Moves the caret to the next field, and back to the first after the last.</summary>
        public void Next()
        {
            Move(_at + 1 < Count ? _at + 1 : 0);
        }

        /// <summary>Gives the keyboard to a field that was pressed.</summary>
        void Enter(int at)
        {
            var had = _typing;
            _typing = true;

            if (at != _at)
                Move(at);
            else if (!had)
                Editing();
        }

        /// <summary>
        /// Gives the keyboard to the first field still to be filled in, or the first that is wrong, or
        /// the first there is.
        /// </summary>
        /// <remarks>Nothing where there are no fields to give it to.</remarks>
        public void Take()
        {
            if (Count == 0)
                return;

            var first = 0;
            for (var i = 0; i < Count && i < _wanted.Count; i++)
            {
                if (At(i).Text.Length > 0 && _wanted[i].Refuses(Called(i), At(i).Text).Length == 0)
                    continue;

                first = i;
                break;
            }

            Enter(first);
        }

        /// <summary>Takes the keyboard away from the fields, and puts away any list left open.</summary>
        public void Release()
        {
            if (!_typing || !_column)
                return;

            _typing = false;
            At(_at).CaretVisible = false;
            At(_at).IsEditing = false;
            Shut();
        }

        /// <summary>Holds every field to what it takes from now on, and says why the first one that fails does.</summary>
        public void Judge()
        {
            _judged = true;

            for (var i = 0; i < Count; i++)
            {
                while (_seen.Count <= i)
                    _seen.Add(false);

                _seen[i] = true;
            }
        }

        /// <summary>Stops holding the fields to anything until finishing is tried again. Only a column is ever let off.</summary>
        public void Unjudge()
        {
            if (_column)
                _judged = false;
        }

        /// <summary>Whether every field holds something it takes, so there is nothing left to fill in.</summary>
        public bool Complete
        {
            get
            {
                for (var i = 0; i < Count && i < _wanted.Count; i++)
                {
                    if (At(i).Text.Length == 0 || _wanted[i].Refuses(Called(i), At(i).Text).Length > 0)
                        return false;
                }

                return true;
            }
        }

        /// <summary>
        /// What each field holds, as its own control spells it, by the field it is in.
        /// </summary>
        /// <remarks>
        /// By name, and by place for a field nobody named, so what was typed survives the fields
        /// changing under it: a hinge picked beside a rotor takes the direction away and leaves the
        /// angle where it was.
        /// </remarks>
        public void Spell(IDictionary<string, string> into)
        {
            for (var i = 0; i < Count && i < _wanted.Count; i++)
                into[Key(_wanted[i], i)] = At(i).Text;
        }

        /// <summary>What a field is known by in <see cref="Spell" />.</summary>
        public static string Key(Field field, int at)
        {
            return string.IsNullOrEmpty(field.Name) ? "#" + Ordinal(at) : field.Name;
        }

        void Move(int to)
        {
            if (to == _at)
                return;

            At(_at).CaretVisible = false;
            At(_at).IsEditing = false;
            _at = to;
            Editing();
        }

        /// <summary>Hands the caret to the field it is now in.</summary>
        /// <remarks>
        /// With what is in it taken, so typing replaces it. A field arrived at already holds either
        /// what it was given or what it starts at, and somebody who moves to it means to say something
        /// else - the same bargain a search box keeps.
        /// </remarks>
        void Editing()
        {
            if (!_typing)
                return;

            At(_at).IsEditing = true;
            At(_at).CaretVisible = true;
            At(_at).SelectAll();

            while (_seen.Count <= _at)
                _seen.Add(false);

            _seen[_at] = true;
        }

        /// <summary>Whether the caret has been in the field at <paramref name="at" />.</summary>
        bool Seen(int at)
        {
            return at < _seen.Count && _seen[at];
        }

        /// <summary>
        /// Why the field at <paramref name="at" /> cannot be sent as it stands, or empty.
        /// </summary>
        /// <remarks>
        /// A field nobody has reached yet is not called wrong for being empty. It still holds the form
        /// back - see <see cref="Missing" /> - so what is not filled in cannot be sent; it is only not
        /// marked in red before somebody has had the chance to fill it.
        /// </remarks>
        string Refuses(int at)
        {
            if (at >= _wanted.Count || !_judged)
                return "";

            if (!Seen(at) && string.IsNullOrEmpty(At(at).Text))
                return "";

            return _wanted[at].Refuses(Called(at), At(at).Text);
        }

        /// <summary>Whether a field that has to be given a value has none.</summary>
        public bool Missing
        {
            get
            {
                for (var i = 0; i < Count && i < _wanted.Count; i++)
                {
                    if (_wanted[i].Needed && string.IsNullOrEmpty(At(i).Text))
                        return true;
                }

                return false;
            }
        }

        /// <summary>What the answer will be sent to.</summary>
        public int Command
        {
            get { return _command; }
        }

        /// <summary>
        /// What is typed so far, as one answer.
        /// </summary>
        /// <remarks>
        /// The fields joined the way the language writes a list of values, so what arrives is what
        /// somebody would have written by hand: separated by spaces, and quoted where a value holds
        /// one. Whoever raised the question reads it back the same way.
        /// </remarks>
        public string Answer
        {
            get
            {
                // A field standing over several values becomes those values again here, in the order
                // its description declared them. That is the other half of showing three numbers as
                // one colour, and it is the only place the two spellings meet.
                _parts.Clear();
                _texts.Clear();
                for (var i = 0; i < Count; i++)
                {
                    if (Colourful(i))
                    {
                        Channels.Spell(At(i).Text, _wanted[i].Covers, _parts);
                        while (_texts.Count < _parts.Count)
                            _texts.Add(false);

                        continue;
                    }

                    // What the program holds for the word that was chosen, which is not the word
                    // wherever a block shows one thing and is told another. Unchanged everywhere
                    // else, because a field that said nothing about this maps a word to itself.
                    _parts.Add(i < _wanted.Count ? _wanted[i].Meaning(At(i).Text) : At(i).Text);
                    _texts.Add(i < _wanted.Count && _wanted[i].Entry == Entry.Text);
                }

                // Text is quoted whatever it spells, so a 42 typed where text is wanted stays text.
                if (_parts.Count == 1)
                    return _texts[0] && _parts[0].Length > 0 ? Quoted(_parts[0], true) : _parts[0];

                // Only as far as the last field somebody actually filled in. A field left alone is
                // a value not given, so the action is handed the default it declared - where an
                // empty one passed along would be an empty value, which is a different thing and
                // usually a worse one.
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

        /// <summary>
        /// One field's text, quoted where it would otherwise read as more than one value.
        /// </summary>
        /// <remarks>
        /// The language's rule, so nothing is lost and nothing changes meaning: a doubled quote
        /// stands for one, which is why no character can escape the next.
        /// </remarks>
        static string Quoted(string said, bool always)
        {
            if (said == null)
                return "\"\"";

            if (!always && said.IndexOf(' ') < 0 && said.IndexOf('"') < 0)
                return said;

            return "\"" + said.Replace("\"", "\"\"") + "\"";
        }

        /// <summary>Whether anything has been typed into any of the fields.</summary>
        bool Filled
        {
            get
            {
                for (var i = 0; i < Count; i++)
                {
                    if (!string.IsNullOrEmpty(At(i).Text))
                        return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Why what is typed cannot be sent, said in place of the question, or empty when it can.
        /// </summary>
        /// <remarks>
        /// Set by whoever raised the question, for the reasons only they can see: a name something
        /// else already has, a target that has gone away. What a field itself will take is
        /// <see cref="Wrong" />, which is the form's own business because the form was told it.
        /// </remarks>
        public string Refused { get; set; }

        /// <summary>
        /// Why one of the fields will not take what is in it, or empty where they all will.
        /// </summary>
        /// <remarks>
        /// The first one, because one reason is what there is room to say and the first is the one
        /// somebody reaches by reading down. A field is held only to what it was told it takes, and
        /// nothing here knows what the block will make of the value afterwards.
        /// <para>
        /// Worked out on the spot rather than watched for, and read every tick while a question is
        /// open. The reason has to follow what is being typed as it is typed, and it costs a parse
        /// per field of one open question - which is at most a handful, for as long as somebody is
        /// standing at a dialog. Nothing runs it while no question is up.
        /// </para>
        /// </remarks>
        public string Wrong
        {
            get
            {
                for (var i = 0; i < Count && i < _wanted.Count; i++)
                {
                    string why = Refuses(i);
                    if (why.Length > 0)
                        return why;
                }

                // Nothing at all is not an answer, even where every value may be left out: the
                // question's own Continue stays dead for it, and a column's Done says so.
                if (_column && _judged && Count > 0 && !Filled)
                    return Called(0) + " needs a value.";

                return "";
            }
        }

        /// <summary>Puts away any list left hanging open, for a press that went elsewhere.</summary>
        /// <remarks>
        /// A list that stayed open over a form somebody had moved on from would be the only thing on
        /// these screens that has to be dismissed before anything else can be reached.
        /// </remarks>
        void Shut()
        {
            for (var i = 0; i < _lists.Count; i++)
                _lists[i].Close();
        }

        /// <summary>Whether a point is on the question. False while none is open.</summary>
        public bool Covers(Vector2 point)
        {
            if (!_up)
                return false;

            if (_column ? _form.Holds(point) || _confirm.Holds(point) : _popup.Holds(point))
                return true;

            // And whatever it has hanging under itself, which is outside its own bounds. A press on
            // an open list read as a press outside the question, and put the question away.
            for (var i = 0; i < _lists.Count; i++)
            {
                if (_lists[i].Over(point))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Asks one thing, under whatever raised it.
        /// </summary>
        /// <remarks>
        /// A name, and names belong to the control that carries them - so the box opens where the
        /// words are. See <see cref="Values(string,int,IEnumerable{Field})" /> for the form, which
        /// does not belong to a control.
        /// </remarks>
        public void Open(string question, Element anchor, int command)
        {
            _up = true;
            _command = command;
            _anchor = anchor;
            _question = question ?? "";
            _wanted.Clear();

            Fields(1, false);
        }

        /// <summary>
        /// Asks for the values something takes, as a dialog with one field each.
        /// </summary>
        /// <remarks>
        /// In the middle of the screen and not under anything, however many fields it has. What
        /// raised it is then nobody's business: a button, a chip in a sentence and a double press
        /// all put the same form in the same place, which is one way of doing it rather than three.
        /// </remarks>
        public void Values(string question, int command, IEnumerable<Field> wanted)
        {
            Values(question, command, wanted, null);
        }

        /// <summary>
        /// The same, with the fields holding what is already there.
        /// </summary>
        /// <remarks>
        /// What it is given is copied here, once, because a question opens when somebody presses
        /// something and lives for one answer - while what handed it over is rebuilt every tick and
        /// must not be holding anything of its own.
        /// </remarks>
        public void Values(string question, int command, IEnumerable<Field> wanted,
            IEnumerable<string> filled)
        {
            _up = true;
            _command = command;
            _anchor = null;
            _question = question ?? "";

            // A column shows as many fields as there are, which may be none. A question opened for
            // values always has somewhere to type one.
            Copy(wanted, _wanted);
            Fields(_wanted.Count < 1 && !_column ? 1 : _wanted.Count, true);

            _filled.Clear();
            Copy(filled, _filled);

            // Read once, when the question opens, and copied out of the live list rather than held.
            // These are the colours this player paints with, and Sprocket only ever reads them.
            _colours.Clear();
            Player.Colours(_colours);

            for (var i = 0; i < Count; i++)
            {
                string said = i < _filled.Count ? _filled[i] ?? "" : "";

                // A field somebody has not answered yet starts at what it says is usual, where it says.
                if (said.Length == 0 && i < _wanted.Count && !string.IsNullOrEmpty(_wanted[i].Default))
                    said = _wanted[i].Default;

                if (i < _wanted.Count)
                    said = _wanted[i].Shown(said);

                if (Colourful(i))
                {
                    Mixed(i).Offer(_colours);
                    Mixed(i).Written.Text = said;
                    Mixed(i).Reread();
                    continue;
                }

                if (Dialled(i))
                {
                    Turned(i).Written.Text = said;
                    Turned(i).Reread();
                    continue;
                }

                if (i < _filled.Count || said.Length > 0)
                    _boxes[i].Text = said;
            }
        }

        static void Copy<T>(IEnumerable<T> from, List<T> into)
        {
            into.Clear();
            if (from == null)
                return;

            foreach (T one in from)
                into.Add(one);
        }

        readonly List<Field> _wanted = new List<Field>();
        readonly List<string> _filled = new List<string>();

        public void Close()
        {
            _up = false;
            _command = 0;
            _anchor = null;
        }

        /// <summary>
        /// Places it under its anchor and draws it, kept on screen.
        /// </summary>
        /// <remarks>
        /// A question that opened half off the bottom would be one nobody could answer, which is worse
        /// than one sitting above its anchor instead.
        /// </remarks>
        /// <summary>
        /// Puts this tick's words and state on the question.
        /// </summary>
        /// <remarks>
        /// Before anything is drawn, rather than while: a control armed in the draw path is armed
        /// after the pointer has already been asked what it is resting on, so Continue lit a frame
        /// late. It is also before the measure, because how a button is dressed decides how much room
        /// it holds clear inside itself.
        /// </remarks>
        public void Dress(Vector2 pointer)
        {
            if (!_up)
                return;

            // What the pointer is resting on, for the parts of this form that take their own presses
            // and are therefore invisible to the walk that lights everything else.
            for (var i = 0; i < _lists.Count; i++)
                _lists[i].Lit(pointer);

            for (var i = 0; i < _switches.Count; i++)
                _switches[i].Lit(pointer);

            for (var i = 0; i < _mixers.Count; i++)
                _mixers[i].Lit(pointer);

            for (var i = 0; i < _dials.Count; i++)
                _dials[i].Lit(pointer);

            // The question gives way to the reason while there is one, so the words sit where somebody
            // is already reading, and come back as soon as the answer is one that can be sent.
            // What is typed is the one part of a colour that can change without its control hearing,
            // so the control is brought into line before anything is drawn. It reads a string and
            // compares it, and does nothing at all while the two agree.
            for (var i = 0; i < Count; i++)
            {
                if (Colourful(i))
                {
                    Mixed(i).Reread();
                    continue;
                }

                if (Dialled(i))
                {
                    Turned(i).Reread();
                    continue;
                }

                if (!Chosen(i))
                    continue;

                // Read from the field rather than remembered, so what is shown cannot drift from what
                // will actually be sent. A field holding none of the words - which is what typing into
                // it does - leaves none of them lit, and the refusal says why.
                string said = _boxes[i].Text;

                // Its line on a lighter plate while it has the keyboard, which is how a field with no
                // caret says where the keys are going. A plate rather than a ring, because the words'
                // own buttons fill the line's height and would be drawn over one.
                if (i < _lines.Count)
                    _lines[i].Background = _typing && i == _at ? Palette.PanelHot : Palette.None;

                if (!Switched(i))
                {
                    if (i < _lists.Count)
                        _lists[i].Show(said);

                    continue;
                }

                if (i >= _switches.Count)
                    continue;

                if (said == _wanted[i].Words[0])
                    _switches[i].Show(true, false);
                else if (said == _wanted[i].Words[1])
                    _switches[i].Show(false, false);
                else
                    _switches[i].Unchosen();
            }

            bool refused = !string.IsNullOrEmpty(Refused);
            _label.Text = _question;
            _label.Color = Palette.Muted;
            _why.Text = Refused ?? "";
            _why.Collapsed = !refused;

            // The field the reason is about wears it too, so a form of three says which one to mend. A
            // column given nothing at all is about its first field.
            var blank = _column && _judged && !Filled;
            for (var i = 0; i < Count; i++)
                At(i).Wrong = Refuses(i).Length > 0 || blank && i == 0;

            // Dead until there is something to send, because an empty answer here is not a meaningful
            // one. Renaming is the opposite case and allows it, which is why the two are told apart. A
            // column's Done is live whenever there is something to finish, because pressing it is how
            // it is told what is wrong.
            //
            // Dressed and armed together, so a button drawn as a dead well cannot still answer a press.
            // That rule was being kept in two halves and only one of them said so.
            if (_column ? Finishable : Filled && !refused && !Missing)
            {
                Widgets.Go(_confirm);
                _confirm.Press = Deed.Commits();
            }
            else
            {
                Widgets.Waiting(_confirm);
                _confirm.Press = Deed.Nothing;
            }
        }

        /// <summary>
        /// Draws whatever is hanging open, after the form it hangs over.
        /// </summary>
        /// <remarks>
        /// Last, because a list under a box covers whatever the form put below it - which on this
        /// form is the way out. Drawn by the form rather than by the control, since a control draws
        /// where it was arranged and this has to draw somewhere else.
        /// </remarks>
        /// <summary>Measures what a column's labels need, before the column holding it is measured.</summary>
        /// <remarks>Does nothing unless the fields have changed since it last measured them.</remarks>
        public void Settle(IGraphics g, float width)
        {
            Inset(g, width);
        }

        /// <summary>Draws a column's hanging lists over everything else, after the screen has been drawn.</summary>
        public void DrawHanging(IGraphics g)
        {
            Hanging(g);
        }

        void Hanging(IGraphics g)
        {
            for (var i = 0; i < _lists.Count; i++)
                _lists[i].Hanging(g);
        }

        public void Draw(IGraphics g, RectangleF bounds, float width)
        {
            if (!_up)
                return;

            // Before the popup is measured, because how much room the labels hold decides how much
            // room everything beside them has left.
            Inset(g, width);

            _popup.Measure(g, new Vector2(width, bounds.Height));

            float height = _popup.DesiredSize.Y;

            // A form goes in the middle, where it is the only thing to answer and where it does not
            // matter which control - or which gesture - raised it.
            if (_anchor == null)
            {
                float across = _popup.DesiredSize.X;
                _popup.Arrange(new RectangleF(bounds.X + (bounds.Width - across) * 0.5f,
                    bounds.Y + (bounds.Height - height) * 0.5f, across, height));

                _popup.Draw(g);
                Hanging(g);
                return;
            }

            float x = _anchor.Bounds.X;
            float y = _anchor.Bounds.Y + _anchor.Bounds.Height + 2f;

            if (y + height > bounds.Y + bounds.Height)
                y = _anchor.Bounds.Y - height - 2f;

            if (x + width > bounds.X + bounds.Width)
                x = bounds.X + bounds.Width - width;
            if (x < bounds.X)
                x = bounds.X;

            _popup.Arrange(new RectangleF(x, y, width, height));
            _popup.Draw(g);
            Hanging(g);
        }
    }
}
