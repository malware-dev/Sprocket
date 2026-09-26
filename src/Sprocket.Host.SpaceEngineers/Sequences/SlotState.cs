using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Styling;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>What a part of a sentence looks like in one state: its words and fill, and the line along its bottom.</summary>
    public class SlotState : TextState
    {
        /// <summary>The color of the line along the bottom, or null to leave it to the layers beneath.</summary>
        public Color? Underline { get; set; }

        /// <inheritdoc />
        public override void Neutral(Element element)
        {
            base.Neutral(element);

            var slot = element as Slot;
            if (slot != null)
                slot.Underline = VRageMath.Color.Transparent;
        }

        /// <inheritdoc />
        public override void Lay(Element element)
        {
            base.Lay(element);

            var slot = element as Slot;
            if (slot != null && Underline.HasValue)
                slot.Underline = Underline.Value;
        }
    }
}
