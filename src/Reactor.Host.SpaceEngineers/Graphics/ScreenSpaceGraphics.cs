using System;
using System.Text;
using Sandbox.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Phosphor
{
    /// <summary>
    /// Draws through <c>MyScreenSpaceText</c> and <c>MyScreenSpaceSprite</c>, over the world rather than onto
    /// a block.
    /// <para>
    /// Its coordinates are a <b>fixed UI size</b>, scaled to whatever the screen happens to be, which is what
    /// the game itself does: the safe GUI rectangle is 4:3 at the full screen height, and the HUD's scale is
    /// that height over 1080. So this draws into roughly 1440x1080 whatever the resolution, exactly as a text
    /// panel draws into its own unchanging pixel size.
    /// </para>
    /// <para>
    /// That is the default because it is what the game does, not because it is the only option. The height
    /// everything is measured against is a constructor argument, and <see cref="NativePixels" /> turns the
    /// scaling off and works in real window pixels instead. Whatever is chosen stays self-consistent: the
    /// game multiplies a font scale by its own factor before drawing, so text is corrected by however far
    /// the chosen height sits from the game's, and text and layout move together either way.
    /// </para>
    /// </summary>
    public sealed class ScreenSpaceGraphics : IGraphics
    {
        /// <summary>
        /// The height the game's own UI is designed at. <c>MyGuiManager</c> computes its HUD scale as the
        /// screen height over this, and that is not reachable from a mod, so it is reproduced here. If it
        /// ever changes, this is the line.
        /// </summary>
        public const float GameReferenceHeight = 1080f;

        /// <summary>
        /// Pass as the reference height to work in real window pixels, with nothing scaled. The drawable area
        /// then changes size with the monitor, which is a thing to want deliberately rather than by default.
        /// </summary>
        public const float NativePixels = 0f;

        private readonly MyScreenSpaceLayer _layer;
        private readonly ClipStack _clip = new ClipStack();
        private readonly float _referenceHeight;

        private Vector2 _origin;
        private Vector2 _size;
        private RectangleF _screen;

        /// <summary>UI units to window pixels.</summary>
        private float _scale = 1f;

        /// <summary>
        /// Applied to a font scale on the way out, to undo the game's own scaling and put back this
        /// instance's. One when the two agree, which is the default case.
        /// </summary>
        private float _fontScale = 1f;

        /// <summary>Scales the way the game's own HUD does.</summary>
        public ScreenSpaceGraphics(MyScreenSpaceLayer layer)
            : this(layer, GameReferenceHeight)
        {
        }

        /// <param name="referenceHeight">
        /// The height a layout is written against, stretched to the screen. <see cref="NativePixels" /> for
        /// no scaling at all.
        /// </param>
        public ScreenSpaceGraphics(MyScreenSpaceLayer layer, float referenceHeight)
        {
            _layer = layer;
            _referenceHeight = referenceHeight;
            Begin();
        }

        /// <summary>
        /// Samples the safe area and drops any clip left over. Call once at the top of a frame.
        /// <para>
        /// Not on <see cref="IGraphics" />, because a frame is opened differently on either target and the
        /// thing being drawn should not know which. A text panel backend would open its <c>DrawFrame</c>
        /// here instead.
        /// </para>
        /// </summary>
        public void Begin()
        {
            RectangleF area = MyScreenSpace.GetSafeGuiArea();

            // What the game is already applying to every font scale it is handed.
            float gameScale = area.Height > 0f ? area.Height / GameReferenceHeight : 1f;

            _origin = new Vector2(area.X, area.Y);
            _scale = _referenceHeight > 0f && area.Height > 0f ? area.Height / _referenceHeight : 1f;
            _fontScale = gameScale > 0f ? _scale / gameScale : 1f;
            _size = new Vector2(area.Width / _scale, area.Height / _scale);

            // The whole window, in the same units, which is a larger rectangle than the safe area on
            // anything wider than 4:3 and starts at a negative X. A pointer belongs in this one: the game
            // lets its own cursor reach the physical edges of the screen, not the edges of its HUD.
            Vector2 viewport = MyAPIGateway.Session != null && MyAPIGateway.Session.Camera != null
                ? MyAPIGateway.Session.Camera.ViewportSize
                : new Vector2(area.Width, area.Height);

            _screen = new RectangleF(
                -_origin.X / _scale,
                -_origin.Y / _scale,
                viewport.X / _scale,
                viewport.Y / _scale);

            _clip.Clear();
            ApplyClip();
        }

        public Vector2 Size
        {
            get { return _size; }
        }

        /// <summary>
        /// The whole window in UI units, which contains <see cref="Size" /> and extends past it on a screen
        /// wider than 4:3. Laying out uses <see cref="Size" />; anything that should be able to reach the
        /// physical edge of the screen, a pointer above all, uses this.
        /// </summary>
        public RectangleF ScreenBounds
        {
            get { return _screen; }
        }

        public void PushClip(RectangleF bounds)
        {
            _clip.Push(bounds);
            ApplyClip();
        }

        public void PopClip()
        {
            _clip.Pop();
            ApplyClip();
        }

        public void DrawSprite(MyStringId material, RectangleF destination, Color color, float rotation)
        {
            if (destination.Width <= 0f || destination.Height <= 0f)
                return;

            Rectangle target = ToPixels(destination);
            if (target.Width <= 0 || target.Height <= 0)
                return;

            MyScreenSpaceSprite.DrawPixels(material, target, null, color, rotation, _layer);
        }

        public void DrawText(string font, string text, Vector2 position, float scale, Color color)
        {
            if (string.IsNullOrEmpty(text))
                return;

            MyScreenSpaceText.DrawPixels(font, text, position * _scale + _origin, scale * _fontScale, color,
                MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, float.PositiveInfinity, _layer);
        }

        public Vector2 MeasureText(string font, StringBuilder text, float scale)
        {
            return MyScreenSpaceText.MeasurePixels(font, text, scale * _fontScale) / _scale;
        }

        public float LineHeight(string font, float scale)
        {
            // GetLineHeight answers in normalized units; everything above this line is in UI units.
            return MyScreenSpace.NormalizedSizeToPixels(
                new Vector2(0f, MyScreenSpaceText.GetLineHeight(font, scale * _fontScale))).Y / _scale;
        }

        /// <summary>
        /// Hands the clip now in force to the game, or clears it when the stack has emptied.
        /// <para>
        /// <b>The one thing here that is not written yet.</b> The screen-space API is expected to carry a
        /// scissor, the way the panel API carries <c>MySprite.CreateClipRect</c>, and the two calls below are
        /// where it goes. It has to be ordered with the draws rather than set as state, because the buffered
        /// layers replay their queue later, which is also why this is called on every push and pop rather
        /// than read back at draw time.
        /// </para>
        /// <para>
        /// Everything above assumes it works. There is deliberately no fallback: approximating a scissor
        /// means sprites cut by source rectangle and text that can only be shortened on the right, which
        /// would put a rule about scrolling by whole rows into the framework to serve a gap that is not
        /// meant to be there.
        /// </para>
        /// </summary>
        private void ApplyClip()
        {
            if (_clip.HasClip)
                SetClip(ToPixels(_clip.Current));
            else
                ClearClip();
        }

        /// <summary>Narrows drawing to <paramref name="rect" />, in window pixels. One line, pending a name.</summary>
        private void SetClip(Rectangle rect)
        {
            // MyScreenSpaceSprite.SetClip(rect, _layer);
        }

        /// <summary>Removes the clip entirely. One line, pending a name.</summary>
        private void ClearClip()
        {
            // MyScreenSpaceSprite.ClearClip(_layer);
        }

        /// <summary>
        /// Moves a rectangle from UI units into window pixels, rounded outward so a fractional edge never
        /// leaves a seam between two things that were meant to meet.
        /// </summary>
        private Rectangle ToPixels(RectangleF bounds)
        {
            int left = (int)Math.Floor(bounds.X * _scale + _origin.X);
            int top = (int)Math.Floor(bounds.Y * _scale + _origin.Y);
            int right = (int)Math.Ceiling((bounds.X + bounds.Width) * _scale + _origin.X);
            int bottom = (int)Math.Ceiling((bounds.Y + bounds.Height) * _scale + _origin.Y);

            return new Rectangle(left, top, right - left, bottom - top);
        }
    }
}
