using System;
using System.Collections.Generic;
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

        /// <summary>The material every flat fill is drawn with, tinted.</summary>
        readonly MyStringId _fill;

        /// <summary>Each font's line height at a scale of one, in UI units, asked of the game once a font.</summary>
        readonly Dictionary<string, float> _lines = new Dictionary<string, float>();

        /// <summary>Nothing, measured to find how tall a line the game draws.</summary>
        readonly StringBuilder _nothing = new StringBuilder();
        readonly MyScreenSpaceLayer _layer;
        readonly float _referenceHeight;

        readonly IScreenSpace _screenSpace;

        /// <summary>How many scissors this has pushed and not yet popped.</summary>
        int _clips;

        /// <summary>The font last turned into a scale, kept with its line, so a run of text in one font asks nothing.</summary>
        string _lastFont;

        float _lastLine;

        /// <summary>Applied to a font scale on the way out, to undo the game's own scaling and put back this instance's.</summary>
        /// <remarks>The game multiplies every font scale it is handed by its own HUD scale.</remarks>
        float _fontScale = 1f;

        Vector2 _origin;

        /// <summary>UI units to window pixels.</summary>
        float _scale = 1f;

        /// <summary>Draws onto a layer with <paramref name="fill" /> for flat fills, at a scale set by the height a layout is written against.</summary>
        /// <remarks>
        ///     The fill is a plain white transparent material. The game declares none a mod can use, so the mod
        ///     declares its own over <c>Textures\GUI\Blank.dds</c>.
        /// </remarks>
        public ScreenSpaceGraphics(IScreenSpace screenSpace, MyScreenSpaceLayer layer, MyStringId fill,
            float referenceHeight = GameReferenceHeight)
        {
            _screenSpace = screenSpace;
            _layer = layer;
            _fill = fill;
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
        public void Fill(RectangleF destination, Color color, float rotation = 0f)
        {
            DrawSprite(_fill, destination, color, rotation);
        }

        /// <inheritdoc />
        public void DrawText(string font, string text, Vector2 position, float size, Color color)
        {
            DrawText(font, text, position, size, color, Align.Left);
        }

        /// <inheritdoc />
        public void DrawText(string font, string text, Vector2 position, float size, Color color,
            Align align)
        {
            if (string.IsNullOrEmpty(text))
                return;

            // Always the top, because a position in the layout is the top of the run.
            var placed = align == Align.Right
                ? MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP
                : MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;

            _screenSpace.DrawText(font, text, position * _scale + _origin, ScaleOf(font, size) * _fontScale, color,
                placed, float.PositiveInfinity, _layer);
        }

        /// <inheritdoc />
        public Vector2 MeasureText(string font, StringBuilder text, float size)
        {
            return _screenSpace.MeasureText(font, text, ScaleOf(font, size) * _fontScale) / _scale;
        }

        /// <summary>Samples the safe area and drops any clip left over.</summary>
        /// <remarks>Called at the top of every draw.</remarks>
        public void Begin()
        {
            var area = _screenSpace.SafeArea;

            // What the game is already applying to every font scale it is handed.
            var gameScale = area.Height > 0f ? area.Height / GameReferenceHeight : 1f;

            _origin = new Vector2(area.X, area.Y);

            var scale = _referenceHeight > 0f && area.Height > 0f ? area.Height / _referenceHeight : 1f;
            var fontScale = gameScale > 0f ? scale / gameScale : 1f;

            if (scale != _scale || fontScale != _fontScale)
            {
                _lines.Clear();
                _lastFont = null;
            }

            _scale = scale;
            _fontScale = fontScale;
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

        /// <summary>The scale that draws <paramref name="font" /> with lines <paramref name="size" /> tall.</summary>
        /// <remarks>
        ///     The line is measured rather than asked for: the game's <c>MyScreenSpaceText.GetLineHeight</c> leaves out
        ///     the <c>FONT_SCALE</c> its drawing and measuring apply, and so answers 37/28.8 of the line it draws.
        /// </remarks>
        float ScaleOf(string font, float size)
        {
            if (!ReferenceEquals(font, _lastFont))
            {
                float line;
                if (!_lines.TryGetValue(font, out line))
                {
                    line = _screenSpace.MeasureText(font, _nothing, _fontScale).Y / _scale;
                    _lines[font] = line;
                }

                _lastFont = font;
                _lastLine = line;
            }

            return _lastLine > 0f ? size / _lastLine : 0f;
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