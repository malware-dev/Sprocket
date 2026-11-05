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
        Label _line;
        RichText _about;
        Label _trouble;
        Section _firstSection;
        RichText _firstHeading;
        Label _first;
        Section _secondSection;
        RichText _secondHeading;
        Label _second;
        HStack _shares;
        Button _share;
        Button _withdraw;
        Button _keep;
        Button _block;
        Label _reach;

        /// <summary>Makes the lines about it, and the sections naming its slots and installations.</summary>
        void CreateView()
        {
            Base.Spacing = Gaps.Items;

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
            Children.Add(new VStack
            {
                Base = { Spacing = Gaps.Within },
                Children =
                {
                    new Label { Style = Styles.Detail, IsVisible = false }.As(out _reach),
                    new HStack
                    {
                        Base = { Spacing = Gaps.Parts },
                        Children =
                        {
                            new Button { Style = Styles.Button }.As(out _share),
                            new Button { Style = Styles.Button }.As(out _withdraw),
                            new Button { Style = Styles.Button }.As(out _keep),
                            new Button { Style = Styles.Button }.As(out _block)
                        }
                    }.As(out _shares)
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
