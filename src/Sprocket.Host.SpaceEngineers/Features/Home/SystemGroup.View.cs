using Lumel;
using Lumel.Controls;
using Lumel.Layout;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Shell;

namespace Sprocket.Host.SpaceEngineers.Features.Home
{
    public partial class SystemGroup
    {
        const float Gutter = 32f;

        StatusRow _heading;
        Spine _spine;
        Button _another;
        VStack _list;

        /// <summary>Makes the group's heading, a row of its own that opens the logic system, and under it its rows and the way to add another, set in by a gutter with a spine down it.</summary>
        void CreateView()
        {
            Spacing = 8f;
            Padding = new Thickness(0f, 0f, 0f, 16f);

            Children.Add(new StatusRow().As(out _heading));
            Children.Add(new HStack
            {
                Padding = new Thickness((Gutter - Sizes.Spine) / 2f, 0f, 0f, 0f),
                Spacing = (Gutter - Sizes.Spine) / 2f,
                Children =
                {
                    new Spine(Sizes.Spine, Sizes.SpineTurn, Palette.Line) { Placement = Fixed.Wide(Sizes.Spine) }.As(out _spine),
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

            // Turned in at the way to add another, the last line the spine runs past.
            _spine.Ends = _another;
        }
    }
}
