using Lumel;
using Lumel.Controls;
using Lumel.Layout;
using Lumel.Text;
using Lumel.Viewing;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Library
{
    sealed partial class LibraryPage
    {
        TextBox _search;
        VStack _list;
        Label _name;
        HStack _here;
        Label _hereLabel;
        Switch _switch;
        Button _keep;
        Button _copy;
        Label _line;
        Label _about;
        Label _trouble;
        Label _firstHeading;
        Label _first;
        Label _secondHeading;
        Label _second;

        /// <inheritdoc />
        /// <remarks>The list, searched, beside the details of what is chosen from it.</remarks>
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
                        Placement = Fixed.Wide(640f),
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
                                Padding = new Thickness(0f, 4f, 16f, 0f),
                                Children =
                                {
                                    new Label { Style = Styles.Name, Trims = true }.As(out _name),
                                    new HStack
                                    {
                                        Spacing = 12f,
                                        Padding = new Thickness(0f, 0f, 0f, 16f),
                                        Children =
                                        {
                                            new Label { Style = Styles.Name }.As(out _hereLabel),
                                            new Switch { Style = Styles.Switch }.As(out _switch),
                                            new HStack { Placement = Fixed.Wide(24f) },
                                            new Button { Style = Styles.Button }.As(out _keep),
                                            new Button { Style = Styles.Button }.As(out _copy)
                                        }
                                    }.As(out _here),
                                    new Label { Style = Styles.Detail }.As(out _line),
                                    new Label { Style = Styles.Empty, Wraps = true }.As(out _about),
                                    new Label { Style = Styles.Diagnostic, Wraps = true }.As(out _trouble),
                                    new Label { Style = Styles.Heading }.As(out _firstHeading),
                                    new Label { Style = Styles.Detail, Wraps = true }.As(out _first),
                                    new Label { Style = Styles.Heading }.As(out _secondHeading),
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
