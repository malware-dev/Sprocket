using Lumel.Drawing;
using Lumel.Input;
using Lumel.Text;
using VRageMath;

namespace Lumel.Screens
{
    /// <summary>One element shown on a <see cref="GameScreen" />, laid out across its room and handed its input through an <see cref="InputRoot" />.</summary>
    /// <remarks>Escape closes it, and Enter carries out <see cref="Accept" /> where it has one.</remarks>
    public class Stage : IScreenView
    {
        readonly Element _root;
        readonly InputRoot _input;
        bool _closing;

        /// <summary>The stage showing <paramref name="root" />.</summary>
        public Stage(Element root)
        {
            _root = root;
            _input = new InputRoot(root);
        }

        /// <summary>What is shown.</summary>
        public Element Root => _root;

        /// <summary>What Enter does, or null where Enter does nothing.</summary>
        /// <remarks>While there is one, the keyboard goes to it and to nothing that could be typed in.</remarks>
        public ICommand Accept { get; set; }

        /// <inheritdoc />
        public InputRoot Input => _input;

        /// <inheritdoc />
        Vector2 IScreenView.Pointer
        {
            set { }
        }

        /// <inheritdoc />
        System.Func<bool> IScreenView.IsKeepKeyHeld
        {
            set { }
        }

        /// <inheritdoc />
        public bool CaretVisible
        {
            set
            {
                var box = _input.Focused as TextBox;
                if (box != null)
                    box.CaretVisible = value;
            }
        }

        /// <inheritdoc />
        public bool IsQuestioning => Accept != null;

        /// <summary>Asks the screen to close as soon as the press under way ends.</summary>
        public void Close()
        {
            _closing = true;
        }

        /// <inheritdoc />
        public bool TakeDefault()
        {
            if (Accept == null || !Accept.CanExecute)
                return false;

            Accept.Execute();
            return true;
        }

        /// <inheritdoc />
        public void PointerDown(Vector2 point, int presses, bool shift, bool ctrl)
        {
            _input.PointerDown(point, presses, shift, ctrl);
        }

        /// <inheritdoc />
        public void PointerMove(Vector2 point)
        {
            _input.PointerMove(point);
        }

        /// <inheritdoc />
        public void PointerUp(Vector2 point)
        {
            _input.PointerUp(point);
        }

        /// <inheritdoc />
        public bool TakeClose()
        {
            var closing = _closing;
            _closing = false;
            return closing;
        }

        /// <inheritdoc />
        public void ScrollBy(Vector2 point, float amount)
        {
            var scrolls = _root.WheelAt(point);
            if (scrolls != null)
                scrolls.ScrollBy(amount);
        }

        /// <inheritdoc />
        public bool Escape()
        {
            return true;
        }

        /// <inheritdoc />
        public void Update()
        {
        }

        /// <inheritdoc />
        public void Draw(IGraphics g, RectangleF bounds)
        {
            _root.Measure(g, new Vector2(bounds.Width, bounds.Height));
            _root.Arrange(bounds);
            _root.Draw(g);
        }
    }
}
