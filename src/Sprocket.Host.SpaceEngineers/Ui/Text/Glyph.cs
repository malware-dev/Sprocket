using System.Text;
using Sprocket.Host.SpaceEngineers.Ui.Drawing;
using Sprocket.Host.SpaceEngineers.Look;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui.Text
{
    /// <summary>One symbol, centred in whatever room it is given.</summary>
    public class Glyph : Element
    {
        readonly StringBuilder _measuring = new StringBuilder(2);

        float _lift;
        float _fontSize;
        Vector2 _size;

        float _sizedAt = -1f;

        /// <summary>What the held size was measured from, so it is only measured again when it moves.</summary>
        string _sizedFor;

        string _symbol;

        /// <summary>A glyph drawing <paramref name="symbol" /> at <paramref name="fontSize" />, tinted <paramref name="tint" />.</summary>
        public Glyph(string symbol, float fontSize, Color tint)
        {
            Symbol = symbol;
            FontSize = fontSize;
            Tint = tint;
        }

        /// <summary>Which symbol, as one of the constants on <see cref="Symbols" />.</summary>
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

        /// <summary>How tall the symbol font's line is, in UI units.</summary>
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

        /// <summary>What colour to draw it, where transparent draws nothing but keeps the room.</summary>
        public Color Tint { get; set; }

        /// <summary>How far to lift the mark off the middle of its own room, as a fraction of its height.</summary>
        public float Lift
        {
            get { return _lift; }
            set
            {
                if (_lift == value)
                    return;

                _lift = value;
                InvalidateMeasure();
            }
        }

        /// <summary>How much room the symbol wants, measured once per symbol and size.</summary>
        /// <remarks>
        ///     Held, because a measure re-runs whenever anything above it moves and measuring text is the
        ///     dearest call the layout makes.
        /// </remarks>
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            if (_sizedFor == _symbol && _sizedAt == FontSize)
                return _size;

            _sizedFor = _symbol;
            _sizedAt = FontSize;

            _size = _measuring.Length == 0
                ? Vector2.Zero
                : g.MeasureText(Symbols.Font, _measuring, FontSize);

            // Set while measuring because the lift is a fraction of a height only measuring knows.
            Offset = new Vector2(Offset.X, -_size.Y * _lift);

            return _size;
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            if (Tint.A == 0 || string.IsNullOrEmpty(_symbol))
                return;

            var room = ContentBounds;
            g.DrawText(Symbols.Font, _symbol, new Vector2(
                room.X + (room.Width - _size.X) * 0.5f,
                room.Y + (room.Height - _size.Y) * 0.5f), FontSize, Tint);
        }
    }
}