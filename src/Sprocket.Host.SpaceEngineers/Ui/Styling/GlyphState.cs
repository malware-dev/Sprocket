using Sprocket.Host.SpaceEngineers.Ui.Text;
using VRage.Game;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui.Styling
{
    /// <summary>What a symbol looks like in one state, on top of what any element does.</summary>
    public class GlyphState : ElementState
    {
        string _font;
        float? _fontSize;
        Color? _tint;

        /// <summary>Which font the symbol is drawn from, or null to leave it to the layers beneath.</summary>
        public string Font
        {
            get { return _font; }
            set
            {
                Check();
                _font = value;
            }
        }

        /// <summary>How tall the font's line is, in UI units, or null to leave it to the layers beneath.</summary>
        public float? FontSize
        {
            get { return _fontSize; }
            set
            {
                Check();
                _fontSize = value;
            }
        }

        /// <summary>The color the symbol is drawn in, or null to leave it to the layers beneath.</summary>
        public Color? Tint
        {
            get { return _tint; }
            set
            {
                Check();
                _tint = value;
            }
        }

        /// <inheritdoc />
        internal override void Neutral(Element element)
        {
            base.Neutral(element);

            var glyph = element as Glyph;
            if (glyph == null)
                return;

            glyph.Font = MyFontEnum.White;
            glyph.FontSize = TextState.NeutralFontSize;
            glyph.Tint = Color.White;
        }

        /// <inheritdoc />
        internal override void Lay(Element element)
        {
            base.Lay(element);

            var glyph = element as Glyph;
            if (glyph == null)
                return;

            if (_font != null)
                glyph.Font = _font;

            if (_fontSize.HasValue)
                glyph.FontSize = _fontSize.Value;

            if (_tint.HasValue)
                glyph.Tint = _tint.Value;
        }

        /// <inheritdoc />
        internal override void Copy(ElementState other)
        {
            base.Copy(other);

            var glyph = other as GlyphState;
            if (glyph == null)
                return;

            _font = glyph._font;
            _fontSize = glyph._fontSize;
            _tint = glyph._tint;
        }
    }
}
