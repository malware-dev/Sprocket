using Lumel;
using Lumel.Controls;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Shell;

namespace Sprocket.Host.SpaceEngineers.Features.Home
{
    public partial class SystemGroup
    {
        const float Gutter = 32f;

        StatusRow _heading;
        Spine _spine;
        RichText _installations;
        Button _another;
        VStack _list;

        /// <summary>Makes the group's heading, a row of its own that opens the logic system, and under it its installations, headed, and the way to add another, set in by a gutter with a spine down it.</summary>
        void CreateView()
        {
            Spacing = 8f;
            Base.Padding = new Thickness(0f, 0f, 0f, 16f);

            Children.Add(new StatusRow().As(out _heading));
            Children.Add(new HStack
            {
                Base = { Padding = new Thickness((Gutter - Sizes.Spine) / 2f, 0f, 0f, 0f) },
                Spacing = (Gutter - Sizes.Spine) / 2f,
                Children =
                {
                    new Spine(Sizes.Spine, Sizes.SpineTurn, Palette.Line) { Placement = FixedSize.Wide(Sizes.Spine) }.As(out _spine),
                    new VStack
                    {
                        Placement = Flex.Grow,
                        Children =
                        {
                            new RichText { Style = Styles.SubHeadingTerm, Align = Align.Left }.As(out _installations),
                            new VStack { Spacing = 8f }.As(out _list),
                            new HStack { Base = { Padding = new Thickness(0f, 8f, 0f, 0f) }, Children = { new Button { Style = Styles.Start }.As(out _another) } }
                        }
                    }
                }
            });

            _spine.Ends = _another;
        }
    }
}
