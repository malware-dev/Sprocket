using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Lumel;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    partial class FacetChoice
    {
        Label _says;

        /// <summary>Makes the line, in line with the heading over it, with what the chosen one is doing at its end.</summary>
        /// <remarks>The drop box goes ahead of it once there are names to offer.</remarks>
        void CreateView()
        {
            Base.Spacing = Gaps.Items;
            Base.Padding = new Thickness(12f, 0f, 0f, 0f);
            Children.Add(new Label { Style = Styles.ValueLabel, Align = Align.Left }.As(out _says));
        }
    }
}
