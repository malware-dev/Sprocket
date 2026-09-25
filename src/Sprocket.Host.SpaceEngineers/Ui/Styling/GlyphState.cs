using VRageMath;
using Sprocket.Host.SpaceEngineers.Ui.Legacy;

namespace Sprocket.Host.SpaceEngineers.Ui.Styling
{
    /// <summary>What a symbol looks like in one state, on top of what any element does.</summary>
    public class GlyphState : ElementState
    {
        /// <summary>How tall the symbol font's line is when a style leaves it to the neutral values.</summary>
        const float NeutralFontSize = 24f;

        float? _fontSize;
        Color? _tint;

        /// <summary>How tall the symbol font's line is, in UI units, or null to leave it to the layers beneath.</summary>
        public float? FontSize
        {
            get { return _fontSize; }
            set
            {
                Check();
                _fontSize = value;
            }
        }

        /// <summary>The colour the symbol is drawn in, or null to leave it to the layers beneath.</summary>
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

            glyph.FontSize = NeutralFontSize;
            glyph.Tint = Color.White;
        }

        /// <inheritdoc />
        internal override void Lay(Element element)
        {
            base.Lay(element);

            var glyph = element as Glyph;
            if (glyph == null)
                return;

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

            _fontSize = glyph._fontSize;
            _tint = glyph._tint;
        }
    }
}
