using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Lumel.Styling;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Parts
{
    /// <summary>How a part of a sentence looks, and how it changes while nothing fills it, while it cannot be pressed, and while hot.</summary>
    public class SlotStyle : Style<SlotChip, TextLook>
    {
        /// <summary>A style with nothing set, which applies as the neutral values.</summary>
        public SlotStyle()
        {
        }

        /// <summary>A style that is everything <paramref name="basedOn" /> is, with whatever this sets laid over it.</summary>
        public SlotStyle(IStyle<SlotChip> basedOn) : base(basedOn)
        {
        }

        /// <summary>What changes while nothing fills the part.</summary>
        public TextLook Empty => this[LookStates.Empty];

        /// <summary>What changes while the part cannot be pressed.</summary>
        public TextLook Still => this[LookStates.Still];
    }
}
