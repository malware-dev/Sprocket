using System.Text;
using Sprocket.Host.SpaceEngineers.Ui.Drawing;
using Sprocket.Host.SpaceEngineers.Ui.Look;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui.Text
{
    /// <summary>
    ///     One symbol, centred in whatever room it is given.
    /// </summary>
    /// <remarks>
    ///     A character drawn in <see cref="Symbols.Font" /> rather than a picture, so it costs the same call
    ///     as a word and is measured the same way. Centred because a row is as tall as its tallest thing, and
    ///     a symbol next to two lines of text would otherwise hang off the first.
    /// </remarks>
    public class Glyph : Element
    {
        readonly StringBuilder _measuring = new StringBuilder(2);

        float _lift;
        float _scale;
        Vector2 _size;

        float _sizedAt = -1f;

        /// <summary>What the held size was measured from, so it is only measured again when it moves.</summary>
        string _sizedFor;

        string _symbol;

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
        ///     A scale rather than a size in units, so the same number next to a label's scale means the same
        ///     height whatever either target's units happen to be.
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
        ///     Keeping the room is the point: a symbol that turns off and on would otherwise drag everything
        ///     beside it sideways. Use <see cref="Element.Collapsed" /> for one that should give its room back.
        /// </remarks>
        public Color Tint { get; set; }

        /// <summary>
        ///     How far to lift the mark off the middle of its own room, as a fraction of its height.
        /// </summary>
        /// <remarks>
        ///     A fraction rather than a number of units, so it holds at whatever size the symbol is drawn.
        ///     Zero centres it, which is what the font promises and what anything standing on its own
        ///     wants. See <c>Widgets.Symbol</c> for the one case that does not.
        ///     <para>
        ///         <b>It moves the whole glyph, not the ink inside it.</b> Nudging only what is drawn would
        ///         leave the mark and the thing a person presses in different places, and a symbol is very
        ///         often a button.
        ///     </para>
        /// </remarks>
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

        /// <summary>
        ///     How much room the symbol wants, measured once per symbol and scale.
        /// </summary>
        /// <remarks>
        ///     Held rather than asked for every time. A measure is invalidated by anything above it moving -
        ///     a banner sliding, a row growing - and the answer for a character that has not changed is the
        ///     answer it gave before, at the dearest call the layout makes.
        /// </remarks>
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            if (_sizedFor == _symbol && _sizedAt == Scale)
                return _size;

            _sizedFor = _symbol;
            _sizedAt = Scale;

            _size = _measuring.Length == 0
                ? Vector2.Zero
                : g.MeasureText(Symbols.Font, _measuring, Scale);

            // Here because the lift is a fraction of a height only measuring knows, and because an
            // offset is what moves an element rather than only what it draws.
            Offset = new Vector2(Offset.X, -_size.Y * _lift);

            return _size;
        }

        protected override void DrawContent(IGraphics g)
        {
            if (Tint.A == 0 || string.IsNullOrEmpty(_symbol))
                return;

            var room = ContentBounds;
            g.DrawText(Symbols.Font, _symbol, new Vector2(
                room.X + (room.Width - _size.X) * 0.5f,
                room.Y + (room.Height - _size.Y) * 0.5f), Scale, Tint);
        }
    }
}