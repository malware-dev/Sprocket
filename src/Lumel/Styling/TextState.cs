using VRage.Game;

namespace Lumel.Styling
{
    /// <summary>What an element that draws words looks like in one state, on top of what any element does.</summary>
    public class TextState : ElementState
    {
        /// <summary>How tall a line is when a style leaves it to the neutral values.</summary>
        public const float NeutralFontSize = 24f;

        /// <summary>Which face the words are drawn in, or null to leave it to the layers beneath.</summary>
        public string Font { get; set; }

        /// <summary>How tall a line is, in UI units, or null to leave it to the layers beneath.</summary>
        public float? FontSize { get; set; }

        /// <summary>The color of the words, or null to leave it to the layers beneath.</summary>
        public VRageMath.Color? Color { get; set; }

        /// <inheritdoc />
        public override void Neutral(Element element)
        {
            base.Neutral(element);

            var text = element as ITextual;
            if (text == null)
                return;

            if (Font != null)
                text.Font = MyFontEnum.White;

            if (FontSize.HasValue)
                text.FontSize = NeutralFontSize;

            if (Color.HasValue)
                text.Color = VRageMath.Color.White;
        }

        /// <inheritdoc />
        public override void Lay(Element element)
        {
            base.Lay(element);

            var text = element as ITextual;
            if (text == null)
                return;

            if (Font != null)
                text.Font = Font;

            if (FontSize.HasValue)
                text.FontSize = FontSize.Value;

            if (Color.HasValue)
                text.Color = Color.Value;
        }

        /// <inheritdoc />
        public override void Clear()
        {
            base.Clear();
            Font = null;
            FontSize = null;
            Color = null;
        }

        /// <inheritdoc />
        public override void Gather(ElementState layer)
        {
            base.Gather(layer);
            var other = layer as TextState;
            if (other == null)
                return;

            if (Font == null)
                Font = other.Font;

            if (!FontSize.HasValue)
                FontSize = other.FontSize;

            if (!Color.HasValue)
                Color = other.Color;
        }
    }
}
