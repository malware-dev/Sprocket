using Lumel;
using Lumel.Controls;
using Lumel.Layout;
using Lumel.Text;
using Lumel.Viewing;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Shell;

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
        Section _firstSection;
        RichText _firstHeading;
        Label _first;
        Section _secondSection;
        RichText _secondHeading;
        Label _second;

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
                            new VStack
                            {
                                Spacing = Gaps.Sections,
                                Children =
                                {
                                    new VStack
                                    {
                                        Spacing = Gaps.Parts,
                                        Children =
                                        {
                                            new Label { Style = Styles.Name, Trims = true }.As(out _name),
                                            new HStack
                                            {
                                                Spacing = Gaps.Parts,
                                                Children =
                                                {
                                                    new Button { Style = Styles.Button }.As(out _add),
                                                    new Button { Style = Styles.Button }.As(out _keep),
                                                    new Button { Style = Styles.Button }.As(out _reset),
                                                    new Button { Style = Styles.Button }.As(out _copy)
                                                }
                                            }.As(out _here)
                                        }
                                    },
                                    new VStack
                                    {
                                        Spacing = Gaps.Within,
                                        Children =
                                        {
                                            new Label { Style = Styles.Detail }.As(out _line),
                                            new RichText { Style = Styles.Description, IsVisible = false }.As(out _about),
                                            new Label { Style = Styles.Diagnostic, Wraps = true }.As(out _trouble)
                                        }
                                    },
                                    new Section
                                    {
                                        Children =
                                        {
                                            new RichText { Style = Styles.H2Term, Align = Align.Left }.As(out _firstHeading),
                                            new Label { Style = Styles.Detail, Wraps = true }.As(out _first)
                                        }
                                    }.As(out _firstSection),
                                    new Section
                                    {
                                        Children =
                                        {
                                            new RichText { Style = Styles.H2Term, Align = Align.Left }.As(out _secondHeading),
                                            new Label { Style = Styles.Detail, Wraps = true }.As(out _second)
                                        }
                                    }.As(out _secondSection)
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
