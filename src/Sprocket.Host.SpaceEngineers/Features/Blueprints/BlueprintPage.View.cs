using Sprocket.Host.SpaceEngineers.Features.Look;
using Lumel;
using Lumel.Controls;
using Lumel.Layout;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.Features.Blueprints
{
    public sealed partial class BlueprintPage
    {
        RichText _description;
        HStack _troubled;
        Label _trouble;
        Button _openTrouble;
        Rule _rule;
        RichText _partsHeading;
        Label _needs;
        Label _locked;
        PartList _partList;

        /// <summary>What the blueprint's author wrote about it, then the slots with how many still need a block.</summary>
        protected override Element CreateView()
        {
            return new VStack
            {
                Spacing = 4f,
                Children =
                {
                    new RichText { Style = Styles.Description, IsVisible = false }.As(out _description),

                    new HStack
                    {
                        Spacing = 8f,
                        Base = { Padding = new Thickness(0f, 0f, 0f, 8f) },
                        IsVisible = false,
                        Children =
                        {
                            new Label { Style = Styles.ProblemLine, Wraps = true, Placement = Flex.Grow }.As(out _trouble),
                            new Button { Style = Styles.Button }.As(out _openTrouble)
                        }
                    }.As(out _troubled),

                    new Rule(1f, Palette.Line) { Base = { Padding = new Thickness(0f, 8f) } }.As(out _rule),
                    new HStack
                    {
                        Spacing = 12f,
                        Children =
                        {
                            new RichText { Style = Styles.HeadingTerm }.As(out _partsHeading),
                            new Label { Style = Styles.Needs, IsVisible = false }.As(out _needs)
                        }
                    },
                    new Label { Base = { Padding = new Thickness(4f, 0f, 0f, 8f) }, Style = Styles.Aside, Wraps = true, IsVisible = false }.As(out _locked),
                    new PartList { Spacing = 16f }.As(out _partList)
                }
            };
        }
    }
}
