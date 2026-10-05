using VRage.Game;
using VRageMath;
using Lumel.Styling;

namespace Lumel.Text
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

        /// <summary>Where the lines sit across the room, or null to leave it to the layers beneath.</summary>
        public Alignment? Alignment { get; set; }

        /// <summary>The color of a link's words, or null to leave it to the layers beneath.</summary>
        public Color? LinkColor { get; set; }

        /// <summary>The fill behind a link's words, or null to leave it to the layers beneath.</summary>
        public Color? LinkFill { get; set; }

        /// <summary>The looks tagged quotes take, or null to leave them to the layers beneath.</summary>
        public QuoteStyles Quotes { get; set; }

        /// <inheritdoc />
        public override void Neutral(Element element)
        {
            base.Neutral(element);

            var text = element as RichText;
            if (text == null)
                return;

            if (StrongColor.HasValue)
                text.StrongColor = VRageMath.Color.White;

            if (EmphasisColor.HasValue)
                text.EmphasisColor = VRageMath.Color.White;

            if (HeadingColor.HasValue)
                text.HeadingColor = VRageMath.Color.White;

            if (RuleColor.HasValue)
                text.RuleColor = VRageMath.Color.White;

            if (CodeFont != null)
                text.CodeFont = MyFontEnum.White;

            if (Heading1Size.HasValue)
                text.Heading1Size = NeutralFontSize;

            if (Heading2Size.HasValue)
                text.Heading2Size = NeutralFontSize;

            if (Heading3Size.HasValue)
                text.Heading3Size = NeutralFontSize;

            if (BlockSpacing.HasValue)
                text.BlockSpacing = 0f;

            if (Indent.HasValue)
                text.Indent = NeutralFontSize;

            if (Alignment.HasValue)
                text.Alignment = Lumel.Text.Alignment.Left;

            if (LinkColor.HasValue)
                text.LinkColor = null;

            if (LinkFill.HasValue)
                text.LinkFill = null;

            if (Quotes != null)
                text.Quotes = null;
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

            if (Alignment.HasValue)
                text.Alignment = Alignment.Value;

            if (LinkColor.HasValue)
                text.LinkColor = LinkColor.Value;

            if (LinkFill.HasValue)
                text.LinkFill = LinkFill.Value;

            if (Quotes != null)
                text.Quotes = Quotes;
        }

        /// <inheritdoc />
        public override void Clear()
        {
            base.Clear();
            StrongColor = null;
            EmphasisColor = null;
            HeadingColor = null;
            RuleColor = null;
            CodeFont = null;
            Heading1Size = null;
            Heading2Size = null;
            Heading3Size = null;
            BlockSpacing = null;
            Indent = null;
            Alignment = null;
            LinkColor = null;
            LinkFill = null;
            Quotes = null;
        }

        /// <inheritdoc />
        public override void Gather(ElementState layer)
        {
            base.Gather(layer);
            var other = layer as RichTextState;
            if (other == null)
                return;

            if (!StrongColor.HasValue)
                StrongColor = other.StrongColor;

            if (!EmphasisColor.HasValue)
                EmphasisColor = other.EmphasisColor;

            if (!HeadingColor.HasValue)
                HeadingColor = other.HeadingColor;

            if (!RuleColor.HasValue)
                RuleColor = other.RuleColor;

            if (CodeFont == null)
                CodeFont = other.CodeFont;

            if (!Heading1Size.HasValue)
                Heading1Size = other.Heading1Size;

            if (!Heading2Size.HasValue)
                Heading2Size = other.Heading2Size;

            if (!Heading3Size.HasValue)
                Heading3Size = other.Heading3Size;

            if (!BlockSpacing.HasValue)
                BlockSpacing = other.BlockSpacing;

            if (!Indent.HasValue)
                Indent = other.Indent;

            if (!Alignment.HasValue)
                Alignment = other.Alignment;

            if (!LinkColor.HasValue)
                LinkColor = other.LinkColor;

            if (!LinkFill.HasValue)
                LinkFill = other.LinkFill;

            if (Quotes == null)
                Quotes = other.Quotes;
        }
    }
}
