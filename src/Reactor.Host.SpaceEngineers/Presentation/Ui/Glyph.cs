using System.Text;
using VRageMath;
using Reactor.Host.SpaceEngineers.Presentation.Graphics;

namespace Reactor.Host.SpaceEngineers.Presentation.Ui
{
    /// <summary>
    /// One symbol, centred in whatever room it is given.
    /// </summary>
    /// <remarks>
    /// A character drawn in <see cref="Symbols.Font" /> rather than a picture, so it costs the same call
    /// as a word and is measured the same way. Centred because a row is as tall as its tallest thing, and
    /// a symbol next to two lines of text would otherwise hang off the first.
    /// </remarks>
    public class Glyph : Element
    {
        private float _scale;

        private readonly StringBuilder _measuring = new StringBuilder(2);

        private string _symbol;
        private Vector2 _size;

        public Glyph(string symbol, float scale, Color tint)
        {
            Symbol = symbol;
            Scale = scale;
            Tint = tint;
        }

        /// <summary>Which symbol. One of the constants on <see cref="Symbols" />.</summary>
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

        /// <summary>How big, as a font scale.</summary>
        /// <remarks>
        /// A scale rather than a size in units, so the same number next to a label's scale means the same
        /// height whatever either target's units happen to be.
        /// </remarks>
        public float Scale
        {
            get { return _scale; }
            set
            {
                if (_scale == value)
                    return;

                _scale = value;
                InvalidateMeasure();
            }
        }

        /// <summary>What colour to draw it. Transparent draws nothing and keeps the room.</summary>
        /// <remarks>
        /// Keeping the room is the point: a symbol that turns off and on would otherwise drag everything
        /// beside it sideways. Use <see cref="Element.Collapsed" /> for one that should give its room back.
        /// </remarks>
        public Color Tint { get; set; }

        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            _size = _measuring.Length == 0
                ? Vector2.Zero
                : g.MeasureText(Symbols.Font, _measuring, Scale);

            return _size;
        }

        protected override void DrawContent(IGraphics g)
        {
            if (Tint.A == 0 || string.IsNullOrEmpty(_symbol))
                return;

            RectangleF room = ContentBounds;
            g.DrawText(Symbols.Font, _symbol, new Vector2(
                room.X + (room.Width - _size.X) * 0.5f,
                room.Y + (room.Height - _size.Y) * 0.5f), Scale, Tint);
        }
    }
}
