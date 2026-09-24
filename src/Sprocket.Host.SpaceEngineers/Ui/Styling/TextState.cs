using Sprocket.Host.SpaceEngineers.Ui.Text;
using VRage.Game;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui.Styling
{
    /// <summary>What a run of text looks like in one state, on top of what any element does.</summary>
    public class TextState : ElementState
    {
        /// <summary>How tall a line is when a style leaves it to the neutral values.</summary>
        const float NeutralFontSize = 24f;

        Color? _color;
        string _font;
        float? _fontSize;

        /// <summary>Which face the words are drawn in, or null to leave it to the layers beneath.</summary>
        public string Font
        {
            get { return _font; }
            set
            {
                Check();
                _font = value;
            }
        }

        /// <summary>How tall a line is, in UI units, or null to leave it to the layers beneath.</summary>
        public float? FontSize
        {
            get { return _fontSize; }
            set
            {
                Check();
                _fontSize = value;
            }
        }

        /// <summary>The colour of the words, or null to leave it to the layers beneath.</summary>
        public Color? Color
        {
            get { return _color; }
            set
            {
                Check();
                _color = value;
            }
        }

        /// <inheritdoc />
        internal override void Neutral(Element element)
        {
            base.Neutral(element);

            var label = element as Label;
            if (label == null)
                return;

            label.Font = MyFontEnum.White;
            label.FontSize = NeutralFontSize;
            label.Color = VRageMath.Color.White;
        }

        /// <inheritdoc />
        internal override void Lay(Element element)
        {
            base.Lay(element);

            var label = element as Label;
            if (label == null)
                return;

            if (_font != null)
                label.Font = _font;

            if (_fontSize.HasValue)
                label.FontSize = _fontSize.Value;

            if (_color.HasValue)
                label.Color = _color.Value;
        }

        /// <inheritdoc />
        internal override void Copy(ElementState other)
        {
            base.Copy(other);

            var text = other as TextState;
            if (text == null)
                return;

            _font = text._font;
            _fontSize = text._fontSize;
            _color = text._color;
        }
    }
}
