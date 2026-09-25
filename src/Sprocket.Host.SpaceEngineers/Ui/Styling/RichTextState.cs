using Sprocket.Host.SpaceEngineers.Ui.Rich;
using VRage.Game;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui.Styling
{
    /// <summary>What a document looks like in one state, on top of what any text does: how each kind of block and run is set apart.</summary>
    public class RichTextState : TextState
    {
        Color? _strong;
        Color? _emphasis;
        Color? _heading;
        Color? _rule;
        string _codeFont;
        float? _heading1;
        float? _heading2;
        float? _heading3;
        float? _spacing;
        float? _indent;

        /// <summary>The color of strong words, or null to leave it to the layers beneath.</summary>
        public Color? StrongColor
        {
            get { return _strong; }
            set
            {
                Check();
                _strong = value;
            }
        }

        /// <summary>The color of emphasized words, or null to leave it to the layers beneath.</summary>
        public Color? EmphasisColor
        {
            get { return _emphasis; }
            set
            {
                Check();
                _emphasis = value;
            }
        }

        /// <summary>The color of headings, or null to leave it to the layers beneath.</summary>
        public Color? HeadingColor
        {
            get { return _heading; }
            set
            {
                Check();
                _heading = value;
            }
        }

        /// <summary>The color of a rule, or null to leave it to the layers beneath.</summary>
        public Color? RuleColor
        {
            get { return _rule; }
            set
            {
                Check();
                _rule = value;
            }
        }

        /// <summary>The font code is set in, or null to leave it to the layers beneath.</summary>
        public string CodeFont
        {
            get { return _codeFont; }
            set
            {
                Check();
                _codeFont = value;
            }
        }

        /// <summary>How tall a first-level heading's line is, or null to leave it to the layers beneath.</summary>
        public float? Heading1Size
        {
            get { return _heading1; }
            set
            {
                Check();
                _heading1 = value;
            }
        }

        /// <summary>How tall a second-level heading's line is, or null to leave it to the layers beneath.</summary>
        public float? Heading2Size
        {
            get { return _heading2; }
            set
            {
                Check();
                _heading2 = value;
            }
        }

        /// <summary>How tall the line of a heading at the third level or deeper is, or null to leave it to the layers beneath.</summary>
        public float? Heading3Size
        {
            get { return _heading3; }
            set
            {
                Check();
                _heading3 = value;
            }
        }

        /// <summary>The room between one block and the next, or null to leave it to the layers beneath.</summary>
        public float? BlockSpacing
        {
            get { return _spacing; }
            set
            {
                Check();
                _spacing = value;
            }
        }

        /// <summary>How far a list item's words stand in from its mark, or null to leave it to the layers beneath.</summary>
        public float? Indent
        {
            get { return _indent; }
            set
            {
                Check();
                _indent = value;
            }
        }

        /// <inheritdoc />
        internal override void Neutral(Element element)
        {
            base.Neutral(element);

            var text = element as RichText;
            if (text == null)
                return;

            text.Font = MyFontEnum.White;
            text.FontSize = NeutralFontSize;
            text.Color = VRageMath.Color.White;
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
        internal override void Lay(Element element)
        {
            base.Lay(element);

            var text = element as RichText;
            if (text == null)
                return;

            if (Font != null)
                text.Font = Font;

            if (FontSize.HasValue)
                text.FontSize = FontSize.Value;

            if (Color.HasValue)
                text.Color = Color.Value;

            if (_strong.HasValue)
                text.StrongColor = _strong.Value;

            if (_emphasis.HasValue)
                text.EmphasisColor = _emphasis.Value;

            if (_heading.HasValue)
                text.HeadingColor = _heading.Value;

            if (_rule.HasValue)
                text.RuleColor = _rule.Value;

            if (_codeFont != null)
                text.CodeFont = _codeFont;

            if (_heading1.HasValue)
                text.Heading1Size = _heading1.Value;

            if (_heading2.HasValue)
                text.Heading2Size = _heading2.Value;

            if (_heading3.HasValue)
                text.Heading3Size = _heading3.Value;

            if (_spacing.HasValue)
                text.BlockSpacing = _spacing.Value;

            if (_indent.HasValue)
                text.Indent = _indent.Value;
        }

        /// <inheritdoc />
        internal override void Copy(ElementState other)
        {
            base.Copy(other);

            var state = other as RichTextState;
            if (state == null)
                return;

            _strong = state._strong;
            _emphasis = state._emphasis;
            _heading = state._heading;
            _rule = state._rule;
            _codeFont = state._codeFont;
            _heading1 = state._heading1;
            _heading2 = state._heading2;
            _heading3 = state._heading3;
            _spacing = state._spacing;
            _indent = state._indent;
        }
    }
}
