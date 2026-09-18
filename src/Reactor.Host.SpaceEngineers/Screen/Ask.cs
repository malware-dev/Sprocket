using System.Collections.Generic;
using System.Globalization;
using Reactor.Host.SpaceEngineers.Drawing;
using Reactor.Host.SpaceEngineers.Layout;
using VRageMath;
using Reactor.Host.SpaceEngineers.Controls;
using Reactor.Host.SpaceEngineers.Look;

namespace Reactor.Host.SpaceEngineers.Screen
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
            _cancel.Press = Deed.Leaves();

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
                _fields.Children.Add(Line(i, count > 1));
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
        /// How much room a value's number takes, in screen units.
        /// </summary>
        /// <remarks>
        /// A width of its own, so every box in the form starts at the same place. Left to size
        /// itself, each number is as wide as the digit in it and the fields step raggedly to the
        /// right - which reads as three things that are not the same kind of thing.
        /// </remarks>
        const float NumberWidth = 28f;

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
        Bar Line(int at, bool numbered)
        {
            while (_lines.Count <= at)
            {
                _lines.Add(new Bar());
                _numbers.Add(Widgets.Label("", Palette.Faint, Lettering.Small));
            }

            Bar line = _lines[at];
            line.Spacing = Space.Item;
            line.Left = numbered ? Numbered(at) : null;
            line.Fill = _boxes[at];

            _numbers[at].Text = numbered
                ? (at + 1).ToString(CultureInfo.InvariantCulture)
                : "";

            return line;
        }

        /// <summary>The number, held at a width so the fields under each other line up.</summary>
        Sized Numbered(int at)
        {
            while (_held.Count <= at)
                _held.Add(new Sized(NumberWidth));

            _held[at].Content = _numbers[at];
            return _held[at];
        }

        readonly List<Sized> _held = new List<Sized>();

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
        public string Refused { get; set; }

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
        /// words are. See <see cref="Values" /> for the form, which does not belong to a control.
        /// </remarks>
        public void Open(string question, Element anchor, int command)
        {
            _up = true;
            _command = command;
            _anchor = anchor;
            _question = question ?? "";

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
        public void Values(string question, int command, int fields)
        {
            Values(question, command, fields, null);
        }

        /// <summary>The same, with the fields holding what is already there.</summary>
        public void Values(string question, int command, int fields, IList<string> filled)
        {
            _up = true;
            _command = command;
            _anchor = null;
            _question = question ?? "";

            Fields(fields < 1 ? 1 : fields, true);

            if (filled == null)
                return;

            for (var i = 0; i < filled.Count && i < Count; i++)
                _boxes[i].Text = filled[i] ?? "";
        }

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
