using System.Text;
using Lumel.Drawing;
using VRage.Game;
using VRageMath;

namespace Lumel.Images
{
    /// <summary>One character of a font, drawn as a symbol and centered in whatever room it is given.</summary>
    public class Glyph : Element, IPadded
    {
        readonly StringBuilder _measuring = new StringBuilder(2);
        readonly StringBuilder _haloMeasuring = new StringBuilder(2);

        string _font = MyFontEnum.White;
        float _fontSize = 24f;
        Thickness _padding;
        Vector2 _size;

        float _sizedAt = -1f;

        string _sizedFor;

        string _symbol;

        string _halo;
        float _haloSize;
        Vector2 _haloSized;
        string _haloSizedFor;
        float _haloSizedAt = -1f;

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

        /// <summary>A character of the same font drawn around the symbol, centered on it and taking no room of its own, or null for none.</summary>
        /// <remarks>Measured with the symbol, so it is given once and shown or hidden by <see cref="HaloTint" /> alone.</remarks>
        public string Halo
        {
            get { return _halo; }
            set
            {
                if (_halo == value)
                    return;

                _halo = value;
                _haloMeasuring.Length = 0;
                if (value != null)
                    _haloMeasuring.Append(value);

                InvalidateMeasure();
            }
        }

        /// <summary>How tall the halo's line is, in UI units.</summary>
        public float HaloSize
        {
            get { return _haloSize; }
            set
            {
                if (_haloSize == value)
                    return;

                _haloSize = value;
                InvalidateMeasure();
            }
        }

        /// <summary>What color to draw the halo, where transparent draws none.</summary>
        public Color HaloTint { get; set; }

        /// <summary>How much room the symbol wants, measured once per symbol, font and size.</summary>
        /// <remarks>Held, because a measure re-runs whenever anything above it moves and measuring text is the dearest call the layout makes.</remarks>
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            if (_haloSizedFor != _halo || _haloSizedAt != _haloSize)
            {
                _haloSizedFor = _halo;
                _haloSizedAt = _haloSize;
                _haloSized = _haloMeasuring.Length == 0 ? Vector2.Zero : g.MeasureText(_font, _haloMeasuring, _haloSize);
            }

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
            var room = ContentBounds;
            if (HaloTint.A != 0 && !string.IsNullOrEmpty(_halo))
            {
                g.DrawText(_font, _halo, new Vector2(
                    room.X + (room.Width - _haloSized.X) * 0.5f,
                    room.Y + (room.Height - _haloSized.Y) * 0.5f), _haloSize, HaloTint);
            }

            if (Tint.A == 0 || string.IsNullOrEmpty(_symbol))
                return;

            g.DrawText(_font, _symbol, new Vector2(
                room.X + (room.Width - _size.X) * 0.5f,
                room.Y + (room.Height - _size.Y) * 0.5f), _fontSize, Tint);
        }
    }
}
