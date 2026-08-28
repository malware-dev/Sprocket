using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Reactor.Sim.Simulation
{
    public enum DrawKind
    {
        Sprite,
        Text,
        SetClip,
        ClearClip
    }

    /// <summary>One thing the game was asked to draw, kept as data rather than done immediately.</summary>
    /// <remarks>
    /// Recorded rather than drawn for two reasons. The near one is mechanical: a canvas only hands out
    /// something to draw with inside its own render pass, while Phosphor lays out and draws in one go and
    /// should not have to care. The far one is the useful one - a frame that exists as a list can be
    /// compared to another frame, which is how this stops being something to look at and starts being
    /// something to assert on.
    /// </remarks>
    public struct DrawCommand
    {
        public DrawKind Kind;
        public MyScreenSpaceLayer Layer;

        public MyStringId Material;
        public Rectangle Destination;
        public Rectangle? Source;
        public float Rotation;

        public string Font;
        public string Text;
        public Vector2 Position;
        public float Scale;
        public MyGuiDrawAlignEnum Align;
        public float MaxWidth;

        public Color Color;
        public Rectangle Clip;
    }

    /// <summary>Whatever can tell us how big text is. Supplied by the renderer, since only it has a font.</summary>
    public interface IFontMetrics
    {
        /// <summary>The size this run would occupy, in window pixels.</summary>
        Vector2 MeasurePixels(string font, string text, float scale);

        /// <summary>The height of one line, in window pixels, including leading.</summary>
        float LineHeightPixels(string font, float scale);
    }

    /// <summary>
    /// Stands in for the game's screen-space drawing. The substitute statics in <c>Sandbox.ModAPI</c> do
    /// nothing but forward here, so what runs above this line is the real backend, arithmetic and all.
    /// </summary>
    public sealed class ScreenSpaceDevice
    {
        /// <summary>
        /// Where the substitute statics send their calls. Static because the game's are, and the shape of
        /// the thing being simulated is not ours to improve.
        /// </summary>
        public static ScreenSpaceDevice Current = new ScreenSpaceDevice();

        readonly List<DrawCommand> _commands = new List<DrawCommand>();
        readonly List<Rectangle> _scissors = new List<Rectangle>();

        public ScreenSpaceDevice()
        {
            SafeGuiArea = new RectangleF(0f, 0f, 1440f, 1080f);
            ViewportSize = new Vector2(1920f, 1080f);
        }

        /// <summary>
        /// The 4:3 box at full screen height that the game lays its own UI out in. Set once per frame by
        /// whoever owns the window.
        /// </summary>
        public RectangleF SafeGuiArea { get; set; }

        /// <summary>The whole window, which is wider than the safe area on anything but a 4:3 monitor.</summary>
        public Vector2 ViewportSize { get; set; }

        public IFontMetrics Fonts { get; set; }

        /// <summary>This frame's calls, in the order they were made.</summary>
        public IList<DrawCommand> Commands
        {
            get { return _commands; }
        }

        public void BeginFrame()
        {
            _commands.Clear();
            _scissors.Clear();
        }

        /// <summary>
        /// Puts this device behind the substitute statics, and behind the two members of the gateway the
        /// backend reads. Everything that pretends to be the game is set up from here, so a headless frame
        /// and a drawn one start from the same state rather than from two copies of it.
        /// </summary>
        public void Install()
        {
            Current = this;
            MyAPIGateway.Session = new SimSession(this);
        }

        sealed class SimSession : IMySession
        {
            readonly SimCamera _camera;

            public SimSession(ScreenSpaceDevice device)
            {
                _camera = new SimCamera(device);
            }

            public IMyCamera Camera
            {
                get { return _camera; }
            }
        }

        sealed class SimCamera : IMyCamera
        {
            readonly ScreenSpaceDevice _device;

            public SimCamera(ScreenSpaceDevice device)
            {
                _device = device;
            }

            public Vector2 ViewportSize
            {
                get { return _device.ViewportSize; }
            }
        }

        public Vector2 NormalizedSizeToPixels(Vector2 size)
        {
            return new Vector2(size.X * ViewportSize.X, size.Y * ViewportSize.Y);
        }

        public Vector2 MeasureText(string font, StringBuilder text, float scale)
        {
            if (Fonts == null || text == null)
                return Vector2.Zero;

            // The real one measures the builder without unpacking it, which is the reason Phosphor holds one
            // at all. Here the string is fine: nothing in this process is running inside a game's frame.
            return Fonts.MeasurePixels(font, text.ToString(), scale);
        }

        /// <summary>
        /// The height of a line, normalized, because that is the unit the game answers in and the backend
        /// converts it back through <see cref="NormalizedSizeToPixels" />. Kept in the same units on purpose
        /// so that round trip is genuinely exercised rather than quietly bypassed.
        /// </summary>
        public float LineHeight(string font, float scale)
        {
            if (Fonts == null || ViewportSize.Y <= 0f)
                return 0f;

            return Fonts.LineHeightPixels(font, scale) / ViewportSize.Y;
        }

        public void DrawSprite(MyStringId material, Rectangle destination, Rectangle? source, Color color,
            float rotation, MyScreenSpaceLayer layer)
        {
            _commands.Add(new DrawCommand
            {
                Kind = DrawKind.Sprite,
                Layer = layer,
                Material = material,
                Destination = destination,
                Source = source,
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
        /// Starts a scissor, and answers whether it took, as the game's does.
        /// </summary>
        /// <remarks>
        /// The stack lives here rather than in the caller because that is where the game keeps its own, and
        /// because a clip still has to reach the frame as an ordered command: the renderer replays a list
        /// and <c>--dump</c> reads one, and neither should have to know a stack exists.
        /// </remarks>
        public bool PushScissor(Rectangle rect, MyScreenSpaceLayer layer)
        {
            _scissors.Add(rect);
            SetClip(rect, layer);
            return true;
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
