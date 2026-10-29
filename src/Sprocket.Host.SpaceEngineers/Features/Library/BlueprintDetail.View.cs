using Lumel;
using Lumel.Controls;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Shell;

namespace Sprocket.Host.SpaceEngineers.Features.Library
{
    public partial class BlueprintDetail
    {
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

        /// <summary>Makes the name over its buttons, the lines about it, and the sections naming its slots and installations.</summary>
        void CreateView()
        {
            Base.Spacing = Gaps.Sections;

            Children.Add(new VStack
            {
                Base = { Spacing = Gaps.Parts },
                Children =
                {
                    new Label { Style = Styles.Name, TextWrapping = TextWrapping.Trim }.As(out _name),
                    new HStack
                    {
                        Base = { Spacing = Gaps.Parts },
                        Children =
                        {
                            new Button { Style = Styles.Button }.As(out _add),
                            new Button { Style = Styles.Button }.As(out _keep),
                            new Button { Style = Styles.Button }.As(out _reset),
                            new Button { Style = Styles.Button }.As(out _copy)
                        }
                    }.As(out _here)
                }
            });
            Children.Add(new VStack
            {
                Base = { Spacing = Gaps.Within },
                Children =
                {
                    new Label { Style = Styles.Detail }.As(out _line),
                    new RichText { Style = Styles.Description, IsVisible = false }.As(out _about),
                    new Label { Style = Styles.Diagnostic, TextWrapping = TextWrapping.Wrap }.As(out _trouble)
                }
            });
            Children.Add(new Section
            {
                Children =
                {
                    new RichText { Style = Styles.H2Term, Align = Align.Left }.As(out _firstHeading),
                    new Label { Style = Styles.Detail, TextWrapping = TextWrapping.Wrap }.As(out _first)
                }
            }.As(out _firstSection));
            Children.Add(new Section
            {
                Children =
                {
                    new RichText { Style = Styles.H2Term, Align = Align.Left }.As(out _secondHeading),
                    new Label { Style = Styles.Detail, TextWrapping = TextWrapping.Wrap }.As(out _second)
                }
            }.As(out _secondSection));
        }
    }
}
