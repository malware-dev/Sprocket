using VRageMath;

namespace Lumel.Windows
{
    /// <summary>The bar along a floating window's top that moves it: pressed anywhere but on what takes presses of its own, the window follows the pointer.</summary>
    public class TitleBar : ContentControl
    {
        Vector2 _held;
        bool _holding;

        /// <summary>The window it moves, or null for none.</summary>
        public FloatingWindow Window { get; set; }

        /// <inheritdoc />
        protected override bool TakesPointer => Window != null;

        /// <inheritdoc />
        public override void OnPointerDown(Vector2 point, int presses, bool shift, bool ctrl)
        {
            if (Window == null)
                return;

            var bounds = Window.Bounds;
            _held = new Vector2(point.X - bounds.X, point.Y - bounds.Y);
            _holding = true;
        }

        /// <inheritdoc />
        public override void OnPointerMove(Vector2 point)
        {
            if (_holding && Window != null)
                Window.Position = point - _held;
        }

        /// <inheritdoc />
        public override void OnPointerUp(Vector2 point)
        {
            if (!_holding || Window == null)
                return;

            _holding = false;
            Window.Position = point - _held;
            Window.Settle();
        }
    }
}
