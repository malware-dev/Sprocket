using Lumel;
using Lumel.Layout;
using Lumel.Text;
using Lumel.Viewing;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Library
{
    public sealed partial class LibraryPage
    {
        TextBox _search;
        VStack _list;
        BlueprintDetail _detail;

        /// <inheritdoc />
        protected override Element CreateView()
        {
            return new HStack
            {
                Spacing = Gaps.Sections,
                Children =
                {
                    new VStack
                    {
                        Spacing = Gaps.Parts,
                        Placement = FixedSize.Wide(640f),
                        Children =
                        {
                            new TextBox { Style = Styles.Search, Focusable = true }.As(out _search),
                            new ScrollView
                            {
                                Style = Styles.ScrollView,
                                Placement = Flex.Grow,
                                Content = new VStack { Spacing = Gaps.Sections }.As(out _list)
                            }
                        }
                    },
                    new VStack
                    {
                        Placement = Flex.Grow,
                        Children =
                        {
                            new BlueprintDetail(Editor).As(out _detail),
                            new VStack { Placement = Flex.Grow }
                        }
                    }
                }
            };
        }
    }
}
