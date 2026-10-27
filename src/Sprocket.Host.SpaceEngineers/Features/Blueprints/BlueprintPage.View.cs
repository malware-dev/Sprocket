using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Shell;
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
                Spacing = Gaps.Items,
                Children =
                {
                    new RichText { Style = Styles.Description, IsVisible = false }.As(out _description),

                    new HStack
                    {
                        Spacing = Gaps.Parts,
                        IsVisible = false,
                        Children =
                        {
                            new Label { Style = Styles.ProblemLine, Wraps = true, Placement = Flex.Grow }.As(out _trouble),
                            new Button { Style = Styles.Button }.As(out _openTrouble)
                        }
                    }.As(out _troubled),

                    new Rule(1f, Palette.Line).As(out _rule),
                    new Section
                    {
                        Children =
                        {
                            new HStack
                            {
                                Spacing = Gaps.Parts,
                                Children =
                                {
                                    new RichText { Style = Styles.H1Term }.As(out _partsHeading),
                                    new Label { Style = Styles.Needs, IsVisible = false }.As(out _needs)
                                }
                            },
                            new Label { Style = Styles.Aside, Wraps = true, IsVisible = false }.As(out _locked),
                            new PartList { Spacing = Gaps.Items }.As(out _partList)
                        }
                    }
                }
            };
        }
    }
}
