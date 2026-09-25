using Sprocket.Host.SpaceEngineers.Ui.Controls;

namespace Sprocket.Host.SpaceEngineers.Ui.Styling
{
    /// <summary>What a button looks like in one state: its fill, edge and padding, and its own label's text.</summary>
    public class ButtonState : TextState
    {
        /// <inheritdoc />
        internal override void Neutral(Element element)
        {
            base.Neutral(element);

            var button = element as Button;
            if (button != null)
                NeutralText(button.Label);
        }

        /// <inheritdoc />
        internal override void Lay(Element element)
        {
            base.Lay(element);

            var button = element as Button;
            if (button != null)
                LayText(button.Label);
        }
    }
}
