using Lumel;
using Lumel.Controls;
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
        Label _name;
        HStack _here;
        Button _add;
        Button _keep;
        Button _reset;
        Button _copy;
        Label _line;
        RichText _about;
        Label _trouble;
        RichText _firstHeading;
        Label _first;
        RichText _secondHeading;
        Label _second;

        /// <inheritdoc />
        protected override Element CreateView()
        {
            return new HStack
            {
                Spacing = 40f,
                Children =
                {
                    new VStack
                    {
                        Spacing = 12f,
                        Placement = FixedSize.Wide(640f),
                        Children =
                        {
                            new TextBox { Style = Styles.Search, Focusable = true }.As(out _search),
                            new ScrollView
                            {
                                Style = Styles.ScrollView,
                                Placement = Flex.Grow,
                                Content = new VStack().As(out _list)
                            }
                        }
                    },
                    new VStack
                    {
                        Placement = Flex.Grow,
                        Children =
                        {
                            new VStack
                            {
                                Spacing = 12f,
                                Base = { Padding = new Thickness(0f, 4f, 16f, 0f) },
                                Children =
                                {
                                    new Label { Style = Styles.Name, Trims = true }.As(out _name),
                                    new HStack
                                    {
                                        Spacing = 12f,
                                        Base = { Padding = new Thickness(0f, 0f, 0f, 16f) },
                                        Children =
                                        {
                                            new Button { Style = Styles.Button }.As(out _add),
                                            new Button { Style = Styles.Button }.As(out _keep),
                                            new Button { Style = Styles.Button }.As(out _reset),
                                            new Button { Style = Styles.Button }.As(out _copy)
                                        }
                                    }.As(out _here),
                                    new Label { Style = Styles.Detail }.As(out _line),
                                    new RichText { Style = Styles.LibraryAbout, IsVisible = false }.As(out _about),
                                    new Label { Style = Styles.Diagnostic, Wraps = true }.As(out _trouble),
                                    new RichText { Style = Styles.HeadingTerm, Align = Align.Left }.As(out _firstHeading),
                                    new Label { Style = Styles.Detail, Wraps = true }.As(out _first),
                                    new RichText { Style = Styles.HeadingTerm, Align = Align.Left }.As(out _secondHeading),
                                    new Label { Style = Styles.Detail, Wraps = true }.As(out _second)
                                }
                            },
                            new VStack { Placement = Flex.Grow }
                        }
                    }
                }
            };
        }
    }
}
