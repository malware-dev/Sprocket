using Reactor.Host.SpaceEngineers.Lists;
using VRageMath;
using Reactor.Host.SpaceEngineers.Drawing;
using Reactor.Host.SpaceEngineers.Layout;
using Reactor.Host.SpaceEngineers.Controls;
using Reactor.Host.SpaceEngineers.Look;

namespace Reactor.Host.SpaceEngineers.Screen
{
    /// <summary>
    /// A short question over the middle of the screen, answered by pressing one of a few words.
    /// </summary>
    /// <remarks>
    /// The sibling of <see cref="Ask" />, which takes a typed answer and hangs under whatever raised
    /// it. This one is centred, because it is not about any one control - it is about what pressing
    /// back would do, and there is nothing on screen to point at.
    /// <para>
    /// It owns the screen while it is up: nothing outside it answers. Pressing away, or Escape,
    /// dismisses it without choosing - which is a third outcome rather than the safe answer, because
    /// somebody who backs out of "switch this on?" has said they did not mean to leave at all.
    /// </para>
    /// </remarks>
    public sealed class Confirm
    {
        private readonly Stack _popup = Widgets.Column(Space.Item);
        private readonly Label _label = Widgets.Label("", Palette.Ink, Lettering.Body);
        private readonly Stack _row = Widgets.RowOf(Space.Item);
        private bool _up;

        public Confirm()
        {
            _popup.Background = Palette.PanelHot;
            _popup.Edge = Palette.Line;
            _popup.Padding = new Thickness(Space.Region, Space.Region);
            _popup.Children.Add(_label);
            _popup.Children.Add(_row);
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
        /// Shows a question and its answers, in the order given.
        /// </summary>
        /// <remarks>
        /// Labels are made once per question rather than reused across questions, because a question
        /// appears when somebody presses back and lives for one answer. Nothing here repeats.
        /// </remarks>
        public void Open(string question, ObservableList<ChoiceEntry> answers)
        {
            _up = true;
            _label.Text = question;

            _row.Children.Clear();

            foreach (ChoiceEntry answer in answers)
            {
                Label word = Widgets.Chip(answer.Text);
                word.Press = Deed.Sends(answer.Command);

                _row.Children.Add(word);
            }
        }

        public void Close()
        {
            _up = false;
            _label.Text = "";
            _row.Children.Clear();
        }

        /// <summary>Draws it across the middle of the screen.</summary>
        public void Draw(IGraphics g, RectangleF bounds, float width)
        {
            if (!_up)
                return;

            _popup.Measure(g, new Vector2(width, bounds.Height));

            Vector2 wanted = _popup.DesiredSize;
            float x = bounds.X + (bounds.Width - wanted.X) * 0.5f;
            float y = bounds.Y + (bounds.Height - wanted.Y) * 0.5f;

            _popup.Arrange(new RectangleF(x, y, wanted.X, wanted.Y));
            _popup.Draw(g);
        }
    }
}
