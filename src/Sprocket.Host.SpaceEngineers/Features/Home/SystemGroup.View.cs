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
        StatusRow _heading;
        TreeLine _spine;
        RichText _installations;
        Button _another;
        VStack _list;

        /// <summary>Makes the group's heading, a row of its own that opens the logic system, and under it its installations, headed, and the way to add another, set in by a gutter with a spine down it.</summary>
        void CreateView()
        {
            Base.Spacing = Gaps.Parts;

            Children.Add(new StatusRow().As(out _heading));
            Children.Add(new HStack
            {
                Base = { Spacing = (Sizes.Indent - Sizes.Spine) / 2f, Padding = new Thickness((Sizes.Indent - Sizes.Spine) / 2f, 0f, 0f, 0f) },
                Children =
                {
                    new TreeLine() { Placement = FixedSize.Width(Sizes.Spine) }.As(out _spine),
                    new VStack
                    {
                        Base = { Spacing = Gaps.Items },
                        Placement = Flex.Grow,
                        Children =
                        {
                            new Section
                            {
                                Children =
                                {
                                    new RichText { Style = Styles.H2Term, Align = Align.Left }.As(out _installations),
                                    new VStack { Base = { Spacing = Gaps.Items } }.As(out _list)
                                }
                            },
                            new HStack { Children = { new Button { Style = Styles.Start }.As(out _another) } }
                        }
                    }
                }
            });

            _spine.Ends = _another;
        }
    }
}
