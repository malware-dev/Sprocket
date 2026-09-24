using System;
using System.Text;
using Sandbox.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui.Drawing
{
    /// <summary>Draws on the game's screen in a fixed UI size, scaled to whatever the screen is.</summary>
    /// <remarks>
    ///     The game's safe GUI area is 4:3 at the full screen height, so at the game's reference height
    ///     this draws into roughly 1440x1080 whatever the resolution.
    /// </remarks>
    public sealed class ScreenSpaceGraphics : IGraphics
    {
        /// <summary>The height the game's own UI is designed at.</summary>
        /// <remarks>
        ///     Transcribed from <c>MyGuiManager</c>, whose HUD scale is the screen height over this and which a
        ///     mod cannot read.
        /// </remarks>
        public const float GameReferenceHeight = 1080f;

        /// <summary>Pass as the reference height to work in real window pixels, with nothing scaled.</summary>
        public const float NativePixels = 0f;

        readonly ClipStack _clip = new ClipStack();
        readonly MyScreenSpaceLayer _layer;
        readonly float _referenceHeight;

        readonly IScreenSpace _screenSpace;

        /// <summary>How many scissors this has pushed and not yet popped.</summary>
        int _clips;

        /// <summary>Applied to a font scale on the way out, to undo the game's own scaling and put back this instance's.</summary>
        /// <remarks>The game multiplies every font scale it is handed by its own HUD scale.</remarks>
        float _fontScale = 1f;

        Vector2 _origin;

        /// <summary>UI units to window pixels.</summary>
        float _scale = 1f;

        /// <summary>Draws onto a layer, at a scale set by the height a layout is written against.</summary>
        public ScreenSpaceGraphics(IScreenSpace screenSpace, MyScreenSpaceLayer layer,
            float referenceHeight = GameReferenceHeight)
        {
            _screenSpace = screenSpace;
            _layer = layer;
            _referenceHeight = referenceHeight;
            Begin();
        }

        /// <summary>The whole window in UI units, which extends past <see cref="Size" /> on a screen wider than 4:3.</summary>
        /// <remarks>The game's own cursor reaches the edges of this, not of <see cref="Size" />.</remarks>
        public RectangleF ScreenBounds { get; private set; }

        /// <inheritdoc />
        public Vector2 Size { get; private set; }

        /// <summary>Narrows drawing to <paramref name="bounds" />, intersected with whatever is already in force.</summary>
        /// <remarks>A scissor applies only to the layer it is pushed on.</remarks>
        public void PushClip(RectangleF bounds)
        {
            _clip.Push(bounds);
            _screenSpace.PushScissor(ToPixels(_clip.Current), _layer);
            _clips++;
        }

        /// <summary>Drops the innermost clip, leaving its parent's standing.</summary>
        public void PopClip()
        {
            if (_clips > 0)
            {
                _screenSpace.PopScissor(_layer);
                _clips--;
            }

            _clip.Pop();
        }

        /// <inheritdoc />
        public void DrawSprite(MyStringId material, RectangleF destination, Color color, float rotation)
        {
            if (destination.Width <= 0f || destination.Height <= 0f)
                return;

            var target = ToPixels(destination);
            if (target.Width <= 0 || target.Height <= 0)
                return;

            _screenSpace.DrawSprite(material, target, color, rotation, _layer);
        }

        /// <inheritdoc />
        public void DrawText(string font, string text, Vector2 position, float scale, Color color)
        {
            DrawText(font, text, position, scale, color, Align.Left);
        }

        /// <inheritdoc />
        public void DrawText(string font, string text, Vector2 position, float scale, Color color,
            Align align)
        {
            if (string.IsNullOrEmpty(text))
                return;

            // Always the top, because a position in the layout is the top of the run.
            var placed = align == Align.Right
                ? MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP
                : MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;

            _screenSpace.DrawText(font, text, position * _scale + _origin, scale * _fontScale, color,
                placed, float.PositiveInfinity, _layer);
        }

        /// <inheritdoc />
        public Vector2 MeasureText(string font, StringBuilder text, float scale)
        {
            return _screenSpace.MeasureText(font, text, scale * _fontScale) / _scale;
        }

        /// <inheritdoc />
        public float LineHeight(string font, float scale)
        {
            return _screenSpace.LineHeight(font, scale * _fontScale) / _scale;
        }

        /// <summary>Samples the safe area and drops any clip left over.</summary>
        /// <remarks>Called at the top of every draw.</remarks>
        public void Begin()
        {
            var area = _screenSpace.SafeArea;

            // What the game is already applying to every font scale it is handed.
            var gameScale = area.Height > 0f ? area.Height / GameReferenceHeight : 1f;

            _origin = new Vector2(area.X, area.Y);
            _scale = _referenceHeight > 0f && area.Height > 0f ? area.Height / _referenceHeight : 1f;
            _fontScale = gameScale > 0f ? _scale / gameScale : 1f;
            Size = new Vector2(area.Width / _scale, area.Height / _scale);

            // Starts at a negative X on anything wider than 4:3.
            var viewport = _screenSpace.ViewportSize;

            ScreenBounds = new RectangleF(
                -_origin.X / _scale,
                -_origin.Y / _scale,
                viewport.X / _scale,
                viewport.Y / _scale);

            CloseScissors();
            _clip.Clear();
        }

        /// <summary>Ends every scissor still standing, innermost first.</summary>
        /// <remarks>A clip left open by an abandoned draw would otherwise narrow the next frame.</remarks>
        void CloseScissors()
        {
            while (_clips > 0)
            {
                _screenSpace.PopScissor(_layer);
                _clips--;
            }
        }

        /// <summary>Moves a point from window pixels back into UI units.</summary>
        public Vector2 FromPixels(Vector2 pixels)
        {
            return _scale > 0f ? (pixels - _origin) / _scale : pixels;
        }

        /// <summary>Moves a size from window pixels back into UI units.</summary>
        public Vector2 FromPixelSize(Vector2 pixels)
        {
            return _scale > 0f ? pixels / _scale : pixels;
        }

        /// <summary>Moves a rectangle from UI units into window pixels.</summary>
        /// <remarks>
        ///     Position and size are rounded apart, not the edges outward, so that the same size is the same
        ///     number of pixels wherever it sits.
        /// </remarks>
        Rectangle ToPixels(RectangleF bounds)
        {
            // Not Math.Round, which rounds a half to the nearest even number.
            var left = (int)Math.Floor(bounds.X * _scale + _origin.X + 0.5f);
            var top = (int)Math.Floor(bounds.Y * _scale + _origin.Y + 0.5f);
            var width = (int)Math.Floor(bounds.Width * _scale + 0.5f);
            var height = (int)Math.Floor(bounds.Height * _scale + 0.5f);

            return new Rectangle(left, top, width, height);
        }
    }
}