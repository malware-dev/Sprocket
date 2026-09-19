using System.Collections.Generic;
using System.Globalization;
using Sprocket.Host.SpaceEngineers.Drawing;
using Sprocket.Host.SpaceEngineers.Layout;
using VRageMath;
using Sprocket.Host.SpaceEngineers.Controls;
using Sprocket.Host.SpaceEngineers.Look;

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
        private readonly List<Bar> _lines = new List<Bar>();
        private readonly List<Label> _numbers = new List<Label>();
        private readonly Label _confirm = Widgets.Label("Continue", Palette.Ink, Lettering.Small);
        private readonly Label _cancel = Widgets.Label("Cancel", Palette.Muted, Lettering.Small);

        private readonly Sized _form = new Sized(FormWidth);

        private Element _anchor;
        private int _command;
        private string _question = "";
        private bool _up;
        private int _at;

        public Ask()
        {
            var ways = Widgets.RowOf(Space.Item);
            ways.Children.Add(_cancel);
            ways.Children.Add(_confirm);

            _cancel.Padding = new Thickness(Space.ChipX, Space.ChipY);
            _cancel.Press = Deed.Cancels();

            // Lit, because everything a press reaches has to be - and quietly, because it is the way
            // out rather than the way on.
            _cancel.Highlight = Palette.Panel;

            _entry.Spacing = Space.Item;
            _entry.Right = ways;

            _popup.Background = Palette.PanelHot;
            _popup.Edge = Palette.Line;
            _popup.Padding = new Thickness(Space.Region, Space.Item);
            _form.Content = _fields;

            // The form above the line that finishes it, so reading down the dialog is the order it
            // is filled in: what it wants, the fields, then the way out.
            _popup.Children.Add(_label);
            _popup.Children.Add(_form);
            _popup.Children.Add(_entry);

            Fields(1, false);
        }

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
                _boxes.Add(Widgets.Box(Lettering.Body, Palette.Page));

            _fields.Children.Clear();
            for (var i = 0; i < count; i++)
            {
                _boxes[i].Text = "";
                _fields.Children.Add(Line(i, count));
            }

            _entry.Fill = form ? null : _boxes[0];
            _form.Collapsed = !form;

            // The way out is the form's, and not every question's. A name is left by pressing away
            // or by Escape, the way it always has been, and a Cancel beside it would take width
            // from the box somebody is typing in.
            _cancel.Collapsed = !form;

            // The caret belongs to the field it is in, and to no other. Every box editing meant
            // every box drawing one, with only the real one blinking - which reads as three carets
            // rather than as one place to type.
            for (var i = 0; i < _boxes.Count; i++)
                _boxes[i].IsEditing = false;

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
            line.Fill = _boxes[at];

            return line;
        }

        string Label(int at, int count)
        {
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
            for (var i = 0; i < Count; i++)
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
            get { return _boxes[_at]; }
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
                if (!_boxes[i].Holds(point))
                    continue;

                Move(i);
                return true;
            }

            return false;
        }

        /// <summary>Moves the caret to the next field, and back to the first after the last.</summary>
        public void Next()
        {
            Move(_at + 1 < Count ? _at + 1 : 0);
        }

        void Move(int to)
        {
            if (to == _at)
                return;

            _boxes[_at].CaretVisible = false;
            _boxes[_at].IsEditing = false;
            _at = to;
            Editing();
        }

        /// <summary>Hands the caret to the field it is now in.</summary>
        void Editing()
        {
            _boxes[_at].IsEditing = true;
            _boxes[_at].CaretVisible = true;
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
                if (Count == 1)
                    return _boxes[0].Text;

                // Only as far as the last field somebody actually filled in. A field left alone is
                // a value not given, so the action is handed the default it declared - where an
                // empty one passed along would be an empty value, which is a different thing and
                // usually a worse one.
                var last = -1;
                for (var i = 0; i < Count; i++)
                {
                    if (!string.IsNullOrEmpty(_boxes[i].Text))
                        last = i;
                }

                var said = "";
                for (var i = 0; i <= last; i++)
                {
                    if (said.Length > 0)
                        said += " ";

                    said += Quoted(_boxes[i].Text);
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
        static string Quoted(string said)
        {
            if (said == null)
                return "\"\"";

            if (said.IndexOf(' ') < 0 && said.IndexOf('"') < 0)
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
                    if (!string.IsNullOrEmpty(_boxes[i].Text))
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
                    string why = _wanted[i].Refuses(Called(i), _boxes[i].Text);
                    if (why.Length > 0)
                        return why;
                }

                return "";
            }
        }

        /// <summary>Whether a point is on the question. False while none is open.</summary>
        public bool Covers(Vector2 point)
        {
            return _up && _popup.Holds(point);
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

            Copy(wanted, _wanted);
            Fields(_wanted.Count < 1 ? 1 : _wanted.Count, true);

            _filled.Clear();
            Copy(filled, _filled);

            for (var i = 0; i < _filled.Count && i < Count; i++)
                _boxes[i].Text = _filled[i] ?? "";
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
        public void Dress()
        {
            if (!_up)
                return;

            // The question gives way to the reason while there is one, so the words sit where somebody
            // is already reading, and come back as soon as the answer is one that can be sent.
            bool refused = !string.IsNullOrEmpty(Refused);
            _label.Text = refused ? Refused : _question;
            _label.Color = refused ? Palette.Problem : Palette.Muted;

            // Dead until there is something to send, because an empty answer here is not a meaningful
            // one. Renaming is the opposite case and allows it, which is why the two are told apart.
            //
            // Dressed and armed together, so a button drawn as a dead well cannot still answer a press.
            // That rule was being kept in two halves and only one of them said so.
            if (Filled && !refused)
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
        }
    }
}
