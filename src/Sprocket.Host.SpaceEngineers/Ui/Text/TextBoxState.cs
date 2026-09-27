using VRageMath;
using Sprocket.Host.SpaceEngineers.Ui.Styling;

namespace Sprocket.Host.SpaceEngineers.Ui.Text
{
    /// <summary>What a text box looks like in one state: its words, and the colors of its selection, caret and hint.</summary>
    public class TextBoxState : TextState
    {
        /// <summary>What is painted behind the selected run, or null to leave it to the layers beneath.</summary>
        public Color? SelectionColor { get; set; }

        /// <summary>The color of the caret, or null to leave it to the layers beneath.</summary>
        public Color? CaretColor { get; set; }

        /// <summary>The color the hint is drawn in while the box is empty, or null to leave it to the layers beneath.</summary>
        public Color? HintColor { get; set; }

        /// <inheritdoc />
        public override void Neutral(Element element)
        {
            base.Neutral(element);

            var box = element as TextBox;
            if (box == null)
                return;

            if (SelectionColor.HasValue)
                box.SelectionColor = new Color(40, 90, 150);

            if (CaretColor.HasValue)
                box.CaretColor = VRageMath.Color.White;

            if (HintColor.HasValue)
                box.HintColor = VRageMath.Color.Gray;
        }

        /// <inheritdoc />
        public override void Lay(Element element)
        {
            base.Lay(element);

            var box = element as TextBox;
            if (box == null)
                return;

            if (SelectionColor.HasValue)
                box.SelectionColor = SelectionColor.Value;

            if (CaretColor.HasValue)
                box.CaretColor = CaretColor.Value;

            if (HintColor.HasValue)
                box.HintColor = HintColor.Value;
        }

        /// <inheritdoc />
        public override void Clear()
        {
            base.Clear();
            SelectionColor = null;
            CaretColor = null;
            HintColor = null;
        }

        /// <inheritdoc />
        public override void Gather(ElementState layer)
        {
            base.Gather(layer);
            var other = layer as TextBoxState;
            if (other == null)
                return;

            if (!SelectionColor.HasValue)
                SelectionColor = other.SelectionColor;

            if (!CaretColor.HasValue)
                CaretColor = other.CaretColor;

            if (!HintColor.HasValue)
                HintColor = other.HintColor;
        }
    }
}
