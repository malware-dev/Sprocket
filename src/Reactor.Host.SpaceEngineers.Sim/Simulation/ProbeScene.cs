using System.Text;
using Phosphor;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace Reactor.Sim.Simulation
{
    /// <summary>
    /// What the simulator draws until there is a Reactor screen to draw instead.
    /// <para>
    /// Deliberately not a pretty demo. Every part of it is here to make one thing visible that is otherwise
    /// only observable by launching the game: where the fixed UI box actually falls, whether a clip cuts a
    /// glyph or a whole row, whether text lands where the layout measured it, and whether a pointer can
    /// reach past the layout to the edge of the screen.
    /// </para>
    /// </summary>
    public sealed class ProbeScene
    {
        static readonly MyStringId Fill = MyStringId.GetOrCompute("Phosphor_Fill");
        static readonly MyStringId Cursor = MyStringId.GetOrCompute("Phosphor_Cursor");
        static readonly MyStringId Unknown = MyStringId.GetOrCompute("Phosphor_NoSuchMaterial");

        readonly Stack _panel = new Stack();
        readonly Stack _clipped = new Stack();
        readonly TextBox _box = new TextBox();
        readonly SimPointer _pointer = new SimPointer();

        public ProbeScene()
        {
            _panel.Spacing = 8f;
            _panel.Background = new Color(16, 22, 30, 230);
            _panel.Children.Add(Heading("Phosphor, out of the game"));

            _box.Scale = 0.7f;
            _box.Background = new Color(20, 34, 48, 235);
            _box.Text = "Hangar door";
            _box.IsEditing = true;
            _panel.Children.Add(_box);

            // The one that matters, and it is arranged on its own rather than inside the panel. A Stack
            // always measures to the sum of its children, so there is no way yet to tell one "you get this
            // much height and cut the rest" - which is exactly the missing piece for a scrolling panel, and
            // the first thing having a window found.
            _clipped.Spacing = 4f;
            _clipped.ClipsContent = true;
            _clipped.Background = new Color(10, 14, 20, 200);

            // Too long for the panel on purpose, so the right-hand edge cuts through a letter rather than
            // between two of them. A row that merely vanished would prove nothing: it would look the same
            // as a row that was never added.
            for (int i = 1; i <= 8; i++)
                _clipped.Children.Add(Body("Row " + i
                    + " runs off the right-hand edge of this panel, and is cut mid-glyph rather than mid-word"));
        }

        public TextBox Box
        {
            get { return _box; }
        }

        /// <summary>Whether the caret is drawn this frame. The blink is the window's clock, not the box's.</summary>
        public bool CaretVisible
        {
            set { _box.CaretVisible = value; }
        }

        /// <summary>
        /// Where the pointer is, in UI units.
        /// <para>
        /// Set absolutely here, where a real cursor exists in a real window. In the game it is integrated
        /// from mouse deltas instead, because the game holds the cursor still and reports only movement.
        /// The difference is confined to this line: the clamping and everything downstream is the same.
        /// </para>
        /// </summary>
        public void MovePointer(Vector2 position, RectangleF bounds)
        {
            _pointer.MoveTo(position, bounds);
        }

        public void SetPressed(bool pressed)
        {
            _pointer.SetPressed(pressed);
        }

        public void Draw(ScreenSpaceGraphics g)
        {
            Vector2 size = g.Size;

            // The fixed UI box the game would lay out in, outlined so its edges are a thing you can see
            // rather than a thing you have to trust.
            Outline(g, new RectangleF(0f, 0f, size.X, size.Y), new Color(0, 200, 255, 90), 2f);

            const float width = 620f;

            _panel.Measure(g, new Vector2(width, size.Y));
            _panel.Arrange(new RectangleF(48f, 64f, width, _panel.DesiredSize.Y));
            _panel.Draw(g);

            // Four and a half rows tall, deliberately. The half is the point: the bottom edge has to fall
            // through the middle of a line of text, so that a scissor which only cut whole rows would be
            // visibly not what we asked for.
            float line = g.LineHeight(MyFontEnum.White, 0.6f);
            float clippedHeight = line * 4.5f + 4f * 4f;
            float clippedTop = _panel.Bounds.Y + _panel.Bounds.Height + 24f;

            _clipped.Measure(g, new Vector2(width, clippedHeight));
            _clipped.Arrange(new RectangleF(48f, clippedTop, width, clippedHeight));
            _clipped.Draw(g);

            var below = Body("Unclipped, below the panel. The rows above are cut on two edges.");
            below.Measure(g, new Vector2(width, size.Y));
            below.Arrange(new RectangleF(48f, clippedTop + clippedHeight + 12f, width, below.DesiredSize.Y));
            below.Draw(g);

            // A material that does not exist, to prove the fallback is loud.
            g.DrawSprite(Unknown, new RectangleF(48f, size.Y - 120f, 220f, 56f), Color.White, 0f);

            if (_pointer.IsAvailable)
                g.DrawSprite(Cursor, new RectangleF(_pointer.Position.X, _pointer.Position.Y, 34f, 34f),
                    Color.White, 0f);
        }

        static void Outline(IGraphics g, RectangleF bounds, Color color, float thickness)
        {
            g.DrawSprite(Fill, new RectangleF(bounds.X, bounds.Y, bounds.Width, thickness), color, 0f);
            g.DrawSprite(Fill, new RectangleF(bounds.X, bounds.Y + bounds.Height - thickness, bounds.Width, thickness), color, 0f);
            g.DrawSprite(Fill, new RectangleF(bounds.X, bounds.Y, thickness, bounds.Height), color, 0f);
            g.DrawSprite(Fill, new RectangleF(bounds.X + bounds.Width - thickness, bounds.Y, thickness, bounds.Height), color, 0f);
        }

        static Label Heading(string text)
        {
            var label = new Label();
            label.Font = MyFontEnum.White;
            label.Scale = 0.9f;
            label.Text = text;
            return label;
        }

        static Label Body(string text)
        {
            var label = new Label();
            label.Font = MyFontEnum.White;
            label.Scale = 0.6f;
            label.Color = new Color(190, 205, 220);
            label.Text = text;
            return label;
        }

        sealed class SimPointer : IPointer
        {
            Vector2 _position;
            bool _pressed;
            bool _wasPressed;
            bool _available;

            public bool IsAvailable
            {
                get { return _available; }
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

            public void MoveTo(Vector2 position, RectangleF bounds)
            {
                _available = true;

                // Clamped to the screen and not to the laid-out area, because the game lets its own cursor
                // reach the physical edges. On anything wider than 4:3 that means negative X is legal.
                if (position.X < bounds.X)
                    position.X = bounds.X;
                if (position.Y < bounds.Y)
                    position.Y = bounds.Y;
                if (position.X > bounds.X + bounds.Width)
                    position.X = bounds.X + bounds.Width;
                if (position.Y > bounds.Y + bounds.Height)
                    position.Y = bounds.Y + bounds.Height;

                _position = position;
            }

            public void SetPressed(bool pressed)
            {
                _wasPressed = pressed && !_pressed;
                _pressed = pressed;
            }
        }
    }
}
