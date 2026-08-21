using Sandbox.ModAPI;
using VRage.Input;
using VRage.Utils;
using VRageMath;

namespace Phosphor
{
    /// <summary>
    /// A pointer driven by the mouse, for the screen-space target only.
    /// <para>
    /// It integrates the movement the camera would have used rather than reading a cursor position, because
    /// during ordinary play the game holds the mouse still and reports only how far it moved. That is also
    /// the reason this works at all: the same movement that would have turned the player's head moves the
    /// pointer instead, once look has been suspended.
    /// </para>
    /// <para>
    /// It draws the game's own arrow, so the pointer a player sees here is the one they already know. There
    /// is no id for that texture, so the mod declares a material pointing at the same file.
    /// </para>
    /// </summary>
    public sealed class MousePointer : IPointer
    {
        private static readonly MyStringId Arrow = MyStringId.GetOrCompute("Phosphor_Cursor");
        private static readonly MyStringId Hand = MyStringId.GetOrCompute("Phosphor_CursorHand");

        /// <summary>Roughly the size the game draws its own, in UI units.</summary>
        private const float Size = 36f;

        private Vector2 _position;
        private bool _pressed;
        private bool _wasPressed;

        public bool IsAvailable
        {
            get { return true; }
        }

        public Vector2 Position
        {
            get { return _position; }
        }

        public bool IsPressed
        {
            get { return _pressed; }
        }

        public bool WasPressed
        {
            get { return _wasPressed; }
        }

        /// <summary>Whether the thing under the pointer can be pressed, which changes the arrow to a hand.</summary>
        public bool OverSomethingPressable { get; set; }

        /// <summary>Puts the pointer in the middle of <paramref name="bounds" />, for when the UI opens.</summary>
        public void Center(RectangleF bounds)
        {
            _position = new Vector2(bounds.X + bounds.Width * 0.5f, bounds.Y + bounds.Height * 0.5f);
            _pressed = false;
            _wasPressed = false;
        }

        /// <summary>
        /// Moves the pointer by however far the mouse moved, and keeps it on screen. Call once per frame from
        /// input handling.
        /// </summary>
        /// <param name="bounds">
        /// Where the pointer is allowed, which is the whole window rather than the laid-out area. The game
        /// lets its own cursor reach the physical edges, and a pointer that stopped at the edge of a 4:3 box
        /// in the middle of a widescreen monitor would feel broken.
        /// </param>
        public void Update(RectangleF bounds)
        {
            if (MyAPIGateway.Input == null)
                return;

            _position.X += MyAPIGateway.Input.GetMouseXForGamePlay();
            _position.Y += MyAPIGateway.Input.GetMouseYForGamePlay();

            if (_position.X < bounds.X)
                _position.X = bounds.X;
            if (_position.Y < bounds.Y)
                _position.Y = bounds.Y;
            if (_position.X > bounds.X + bounds.Width)
                _position.X = bounds.X + bounds.Width;
            if (_position.Y > bounds.Y + bounds.Height)
                _position.Y = bounds.Y + bounds.Height;

            _wasPressed = MyAPIGateway.Input.IsNewLeftMousePressed();
            _pressed = MyAPIGateway.Input.IsLeftMousePressed();
        }

        /// <summary>
        /// Draws the arrow with its point at <see cref="Position" />. The texture's tip is its top-left
        /// corner, so the sprite hangs down and right of where it is actually pointing.
        /// </summary>
        public void Draw(IGraphics g)
        {
            MyStringId material = OverSomethingPressable ? Hand : Arrow;
            g.DrawSprite(material, new RectangleF(_position.X, _position.Y, Size, Size), Color.White, 0f);
        }
    }
}
