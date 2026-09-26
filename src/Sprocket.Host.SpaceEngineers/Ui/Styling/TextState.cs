using VRage.Game;

namespace Sprocket.Host.SpaceEngineers.Ui.Styling
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

            text.Font = MyFontEnum.White;
            text.FontSize = NeutralFontSize;
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
    }
}
