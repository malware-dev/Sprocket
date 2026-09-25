using Sprocket.Host.SpaceEngineers.Ui.Text;
using VRage.Game;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui.Styling
{
    /// <summary>What a text box looks like in one state: its words, and the colors of its selection, caret and hint.</summary>
    public class TextBoxState : TextState
    {
        Color? _caret;
        Color? _hint;
        Color? _selection;

        /// <summary>What is painted behind the selected run, or null to leave it to the layers beneath.</summary>
        public Color? SelectionColor
        {
            get { return _selection; }
            set
            {
                Check();
                _selection = value;
            }
        }

        /// <summary>The color of the caret, or null to leave it to the layers beneath.</summary>
        public Color? CaretColor
        {
            get { return _caret; }
            set
            {
                Check();
                _caret = value;
            }
        }

        /// <summary>The color the hint is drawn in while the box is empty, or null to leave it to the layers beneath.</summary>
        public Color? HintColor
        {
            get { return _hint; }
            set
            {
                Check();
                _hint = value;
            }
        }

        /// <inheritdoc />
        internal override void Neutral(Element element)
        {
            base.Neutral(element);

            var box = element as TextBox;
            if (box == null)
                return;

            box.Font = MyFontEnum.White;
            box.FontSize = NeutralFontSize;
            box.Color = VRageMath.Color.White;
            box.SelectionColor = new Color(40, 90, 150);
            box.CaretColor = VRageMath.Color.White;
            box.HintColor = VRageMath.Color.Gray;
        }

        /// <inheritdoc />
        internal override void Lay(Element element)
        {
            base.Lay(element);

            var box = element as TextBox;
            if (box == null)
                return;

            if (Font != null)
                box.Font = Font;

            if (FontSize.HasValue)
                box.FontSize = FontSize.Value;

            if (Color.HasValue)
                box.Color = Color.Value;

            if (_selection.HasValue)
                box.SelectionColor = _selection.Value;

            if (_caret.HasValue)
                box.CaretColor = _caret.Value;

            if (_hint.HasValue)
                box.HintColor = _hint.Value;
        }

        /// <inheritdoc />
        internal override void Copy(ElementState other)
        {
            base.Copy(other);

            var state = other as TextBoxState;
            if (state == null)
                return;

            _selection = state._selection;
            _caret = state._caret;
            _hint = state._hint;
        }
    }
}
