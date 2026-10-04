using System;
using System.Collections.Generic;
using System.Text;
using Lumel.Images;
using Sandbox.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Lumel.Drawing
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

        /// <summary>How far below where it is told the game draws every glyph, in the font's own units.</summary>
        /// <remarks>
        ///     Transcribed from the game's <c>MyRenderGuiConstants.FONT_TOP_SIDE_BEARING</c>, which the renderer
        ///     adds to every glyph whatever the font. Taken back off here, so a font's line sits where its
        ///     own file puts it.
        /// </remarks>
        public const float TopSideBearing = 3f * 23f / 18f;

        readonly ClipStack _clip = new ClipStack();

        readonly MyStringId _fill;

        readonly Dictionary<string, float> _lines = new Dictionary<string, float>();
        readonly Dictionary<string, float> _lifts = new Dictionary<string, float>();

        readonly StringBuilder _nothing = new StringBuilder();
        readonly MyScreenSpaceLayer _layer;
        readonly float _referenceHeight;

        readonly IScreenSpace _screenSpace;

        readonly List<Vector3> _transforms = new List<Vector3>();

        int _clips;

        string _lastFont;

        float _lastLine;

        float _lastLift;

        float _fontScale = 1f;

        Vector2 _origin;

        float _scale = 1f;

        float _transformScale = 1f;
        Vector2 _transformMove;
        bool _transformed;

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
            Images = new ScreenSpaceImages(screenSpace);
            Begin();
        }

        /// <inheritdoc />
        /// <remarks>Each picture is looked up once for as long as this lives.</remarks>
        public IImageCatalog Images { get; }

        /// <summary>The whole window in UI units, which extends past <see cref="Size" /> on a screen wider than 4:3.</summary>
        /// <remarks>The game's own cursor reaches the edges of this, not of <see cref="Size" />.</remarks>
        public RectangleF ScreenBounds { get; private set; }

        /// <inheritdoc />
        public Vector2 Size { get; private set; }

        /// <summary>Narrows drawing to <paramref name="bounds" />, intersected with whatever is already in force.</summary>
        /// <remarks>A scissor applies only to the layer it is pushed on.</remarks>
        public void PushClip(RectangleF bounds)
        {
            _clip.Push(Transformed(bounds));
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
        public void PushTransform(RenderTransform transform, Vector2 origin)
        {
            // The one in force is held as a scale and a move, a point p landing at p * scale + move, and the
            // ones around it kept flat in a list that is reused, because this is on a frame's drawing path.
            _transforms.Add(new Vector3(_transformScale, _transformMove.X, _transformMove.Y));

            // The element's own transform, about its origin, as a scale and a move, and then the one around it.
            var scale = transform.Scale;
            var move = origin - origin * scale + transform.Translate;

            _transformMove = move * _transformScale + _transformMove;
            _transformScale *= scale;
            _transformed = _transformScale != 1f || _transformMove.X != 0f || _transformMove.Y != 0f;
        }

        /// <inheritdoc />
        public void PopTransform()
        {
            var last = _transforms.Count - 1;
            if (last < 0)
                return;

            var held = _transforms[last];
            _transforms.RemoveAt(last);

            _transformScale = held.X;
            _transformMove = new Vector2(held.Y, held.Z);
            _transformed = _transformScale != 1f || _transformMove.X != 0f || _transformMove.Y != 0f;
        }

        /// <inheritdoc />
        public void DrawSprite(MyStringId material, RectangleF destination, Color color, float rotation)
        {
            if (destination.Width <= 0f || destination.Height <= 0f)
                return;

            var target = ToPixels(Transformed(destination));
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
            DrawText(font, text, position, size, color, TextAlign.Left);
        }

        /// <inheritdoc />
        public void DrawText(string font, string text, Vector2 position, float size, Color color,
            TextAlign align)
        {
            if (string.IsNullOrEmpty(text))
                return;

            // Always the top, because a position in the layout is the top of the run.
            var placed = align == TextAlign.Right
                ? MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP
                : MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;

            if (_transformed)
            {
                position = position * _transformScale + _transformMove;
                size *= _transformScale;
            }

            var scale = ScaleOf(font, size);
            position.Y -= size * _lastLift;

            _screenSpace.DrawText(font, text, position * _scale + _origin, scale * _fontScale, color,
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

            _transforms.Clear();
            _transformScale = 1f;
            _transformMove = Vector2.Zero;
            _transformed = false;
        }

        /// <summary>The scale that draws <paramref name="font" /> with lines <paramref name="size" /> tall.</summary>
        /// <remarks>
        ///     The line is measured, by the same call that measures every run, so a line is laid out at exactly the
        ///     height its text measures. Measured once per font.
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
                _lastLift = Lift(font);
            }

            return _lastLine > 0f ? size / _lastLine : 0f;
        }

        /// <summary>The game's <see cref="TopSideBearing" /> for <paramref name="font" />, as a share of one of its lines.</summary>
        /// <remarks>
        ///     A share of the line because the line is what a size is: the game scales the bearing and the line
        ///     alike, so a run <c>size</c> tall is drawn <c>size</c> times this too low. Nothing for a font
        ///     whose line cannot be read. Kept per font, since reading one opens its file.
        /// </remarks>
        float Lift(string font)
        {
            if (font == null)
                return 0f;

            float lift;
            if (_lifts.TryGetValue(font, out lift))
                return lift;

            var units = _screenSpace.LineUnits(font);
            lift = units > 0 ? TopSideBearing / units : 0f;
            _lifts[font] = lift;
            return lift;
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

        /// <summary>Where <paramref name="bounds" /> are drawn under the transform in force.</summary>
        RectangleF Transformed(RectangleF bounds)
        {
            if (!_transformed)
                return bounds;

            return new RectangleF(
                bounds.X * _transformScale + _transformMove.X,
                bounds.Y * _transformScale + _transformMove.Y,
                bounds.Width * _transformScale,
                bounds.Height * _transformScale);
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