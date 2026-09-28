using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Controls;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.Ui.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Home
{
    partial class BlueprintGroup
    {
        RichText _heading;
        Button _edit;
        VStack _list;

        /// <summary>Makes the group's heading with the button that opens the blueprint on the right, and the place its rows go.</summary>
        void CreateView()
        {
            // Padded on the right as the rows under it are, so the button ends where their controls do.
            Children.Add(new HStack
            {
                Spacing = 8f,
                Padding = new Thickness(4f, 24f, 16f, 8f),
                Children =
                {
                    new RichText { Style = Styles.Section, Placement = Flex.Grow }.As(out _heading),
                    new Button { Style = Styles.Button }.As(out _edit)
                }
            });
            Children.Add(new VStack { Spacing = 8f }.As(out _list));
        }
    }
}
