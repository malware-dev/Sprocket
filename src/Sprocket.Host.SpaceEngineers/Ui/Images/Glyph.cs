using System.Text;
using Sprocket.Host.SpaceEngineers.Ui.Drawing;
using VRage.Game;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui.Images
{
    /// <summary>One character of a font, drawn as a symbol and centered in whatever room it is given.</summary>
    public class Glyph : Element, IPadded
    {
        readonly StringBuilder _measuring = new StringBuilder(2);

        string _font = MyFontEnum.White;
        float _fontSize = 24f;
        Thickness _padding;
        Vector2 _size;

        float _sizedAt = -1f;

        string _sizedFor;

        string _symbol;

        /// <summary>A glyph with no symbol, in white, 24 units tall.</summary>
        public Glyph()
        {
            Tint = Color.White;
        }

        /// <summary>Which character to draw.</summary>
        public string Symbol
        {
            get { return _symbol; }
            set
            {
                if (_symbol == value)
                    return;

                _symbol = value;
                _measuring.Length = 0;
                InvalidateMeasure();

                if (value != null)
                    _measuring.Append(value);
            }
        }

        /// <summary>Which font the symbol is drawn from.</summary>
        public string Font
        {
            get { return _font; }
            set
            {
                if (_font == value)
                    return;

                _font = value;
                _sizedAt = -1f;
                InvalidateMeasure();
            }
        }

        /// <summary>How tall the font's line is, in UI units.</summary>
        public float FontSize
        {
            get { return _fontSize; }
            set
            {
                if (_fontSize == value)
                    return;

                _fontSize = value;
                InvalidateMeasure();
            }
        }

        /// <summary>Room held clear between the edge and the symbol, which is centered within.</summary>
        public Thickness Padding
        {
            get { return _padding; }
            set
            {
                if (_padding == value)
                    return;

                _padding = value;
                InvalidateMeasure();
            }
        }

        /// <inheritdoc />
        protected override Thickness Inset => _padding;

        /// <summary>What color to draw it, where transparent draws nothing but keeps the room.</summary>
        public Color Tint { get; set; }

        /// <summary>How much room the symbol wants, measured once per symbol, font and size.</summary>
        /// <remarks>Held, because a measure re-runs whenever anything above it moves and measuring text is the dearest call the layout makes.</remarks>
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            if (_sizedFor == _symbol && _sizedAt == _fontSize)
                return _size;

            _sizedFor = _symbol;
            _sizedAt = _fontSize;

            _size = _measuring.Length == 0
                ? Vector2.Zero
                : g.MeasureText(_font, _measuring, _fontSize);

            return _size;
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            if (Tint.A == 0 || string.IsNullOrEmpty(_symbol))
                return;

            var room = ContentBounds;
            g.DrawText(_font, _symbol, new Vector2(
                room.X + (room.Width - _size.X) * 0.5f,
                room.Y + (room.Height - _size.Y) * 0.5f), _fontSize, Tint);
        }
    }
}
