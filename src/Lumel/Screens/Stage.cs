using Lumel.Drawing;
using Lumel.Input;
using Lumel.Text;
using VRageMath;

namespace Lumel.Screens
{
    /// <summary>One element shown on a <see cref="GameScreen" />, laid out across its room and handed its input through an <see cref="InputRoot" />.</summary>
    /// <remarks>
    ///     The plainest view a screen can show: it asks no questions, and Escape closes it. A press asks it to close through
    ///     <see cref="Close" />, as a cross on it would.
    /// </remarks>
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

        /// <inheritdoc />
        public InputRoot Input => _input;

        /// <inheritdoc />
        /// <remarks>A stage shows no tips, so where the pointer rests between presses tells it nothing.</remarks>
        Vector2 IScreenView.Pointer
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
        public bool IsQuestioning => false;

        /// <summary>Asks the screen to close as soon as the press under way ends.</summary>
        public void Close()
        {
            _closing = true;
        }

        /// <inheritdoc />
        public bool TakeDefault()
        {
            return false;
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
        /// <remarks>Moves the innermost scroll under the pointer that can move.</remarks>
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
