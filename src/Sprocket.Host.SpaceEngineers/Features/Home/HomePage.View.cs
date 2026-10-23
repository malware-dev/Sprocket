using Lumel;
using Lumel.Controls;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Home
{
    public sealed partial class HomePage
    {
        RichText _heading;
        Label _empty;
        TextBox _search;
        HStack _headingLine;
        VStack _list;
        Button _new;

        /// <inheritdoc />
        protected override Element CreateView()
        {
            return new VStack
            {
                Children =
                {
                    // The heading and the search over what it heads share one line, the box at its right end.
                    new HStack
                    {
                        Spacing = 16f,
                        Children =
                        {
                            new RichText { Style = Styles.HeadingTerm }.As(out _heading),
                            new VStack { Placement = Flex.Grow },
                            new TextBox { Style = Styles.Search, Focusable = true, Placement = FixedSize.Wide(SearchWide) }.As(out _search)
                        }
                    }.As(out _headingLine),
                    new Label { Style = Styles.Empty, Wraps = true }.As(out _empty),
                    new VStack().As(out _list),
                    new HStack { Padding = new Thickness(0f, 8f, 0f, 0f), Children = { new Button { Style = Styles.Start }.As(out _new) } }
                }
            };
        }
    }
}
