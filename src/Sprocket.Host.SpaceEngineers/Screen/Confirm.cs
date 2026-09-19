using Sprocket.Host.SpaceEngineers.Lists;
using VRageMath;
using Sprocket.Host.SpaceEngineers.Drawing;
using Sprocket.Host.SpaceEngineers.Layout;
using Sprocket.Host.SpaceEngineers.Controls;
using Sprocket.Host.SpaceEngineers.Look;

namespace Sprocket.Host.SpaceEngineers.Screen
{
    /// <summary>
    /// A short question under whatever raised it, answered by pressing one of a few words.
    /// </summary>
    /// <remarks>
    /// The sibling of <see cref="Ask" />, which takes a typed answer, and it hangs the same way and
    /// in the same place: under the control that asked, reading as that control opening rather than
    /// as the screen stopping. A question in the middle of the screen takes the eye away from the
    /// row it is about, and then has to say which row that was.
    /// <para>
    /// Its answers are a column of rows rather than a line of chips. Four of them - what a trigger
    /// can be for - is a list to read down, and a row is the same shape as every other thing on
    /// these screens that leads somewhere.
    /// </para>
    /// <para>
    /// It owns the screen while it is up: nothing outside it answers. Pressing away, or Escape,
    /// dismisses it without choosing - which is a third outcome rather than the safe answer, because
    /// somebody who backs out of "switch this on?" has said they did not mean to leave at all.
    /// </para>
    /// </remarks>
    public sealed class Confirm
    {
        private readonly Stack _popup = Widgets.Column(Space.Line);
        private readonly Label _label = Widgets.Label("", Palette.Muted, Lettering.Tiny);
        private readonly Stack _rows = Widgets.Column(0f);

        private Element _anchor;
        private bool _up;

        public Confirm()
        {
            _label.Padding = new Thickness(Space.ChipX, Space.ChipY, Space.ChipX, 0f);

            _popup.Background = Palette.PanelHot;
            _popup.Edge = Palette.Line;
            _popup.Padding = new Thickness(0f, Space.ChipY);
            _popup.Children.Add(_label);
            _popup.Children.Add(_rows);
        }

        public bool IsUp
        {
            get { return _up; }
        }

        /// <summary>The question itself, for the screen to walk while it owns it.</summary>
        public Element Root
        {
            get { return _popup; }
        }

        /// <summary>Whether a point is on the question. False while none is up.</summary>
        public bool Covers(Vector2 point)
        {
            return _up && _popup.Holds(point);
        }

        /// <summary>
        /// Shows a question and its answers, in the order given, under what raised it.
        /// </summary>
        /// <remarks>
        /// Labels are made once per question rather than reused across questions, because a question
        /// appears when somebody presses something and lives for one answer. Nothing here repeats.
        /// </remarks>
        public void Open(string question, ObservableList<ChoiceEntry> answers, Element anchor)
        {
            _up = true;
            _anchor = anchor;
            _label.Text = question;
            _label.Collapsed = string.IsNullOrEmpty(question);

            _rows.Children.Clear();

            foreach (ChoiceEntry answer in answers)
            {
                Label row = Widgets.Way(answer.Text);
                row.Padding = new Thickness(Space.ChipX, Space.ChipY);
                row.Press = Deed.Sends(answer.Command);

                _rows.Children.Add(row);
            }
        }

        public void Close()
        {
            _up = false;
            _anchor = null;
        }

        /// <summary>
        /// Places it under its anchor and draws it, kept on screen.
        /// </summary>
        /// <remarks>
        /// A question that opened half off the bottom would be one nobody could answer, which is
        /// worse than one sitting above its anchor instead. With nothing to hang under - a question
        /// nobody's press raised - it goes in the middle, where it is at least findable.
        /// </remarks>
        public void Draw(IGraphics g, RectangleF bounds, float width)
        {
            if (!_up)
                return;

            _popup.Measure(g, new Vector2(width, bounds.Height));

            Vector2 wanted = _popup.DesiredSize;

            if (_anchor == null)
            {
                Place(g, bounds.X + (bounds.Width - wanted.X) * 0.5f,
                    bounds.Y + (bounds.Height - wanted.Y) * 0.5f, wanted);
                return;
            }

            float x = _anchor.Bounds.X;
            float y = _anchor.Bounds.Y + _anchor.Bounds.Height + 2f;

            if (y + wanted.Y > bounds.Y + bounds.Height)
                y = _anchor.Bounds.Y - wanted.Y - 2f;

            if (x + wanted.X > bounds.X + bounds.Width)
                x = bounds.X + bounds.Width - wanted.X;
            if (x < bounds.X)
                x = bounds.X;

            Place(g, x, y, wanted);
        }

        void Place(IGraphics g, float x, float y, Vector2 wanted)
        {
            _popup.Arrange(new RectangleF(x, y, wanted.X, wanted.Y));
            _popup.Draw(g);
        }
    }
}
