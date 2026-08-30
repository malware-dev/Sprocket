using Phosphor.Graphics;
using Phosphor.Ui;
using VRageMath;
using Reactor.Screens.Controls;
using Reactor.Screens.Lists;
using Reactor.Screens.Look;
using Reactor.Screens.Picking;
using Reactor.Screens.Sequences;

namespace Reactor.Screens.Host
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
        private RectangleF _bounds;
        private int _command;
        private bool _up;

        public Ask()
        {
            _box.IsEditing = true;
            _confirm.Padding = new Thickness(Space.PadX, Space.ChipY * 1.3f);

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

        /// <summary>Whether a point is on the question. False while none is open.</summary>
        public bool Covers(Vector2 point)
        {
            return _up
                && point.X >= _bounds.X && point.X <= _bounds.X + _bounds.Width
                && point.Y >= _bounds.Y && point.Y <= _bounds.Y + _bounds.Height;
        }

        /// <summary>Whether a point is on the button that sends the answer.</summary>
        public bool OnConfirm(Vector2 point)
        {
            RectangleF at = _confirm.Bounds;

            return _up
                && point.X >= at.X && point.X <= at.X + at.Width
                && point.Y >= at.Y && point.Y <= at.Y + at.Height;
        }

        public void Open(string question, Element anchor, int command)
        {
            _up = true;
            _command = command;
            _anchor = anchor;
            _label.Text = question;
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
            {
                _bounds = new RectangleF();
                return;
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

            _bounds = new RectangleF(x, y, width, height);
            _popup.Arrange(_bounds);

            // Dead until there is something to send, because an empty answer here is not a meaningful
            // one. Renaming is the opposite case and allows it, which is why the two are told apart.
            bool ready = !string.IsNullOrEmpty(_box.Text);

            // A well rather than a light fill when it is not ready. Dim is what an unlit thing is drawn
            // in, and a button wearing it with quiet text on top was two near-identical greys.
            _confirm.Background = ready ? Palette.Accent : Palette.SlotFill;
            _confirm.Color = ready ? Palette.Chrome : Palette.Faint;

            _popup.Draw(g);
        }
    }
}
