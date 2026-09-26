using VRage.Game;
using VRageMath;
using Sprocket.Host.SpaceEngineers.Ui.Styling;

namespace Sprocket.Host.SpaceEngineers.Ui.Text
{
    /// <summary>What a document looks like in one state, on top of what any text does: how each kind of block and run is set apart.</summary>
    public class RichTextState : TextState
    {
        /// <summary>The color of strong words, or null to leave it to the layers beneath.</summary>
        public Color? StrongColor { get; set; }

        /// <summary>The color of emphasized words, or null to leave it to the layers beneath.</summary>
        public Color? EmphasisColor { get; set; }

        /// <summary>The color of headings, or null to leave it to the layers beneath.</summary>
        public Color? HeadingColor { get; set; }

        /// <summary>The color of a rule, or null to leave it to the layers beneath.</summary>
        public Color? RuleColor { get; set; }

        /// <summary>The font code is set in, or null to leave it to the layers beneath.</summary>
        public string CodeFont { get; set; }

        /// <summary>How tall a first-level heading's line is, or null to leave it to the layers beneath.</summary>
        public float? Heading1Size { get; set; }

        /// <summary>How tall a second-level heading's line is, or null to leave it to the layers beneath.</summary>
        public float? Heading2Size { get; set; }

        /// <summary>How tall the line of a heading at the third level or deeper is, or null to leave it to the layers beneath.</summary>
        public float? Heading3Size { get; set; }

        /// <summary>The room between one block and the next, or null to leave it to the layers beneath.</summary>
        public float? BlockSpacing { get; set; }

        /// <summary>How far a list item's words stand in from its mark, or null to leave it to the layers beneath.</summary>
        public float? Indent { get; set; }

        /// <inheritdoc />
        public override void Neutral(Element element)
        {
            base.Neutral(element);

            var text = element as RichText;
            if (text == null)
                return;

            text.StrongColor = VRageMath.Color.White;
            text.EmphasisColor = VRageMath.Color.White;
            text.HeadingColor = VRageMath.Color.White;
            text.RuleColor = VRageMath.Color.White;
            text.CodeFont = MyFontEnum.White;
            text.Heading1Size = NeutralFontSize;
            text.Heading2Size = NeutralFontSize;
            text.Heading3Size = NeutralFontSize;
            text.BlockSpacing = 0f;
            text.Indent = NeutralFontSize;
        }

        /// <inheritdoc />
        public override void Lay(Element element)
        {
            base.Lay(element);

            var text = element as RichText;
            if (text == null)
                return;

            if (StrongColor.HasValue)
                text.StrongColor = StrongColor.Value;

            if (EmphasisColor.HasValue)
                text.EmphasisColor = EmphasisColor.Value;

            if (HeadingColor.HasValue)
                text.HeadingColor = HeadingColor.Value;

            if (RuleColor.HasValue)
                text.RuleColor = RuleColor.Value;

            if (CodeFont != null)
                text.CodeFont = CodeFont;

            if (Heading1Size.HasValue)
                text.Heading1Size = Heading1Size.Value;

            if (Heading2Size.HasValue)
                text.Heading2Size = Heading2Size.Value;

            if (Heading3Size.HasValue)
                text.Heading3Size = Heading3Size.Value;

            if (BlockSpacing.HasValue)
                text.BlockSpacing = BlockSpacing.Value;

            if (Indent.HasValue)
                text.Indent = Indent.Value;
        }
    }
}
