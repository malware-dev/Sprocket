using VRage.Game;
using VRageMath;
using Lumel.Styling;

namespace Lumel.Images
{
    /// <summary>What a symbol looks like in one state, on top of what any element does.</summary>
    public class GlyphState : ElementState
    {
        /// <summary>Which font the symbol is drawn from, or null to leave it to the layers beneath.</summary>
        public string Font { get; set; }

        /// <summary>How tall the font's line is, in UI units, or null to leave it to the layers beneath.</summary>
        public float? FontSize { get; set; }

        /// <summary>The color the symbol is drawn in, or null to leave it to the layers beneath.</summary>
        public Color? Tint { get; set; }

        /// <inheritdoc />
        public override void Neutral(Element element)
        {
            base.Neutral(element);

            var glyph = element as Glyph;
            if (glyph == null)
                return;

            if (Font != null)
                glyph.Font = MyFontEnum.White;

            if (FontSize.HasValue)
                glyph.FontSize = TextState.NeutralFontSize;

            if (Tint.HasValue)
                glyph.Tint = Color.White;
        }

        /// <inheritdoc />
        public override void Lay(Element element)
        {
            base.Lay(element);

            var glyph = element as Glyph;
            if (glyph == null)
                return;

            if (Font != null)
                glyph.Font = Font;

            if (FontSize.HasValue)
                glyph.FontSize = FontSize.Value;

            if (Tint.HasValue)
                glyph.Tint = Tint.Value;
        }

        /// <inheritdoc />
        public override void Clear()
        {
            base.Clear();
            Font = null;
            FontSize = null;
            Tint = null;
        }

        /// <inheritdoc />
        public override void Gather(ElementState layer)
        {
            base.Gather(layer);
            var other = layer as GlyphState;
            if (other == null)
                return;

            if (Font == null)
                Font = other.Font;

            if (!FontSize.HasValue)
                FontSize = other.FontSize;

            if (!Tint.HasValue)
                Tint = other.Tint;
        }
    }
}
