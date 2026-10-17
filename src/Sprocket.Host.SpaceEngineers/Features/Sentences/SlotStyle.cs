using Lumel;
using Lumel.Styling;

namespace Sprocket.Host.SpaceEngineers.Features.Sentences
{
    /// <summary>How a part of a sentence looks, and how it changes while nothing fills it, while it cannot be pressed, and while hot.</summary>
    /// <remarks>Laid in that order, so a part that cannot be pressed is never hot, and one that is empty says so either way.</remarks>
    public sealed class SlotStyle : Style<SlotState>
    {
        /// <summary>A style with nothing set, which applies as the neutral values.</summary>
        public SlotStyle()
        {
            Empty = Layer();
            Still = Layer();
            Hot = Layer();
        }

        /// <summary>A style that is everything <paramref name="basedOn" /> is, with whatever this sets laid over it.</summary>
        public SlotStyle(SlotStyle basedOn) : base(basedOn)
        {
            Empty = Layer();
            Still = Layer();
            Hot = Layer();
        }

        /// <summary>What changes while nothing fills the part.</summary>
        public SlotState Empty { get; }

        /// <summary>What changes while the part cannot be pressed.</summary>
        public SlotState Still { get; }

        /// <summary>What changes while a press would reach the part.</summary>
        public SlotState Hot { get; }

        /// <inheritdoc />
        protected override void LayStates(Element element)
        {
            var slot = element as SlotChip;
            if (slot != null && slot.IsEmpty)
                Lay(Empty, element);

            if (element.Command == null || !element.IsEnabled)
            {
                Lay(Still, element);
                return;
            }

            if (element.IsHot)
                Lay(Hot, element);
        }
    }
}
