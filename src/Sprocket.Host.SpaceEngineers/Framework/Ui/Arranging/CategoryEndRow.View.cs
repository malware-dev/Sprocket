using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using Lumel;
using Lumel.Controls;
using Lumel.Layout;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Arranging
{
    partial class CategoryEndRow
    {
        DragHandle _grip;
        PlusMenu _adds;

        /// <summary>Makes the grip, which is never shown, and the way to add something into the category.</summary>
        void CreateView()
        {
            Base.Spacing = Gaps.Parts;
            Children.Add(new DragHandle { IsVisible = false, CanDrag = false, Placement = FixedSize.Size(24f, 24f) }.As(out _grip));
            Children.Add(new PlusMenu { Align = Align.Left }.As(out _adds));
        }
    }
}
