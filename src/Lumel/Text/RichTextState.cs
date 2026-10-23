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

        /// <summary>The font strong words are set in, or null to leave it to the layers beneath.</summary>
        public string StrongFont { get; set; }

        /// <summary>The font emphasized words are set in, or null to leave it to the layers beneath.</summary>
        public string EmphasisFont { get; set; }

        /// <summary>The font words both strong and emphasized are set in, or null to leave it to the layers beneath.</summary>
        public string StrongEmphasisFont { get; set; }

        /// <summary>The font headings and a table's heading row are set in, or null to leave it to the layers beneath.</summary>
        public string HeadingFont { get; set; }

        /// <summary>How tall a first-level heading's line is, or null to leave it to the layers beneath.</summary>
        public float? Heading1Size { get; set; }

        /// <summary>How tall a second-level heading's line is, or null to leave it to the layers beneath.</summary>
        public float? Heading2Size { get; set; }

        /// <summary>How tall the line of a heading at the third level or deeper is, or null to leave it to the layers beneath.</summary>
        public float? Heading3Size { get; set; }

        /// <summary>The room between one block and the next, or null to leave it to the layers beneath.</summary>
        public float? BlockSpacing { get; set; }

        /// <summary>The deepest level of heading drawn with a rule under it, or null to leave it to the layers beneath.</summary>
        public int? RuledHeadings { get; set; }

        /// <summary>How far a list item's words stand in from its mark, or null to leave it to the layers beneath.</summary>
        public float? Indent { get; set; }

        /// <summary>Where the lines sit across the room, or null to leave it to the layers beneath.</summary>
        public Alignment? Alignment { get; set; }

        /// <summary>The color of a link's words, or null to leave it to the layers beneath.</summary>
        public Color? LinkColor { get; set; }

        /// <summary>The fill behind a link's words, or null to leave it to the layers beneath.</summary>
        public Color? LinkFill { get; set; }

        /// <summary>The box behind code, or null to leave it to the layers beneath.</summary>
        public Color? CodeFill { get; set; }

        /// <summary>How far the box behind code reaches past its words, or null to leave it to the layers beneath.</summary>
        public float? CodeInset { get; set; }

        /// <summary>How thick the line under a link's words is, or null to leave it to the layers beneath.</summary>
        public float? LinkUnderline { get; set; }

        /// <summary>How far down a line a link's underline lies, as a fraction of its height, or null to leave it to the layers beneath.</summary>
        public float? UnderlineAt { get; set; }

        /// <summary>What the attributes a box names ask for, or null to leave them to the layers beneath.</summary>
        public BoxStyles Boxes { get; set; }

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

            if (StrongFont != null)
                text.StrongFont = null;

            if (EmphasisFont != null)
                text.EmphasisFont = null;

            if (StrongEmphasisFont != null)
                text.StrongEmphasisFont = null;

            if (HeadingFont != null)
                text.HeadingFont = null;

            if (Heading1Size.HasValue)
                text.Heading1Size = NeutralFontSize;

            if (Heading2Size.HasValue)
                text.Heading2Size = NeutralFontSize;

            if (Heading3Size.HasValue)
                text.Heading3Size = NeutralFontSize;

            if (BlockSpacing.HasValue)
                text.BlockSpacing = 0f;

            if (RuledHeadings.HasValue)
                text.RuledHeadings = 0;

            if (Indent.HasValue)
                text.Indent = NeutralFontSize;

            if (Alignment.HasValue)
                text.Alignment = Lumel.Text.Alignment.Left;

            if (LinkColor.HasValue)
                text.LinkColor = null;

            if (LinkFill.HasValue)
                text.LinkFill = null;

            if (CodeFill.HasValue)
                text.CodeFill = RichText.CodeWash;

            if (CodeInset.HasValue)
                text.CodeInset = RichText.CodeRoom;

            if (LinkUnderline.HasValue)
                text.LinkUnderline = RichText.UnderlineThickness;

            if (UnderlineAt.HasValue)
                text.UnderlineAt = RichText.UnderlineDrop;

            if (Boxes != null)
                text.Boxes = null;
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

            if (StrongFont != null)
                text.StrongFont = StrongFont;

            if (EmphasisFont != null)
                text.EmphasisFont = EmphasisFont;

            if (StrongEmphasisFont != null)
                text.StrongEmphasisFont = StrongEmphasisFont;

            if (HeadingFont != null)
                text.HeadingFont = HeadingFont;

            if (Heading1Size.HasValue)
                text.Heading1Size = Heading1Size.Value;

            if (Heading2Size.HasValue)
                text.Heading2Size = Heading2Size.Value;

            if (Heading3Size.HasValue)
                text.Heading3Size = Heading3Size.Value;

            if (BlockSpacing.HasValue)
                text.BlockSpacing = BlockSpacing.Value;

            if (RuledHeadings.HasValue)
                text.RuledHeadings = RuledHeadings.Value;

            if (Indent.HasValue)
                text.Indent = Indent.Value;

            if (Alignment.HasValue)
                text.Alignment = Alignment.Value;

            if (LinkColor.HasValue)
                text.LinkColor = LinkColor.Value;

            if (LinkFill.HasValue)
                text.LinkFill = LinkFill.Value;

            if (CodeFill.HasValue)
                text.CodeFill = CodeFill.Value;

            if (CodeInset.HasValue)
                text.CodeInset = CodeInset.Value;

            if (LinkUnderline.HasValue)
                text.LinkUnderline = LinkUnderline.Value;

            if (UnderlineAt.HasValue)
                text.UnderlineAt = UnderlineAt.Value;

            if (Boxes != null)
                text.Boxes = Boxes;
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
            StrongFont = null;
            EmphasisFont = null;
            StrongEmphasisFont = null;
            HeadingFont = null;
            Heading1Size = null;
            Heading2Size = null;
            Heading3Size = null;
            BlockSpacing = null;
            RuledHeadings = null;
            Indent = null;
            Alignment = null;
            LinkColor = null;
            LinkFill = null;
            CodeFill = null;
            CodeInset = null;
            LinkUnderline = null;
            UnderlineAt = null;
            Boxes = null;
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

            if (StrongFont == null)
                StrongFont = other.StrongFont;

            if (EmphasisFont == null)
                EmphasisFont = other.EmphasisFont;

            if (StrongEmphasisFont == null)
                StrongEmphasisFont = other.StrongEmphasisFont;

            if (HeadingFont == null)
                HeadingFont = other.HeadingFont;

            if (!Heading1Size.HasValue)
                Heading1Size = other.Heading1Size;

            if (!Heading2Size.HasValue)
                Heading2Size = other.Heading2Size;

            if (!Heading3Size.HasValue)
                Heading3Size = other.Heading3Size;

            if (!BlockSpacing.HasValue)
                BlockSpacing = other.BlockSpacing;

            if (!RuledHeadings.HasValue)
                RuledHeadings = other.RuledHeadings;

            if (!Indent.HasValue)
                Indent = other.Indent;

            if (!Alignment.HasValue)
                Alignment = other.Alignment;

            if (!LinkColor.HasValue)
                LinkColor = other.LinkColor;

            if (!LinkFill.HasValue)
                LinkFill = other.LinkFill;

            if (!CodeFill.HasValue)
                CodeFill = other.CodeFill;

            if (!CodeInset.HasValue)
                CodeInset = other.CodeInset;

            if (!LinkUnderline.HasValue)
                LinkUnderline = other.LinkUnderline;

            if (!UnderlineAt.HasValue)
                UnderlineAt = other.UnderlineAt;

            if (Boxes == null)
                Boxes = other.Boxes;
        }
    }
}
