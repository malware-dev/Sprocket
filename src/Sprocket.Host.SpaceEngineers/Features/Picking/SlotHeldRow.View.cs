using Lumel;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Picking
{
    public partial class SlotHeldRow
    {
        SlotChip _blocks;
        SlotChip _answers;

        /// <summary>Makes the blocks' part and the part beside it, standing in from the row's mark as the name above them does.</summary>
        void CreateView()
        {
            Base.Spacing = Gaps.Parts;
            Base.Padding = new Thickness(48f, 0f, 8f, 0f);
            Children.Add(new SlotChip(false) { Spacing = Gaps.Within }.As(out _blocks));
            Children.Add(new SlotChip { IsVisible = false, Align = Align.Top }.As(out _answers));
        }
    }
}
