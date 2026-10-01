using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Controls;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Shell;

namespace Sprocket.Host.SpaceEngineers.Features.Home
{
    partial class BlueprintGroup
    {
        const float Gutter = 32f;
        const float RailWidth = 2f;

        StatusRow _heading;
        Button _another;
        VStack _list;

        /// <summary>Makes the group's heading, a row of its own that opens the blueprint, and under it its rows and the way to add another, set in by a gutter with a rail down it.</summary>
        void CreateView()
        {
            Spacing = 8f;
            Padding = new Thickness(0f, 0f, 0f, 16f);

            Children.Add(new StatusRow().As(out _heading));
            Children.Add(new HStack
            {
                Padding = new Thickness((Gutter - RailWidth) / 2f, 0f, 0f, 0f),
                Spacing = (Gutter - RailWidth) / 2f,
                Children =
                {
                    new VStack { Background = Palette.Line, Placement = Fixed.Wide(RailWidth) },
                    new VStack
                    {
                        Placement = Flex.Grow,
                        Children =
                        {
                            new VStack { Spacing = 8f }.As(out _list),
                            new HStack { Padding = new Thickness(0f, 8f, 0f, 0f), Children = { new Button { Style = Styles.Start }.As(out _another) } }
                        }
                    }
                }
            });
        }
    }
}
