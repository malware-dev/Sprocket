using Lumel;
using Lumel.Controls;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.LogicSystems
{
    partial class NameChip
    {
        Button _name;
        TextBox _box;

        /// <summary>Makes the name, and the box it is typed in.</summary>
        void CreateView()
        {
            Children.Add(new Button { Style = Styles.SlotNameStill }.As(out _name));
            Children.Add(new TextBox { Style = Styles.Field, IsVisible = false, Longest = NameLongest, Placement = FixedSize.Wide(240f) }.As(out _box));
        }
    }
}
