using Lumel;
using Lumel.Controls;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Shell;

namespace Sprocket.Host.SpaceEngineers.Features.Home
{
    public sealed partial class HomePage
    {
        RichText _heading;
        Label _empty;
        TextBox _search;
        VStack _list;
        Button _new;

        /// <inheritdoc />
        protected override Element CreateView()
        {
            return new VStack
            {
                Base = { Spacing = Gaps.Sections },
                Children =
                {
                    new Section
                    {
                        Children =
                        {
                            // The heading and the search over what it heads share one line, the box at its right end.
                            new HStack
                            {
                                Base = { Spacing = Gaps.Items },
                                Children =
                                {
                                    new RichText { Style = Styles.H1Term }.As(out _heading),
                                    new VStack { Placement = Flex.Grow },
                                    new TextBox { Style = Styles.Search, Focusable = true, Placement = FixedSize.Width(SearchWide) }.As(out _search)
                                }
                            },
                            new Label { Style = Styles.Empty, TextWrapping = TextWrapping.Wrap }.As(out _empty),
                            new VStack { Base = { Spacing = Gaps.Sections } }.As(out _list)
                        }
                    },
                    new HStack { Children = { new Button { Style = Styles.Start }.As(out _new) } }
                }
            };
        }
    }
}
