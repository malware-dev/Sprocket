using Lumel;
using Lumel.Controls;
using Lumel.Layout;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Shell;

namespace Sprocket.Host.SpaceEngineers.Features.Home
{
    partial class BlueprintGroup
    {
        const float Gutter = 32f;
        const float RailWidth = 2f;

        StatusRow _heading;
        Rail _rail;
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
                    new Rail { Placement = Fixed.Wide(RailWidth) }.As(out _rail),
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

            // Turned in at the way to add another, the last line the rail runs past.
            _rail.Ends = _another;
        }
    }
}
