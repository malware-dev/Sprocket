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
        private readonly TextBox _box = Widgets.Box(Lettering.Body, Palette.Page);
        private readonly Label _confirm = Widgets.Label("Continue", Palette.Ink, Lettering.Small);

        private Element _anchor;
        private int _command;
        private string _question = "";
        private bool _up;

        public Ask()
        {
            _box.IsEditing = true;

            var entry = new Bar();
            entry.Spacing = Space.Item;
            entry.Fill = _box;
            entry.Right = _confirm;

            _popup.Background = Palette.PanelHot;
            _popup.Edge = Palette.Line;
            _popup.Padding = new Thickness(Space.Region, Space.Item);
            _popup.Children.Add(_label);
            _popup.Children.Add(entry);
        }

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

        /// <summary>The box the answer is typed into.</summary>
        public TextBox Box
        {
            get { return _box; }
        }

        /// <summary>What the answer will be sent to.</summary>
        public int Command
        {
            get { return _command; }
        }

        /// <summary>What is typed so far.</summary>
        public string Answer
        {
            get { return _box.Text; }
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

        public void Open(string question, Element anchor, int command)
        {
            _up = true;
            _command = command;
            _anchor = anchor;
            _question = question ?? "";
            _box.Text = "";
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
        public void Draw(IGraphics g, RectangleF bounds, float width)
        {
            if (!_up || _anchor == null)
                return;

            // The question gives way to the reason while there is one, so the words sit where somebody is
            // already reading, and come back as soon as the answer is one that can be sent.
            bool refused = !string.IsNullOrEmpty(Refused);
            _label.Text = refused ? Refused : _question;
            _label.Color = refused ? Palette.Problem : Palette.Muted;

            // Dead until there is something to send, because an empty answer here is not a meaningful
            // one. Renaming is the opposite case and allows it, which is why the two are told apart.
            //
            // Before the measure, not after: how a button is dressed decides how much room it holds
            // clear inside itself, and a button dressed after being measured is drawn at the size it
            // was last frame.
            // Dressed and armed together, so a button drawn as a dead well cannot still answer a
            // press. That rule was being kept in two halves and only one of them said so.
            if (!string.IsNullOrEmpty(_box.Text) && !refused)
            {
                Widgets.Go(_confirm);
                _confirm.Press = Deed.Commits();
            }
            else
            {
                Widgets.Waiting(_confirm);
                _confirm.Press = Deed.Nothing;
            }

            _popup.Measure(g, new Vector2(width, bounds.Height));

            float height = _popup.DesiredSize.Y;
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
