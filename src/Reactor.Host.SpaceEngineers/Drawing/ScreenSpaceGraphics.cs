using System;

// Needed by one of the two projects this shared code is compiled into and not the other, so an
// inspection run against either will call it redundant. It is not.
// ReSharper disable once RedundantUsingDirective
using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Reactor.Host.SpaceEngineers.Drawing
{
    /// <summary>
    /// Draws through <c>MyScreenSpaceText</c> and <c>MyScreenSpaceSprite</c>, on the screen rather than
    /// onto a block.
    /// </summary>
    /// <remarks>
    /// Its coordinates are a <b>fixed UI size</b>, scaled to whatever the screen happens to be, which is
    /// what the game does: the safe GUI rectangle is 4:3 at the full screen height, and the HUD's scale is
    /// that height over 1080. So this draws into roughly 1440x1080 whatever the resolution.
    /// <para>
    /// The height everything is measured against is a constructor argument, and
    /// <see cref="NativePixels" /> turns the scaling off and works in real window pixels instead. The game
    /// multiplies a font scale by its own factor before drawing, so text is corrected by however far the
    /// chosen height sits from the game's, and text and layout move together either way.
    /// </para>
    /// </remarks>
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

        private readonly IScreenSpace _screenSpace;
        private readonly MyScreenSpaceLayer _layer;
        private readonly ClipStack _clip = new ClipStack();
        private readonly float _referenceHeight;

        /// <summary>How many clips are in force, so one is only ever popped if it was pushed.</summary>
        private int _clips;

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
        public ScreenSpaceGraphics(IScreenSpace screenSpace, MyScreenSpaceLayer layer)
            : this(screenSpace, layer, GameReferenceHeight)
        {
        }

        /// <summary>
        /// Draws onto a layer, at a scale set by the height a layout is written against.
        /// </summary>
        /// <remarks>
        /// <paramref name="referenceHeight" /> is stretched to the screen.
        /// <see cref="NativePixels" /> for no scaling at all.
        /// </remarks>
        public ScreenSpaceGraphics(IScreenSpace screenSpace, MyScreenSpaceLayer layer,
            float referenceHeight)
        {
            _screenSpace = screenSpace;
            _layer = layer;
            _referenceHeight = referenceHeight;
            Begin();
        }

        /// <summary>
        /// Samples the safe area and drops any clip left over. Call once at the top of a frame.
        /// </summary>
        /// <remarks>
        /// Not on <see cref="IGraphics" />, because a frame is opened differently on either target and the
        /// thing being drawn should not know which. A text panel backend would open its <c>DrawFrame</c>
        /// here instead.
        /// </remarks>
        public void Begin()
        {
            RectangleF area = _screenSpace.SafeArea;

            // What the game is already applying to every font scale it is handed.
            float gameScale = area.Height > 0f ? area.Height / GameReferenceHeight : 1f;

            _origin = new Vector2(area.X, area.Y);
            _scale = _referenceHeight > 0f && area.Height > 0f ? area.Height / _referenceHeight : 1f;
            _fontScale = gameScale > 0f ? _scale / gameScale : 1f;
            _size = new Vector2(area.Width / _scale, area.Height / _scale);

            // The whole window, in the same units, which is a larger rectangle than the safe area on
            // anything wider than 4:3 and starts at a negative X. A pointer belongs in this one: the game
            // lets its own cursor reach the physical edges of the screen, not the edges of its HUD.
            Vector2 viewport = _screenSpace.ViewportSize;

            _screen = new RectangleF(
                -_origin.X / _scale,
                -_origin.Y / _scale,
                viewport.X / _scale,
                viewport.Y / _scale);

            CloseScissors();
            _clip.Clear();
        }

        public Vector2 Size => _size;

        /// <summary>
        /// The whole window in UI units, which contains <see cref="Size" /> and extends past it on a screen
        /// wider than 4:3. Laying out uses <see cref="Size" />; anything that should be able to reach the
        /// physical edge of the screen, a pointer above all, uses this.
        /// </summary>
        public RectangleF ScreenBounds => _screen;

        /// <summary>
        /// Narrows drawing to <paramref name="bounds" />, intersected with whatever is already in force.
        /// </summary>
        /// <remarks>
        /// Nested clips intersect, and the screen does that for free - but it is still
        /// <see cref="ClipStack.Current" /> that gets handed over rather than the raw bounds. Narrowing
        /// an already-narrowed rectangle by its parent gives the same rectangle back, and it keeps the
        /// promise the seam makes to both targets: that a screen only ever honours one rectangle.
        /// <para>
        /// <b>The layer is not optional.</b> Everything here draws on whichever layer this instance was
        /// built for, so leaving it out would push a scissor onto a layer nothing is drawn to and
        /// quietly clip nothing at all.
        /// </para>
        /// </remarks>
        public void PushClip(RectangleF bounds)
        {
            _clip.Push(bounds);
            _screenSpace.PushScissor(ToPixels(_clip.Current), _layer);
            _clips++;
        }

        /// <summary>Drops the innermost clip, leaving its parent's standing.</summary>
        /// <remarks>
        /// Pairing is guaranteed by <c>Element.Draw</c> not being virtual: it pushes and pops around
        /// <c>DrawContent</c>, so a subclass cannot get between them and these are strictly nested.
        /// </remarks>
        public void PopClip()
        {
            if (_clips > 0)
            {
                _screenSpace.PopScissor(_layer);
                _clips--;
            }

            _clip.Pop();
        }

        public void DrawSprite(MyStringId material, RectangleF destination, Color color, float rotation)
        {
            if (destination.Width <= 0f || destination.Height <= 0f)
                return;

            Rectangle target = ToPixels(destination);
            if (target.Width <= 0 || target.Height <= 0)
                return;

            _screenSpace.DrawSprite(material, target, color, rotation, _layer);
        }

        public void DrawText(string font, string text, Vector2 position, float scale, Color color)
        {
            if (string.IsNullOrEmpty(text))
                return;

            _screenSpace.DrawText(font, text, position * _scale + _origin, scale * _fontScale, color,
                MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, float.PositiveInfinity, _layer);
        }

        public Vector2 MeasureText(string font, StringBuilder text, float scale)
        {
            return _screenSpace.MeasureText(font, text, scale * _fontScale) / _scale;
        }

        public float LineHeight(string font, float scale)
        {
            // The screen answers in pixels; everything above this line is in UI units.
            return _screenSpace.LineHeight(font, scale * _fontScale) / _scale;
        }

        /// <summary>
        /// Ends every scissor still standing, innermost first. For the start of a frame, where a clip left
        /// open by an abandoned draw would otherwise narrow the next one.
        /// </summary>
        private void CloseScissors()
        {
            while (_clips > 0)
            {
                _screenSpace.PopScissor(_layer);
                _clips--;
            }
        }

        /// <summary>
        /// Moves a point from window pixels back into UI units.
        /// </summary>
        /// <remarks>
        /// The inverse of the position half of <see cref="ToPixels" />, and it does not round: a pointer is
        /// allowed to sit between two units, and rounding one to a whole unit would make the last pixel of
        /// travel before a boundary do nothing.
        /// </remarks>
        public Vector2 FromPixels(Vector2 pixels)
        {
            return _scale > 0f ? (pixels - _origin) / _scale : pixels;
        }

        /// <summary>
        /// Moves a size from window pixels back into UI units. A size has no origin, which is the whole
        /// difference between this and <see cref="FromPixels" />.
        /// </summary>
        public Vector2 FromPixelSize(Vector2 pixels)
        {
            return _scale > 0f ? pixels / _scale : pixels;
        }

        /// <summary>
        /// Moves a rectangle from UI units into window pixels, rounding <b>where it is</b> and <b>how big
        /// it is</b> separately.
        /// </summary>
        /// <remarks>
        /// The point of doing it this way is that the same size always comes out the same number of
        /// pixels, wherever on the screen it happens to sit. That sounds like a detail and is not: a
        /// control is several rectangles, a screen holds several of that control, and a UI unit is not a
        /// whole pixel at most of the sizes a monitor comes in.
        /// <para>
        /// <b>Not rounded outward</b> - floor the near edge, ceil the far one - which is the obvious way
        /// to guarantee two things meant to touch never leave a seam, and costs more than it buys: a box
        /// twenty-nine and a half units tall comes out thirty pixels or twenty-nine depending on whether
        /// it lands on a half, so the same switch drawn in two rows of different heights is a pixel
        /// different in each, and a one-unit border comes out two pixels on the sides where its span
        /// crosses a boundary and one on the sides where it does not.
        /// </para>
        /// <para>
        /// Rounding position and size apart cannot do that. It can in principle leave a seam where two
        /// rectangles abut on a fraction, and in practice does not: both edges of the join round the same
        /// way, because both are the same number.
        /// </para>
        /// </remarks>
        private Rectangle ToPixels(RectangleF bounds)
        {
            // Floor of plus a half rather than Math.Round, which rounds a half to the nearest even number
            // and would make the answer depend on which side of the screen a thing is on all over again.
            int left = (int)Math.Floor(bounds.X * _scale + _origin.X + 0.5f);
            int top = (int)Math.Floor(bounds.Y * _scale + _origin.Y + 0.5f);
            int width = (int)Math.Floor(bounds.Width * _scale + 0.5f);
            int height = (int)Math.Floor(bounds.Height * _scale + 0.5f);

            return new Rectangle(left, top, width, height);
        }
    }
}
