using System.Collections.Generic;
using System.Text;
using Reactor.Host.SpaceEngineers.Drawing;
using Reactor.Sim.Fakes;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Reactor.Sim.Simulation
{
    /// <summary>
    /// The screen, as somewhere to write down what was drawn on it.
    /// </summary>
    /// <remarks>
    /// The mod's own drawing runs above this line - the arithmetic, the clipping, the letterboxing -
    /// and every call it makes to the game's screen lands here as an ordered command instead. That
    /// list is what a reference frame is.
    /// </remarks>
    public sealed class ScreenSpaceDevice : IScreenSpace
    {
        readonly List<DrawCommand> _commands = new List<DrawCommand>();
        readonly List<Rectangle> _scissors = new List<Rectangle>();

        public ScreenSpaceDevice()
        {
            SafeArea = new RectangleF(0f, 0f, 1440f, 1080f);
            ViewportSize = new Vector2(1920f, 1080f);
        }

        /// <summary>
        /// The 4:3 box at full screen height that the game lays its own UI out in. Set once per frame by
        /// whoever owns the window.
        /// </summary>
        public RectangleF SafeArea { get; set; }

        /// <summary>The whole window, which is wider than the safe area on anything but a 4:3 monitor.</summary>
        public Vector2 ViewportSize { get; set; }

        public IFontMetrics Fonts { get; set; }

        /// <summary>This frame's calls, in the order they were made.</summary>
        public IList<DrawCommand> Commands => _commands;

        public void BeginFrame()
        {
            _commands.Clear();
            _scissors.Clear();
        }

        /// <summary>
        /// Puts the harness behind the two members of the gateway a screen reads.
        /// </summary>
        /// <remarks>
        /// Nothing about drawing any more - that arrives through <see cref="IScreenSpace" /> now. What
        /// is left is the game's session, which the mod asks for a frame counter and a viewport.
        /// </remarks>
        public void Install()
        {
            var camera = Pretend.To<IMyCamera>()
                .Gives("ViewportSize", () => ViewportSize)
                .Done();

            MyAPIGateway.Session = Pretend.To<IMySession>()
                .Gives("Camera", () => camera)
                .Done();
        }

        /// <summary>
        /// How many times anything has asked for the width of a string.
        /// </summary>
        /// <remarks>
        /// <b>For measuring, and it is not idle curiosity.</b> Measuring text is the dearest thing the
        /// layout does and the one whose cost no screenshot can show: a layout that re-measures every
        /// label on every frame draws exactly the right picture. The only honest proof is a count, so
        /// the harness keeps one.
        /// </remarks>
        public int Measured { get; private set; }

        public Vector2 MeasureText(string font, StringBuilder text, float scale)
        {
            if (Fonts == null || text == null)
                return Vector2.Zero;

            Measured++;

            // The real one measures the builder without unpacking it, which is the reason a builder is held
            // at all. Here the string is fine: nothing in this process is running inside a game's frame.
            return Fonts.MeasurePixels(font, text.ToString(), scale);
        }

        /// <summary>The height of a line in pixels, which is what the screen answers in.</summary>
        public float LineHeight(string font, float scale)
        {
            return Fonts == null ? 0f : Fonts.LineHeightPixels(font, scale);
        }

        /// <summary>
        /// Always no.
        /// </summary>
        /// <remarks>
        /// The harness drives the editor through its own window and its own recorded frames, and the
        /// only thing that reads this is the game's screen host, which the harness does not run.
        /// </remarks>
        public bool TryCursorPixels(out Vector2 pixels)
        {
            pixels = Vector2.Zero;
            return false;
        }

        public Vector2 NormalizedToPixels(Vector2 position)
        {
            return new Vector2(position.X * ViewportSize.X, position.Y * ViewportSize.Y);
        }

        public Vector2 NormalizedSizeToPixels(Vector2 size)
        {
            return new Vector2(size.X * ViewportSize.X, size.Y * ViewportSize.Y);
        }

        public void DrawSprite(MyStringId material, Rectangle destination, Color color, float rotation,
            MyScreenSpaceLayer layer)
        {
            _commands.Add(new DrawCommand
            {
                Kind = DrawKind.Sprite,
                Layer = layer,
                Material = material,
                Destination = destination,
                Color = color,
                Rotation = rotation
            });
        }

        public void DrawText(string font, string text, Vector2 position, float scale, Color color,
            MyGuiDrawAlignEnum align, float maxWidth, MyScreenSpaceLayer layer)
        {
            _commands.Add(new DrawCommand
            {
                Kind = DrawKind.Text,
                Layer = layer,
                Font = font,
                Text = text,
                Position = position,
                Scale = scale,
                Color = color,
                Align = align,
                MaxWidth = maxWidth
            });
        }

        /// <summary>
        /// Starts a scissor.
        /// </summary>
        /// <remarks>
        /// The stack lives here because that is where the game keeps its own, and because a clip still
        /// has to reach the frame as an ordered command: the renderer replays a list and <c>--dump</c>
        /// reads one, and neither should have to know a stack exists.
        /// </remarks>
        public void PushScissor(Rectangle pixels, MyScreenSpaceLayer layer)
        {
            _scissors.Add(pixels);
            SetClip(pixels, layer);
        }

        /// <summary>Ends the innermost scissor, putting its parent's back rather than clearing outright.</summary>
        public void PopScissor(MyScreenSpaceLayer layer)
        {
            if (_scissors.Count == 0)
                return;

            _scissors.RemoveAt(_scissors.Count - 1);

            if (_scissors.Count > 0)
                SetClip(_scissors[_scissors.Count - 1], layer);
            else
                ClearClip(layer);
        }

        void SetClip(Rectangle rect, MyScreenSpaceLayer layer)
        {
            _commands.Add(new DrawCommand { Kind = DrawKind.SetClip, Layer = layer, Clip = rect });
        }

        void ClearClip(MyScreenSpaceLayer layer)
        {
            _commands.Add(new DrawCommand { Kind = DrawKind.ClearClip, Layer = layer });
        }
    }
}
