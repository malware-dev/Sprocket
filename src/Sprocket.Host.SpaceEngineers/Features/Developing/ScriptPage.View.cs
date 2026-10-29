using Lumel;
using Lumel.Controls;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Developing
{
    public sealed partial class ScriptPage
    {
        Label _problem;
        HStack _spacer;
        Button _apply;
        TextArea _listing;

        /// <summary>What is wrong with the text beside the button that applies it, over the script on a listing that fills the rest of the page and moves both ways.</summary>
        protected override Element CreateView()
        {
            return new VStack
            {
                Base = { Spacing = Gaps.Parts },
                Children =
                {
                    new HStack
                    {
                        Base = { Spacing = Gaps.Parts },
                        Children =
                        {
                            new Label { Style = Styles.ProblemLine, TextWrapping = TextWrapping.Wrap, IsVisible = false, Placement = Flex.Grow }.As(out _problem),
                            new HStack { Placement = Flex.Grow }.As(out _spacer),
                            new Button { Style = Styles.Primary }.As(out _apply)
                        }
                    },
                    new TextArea
                    {
                        Style = Styles.ListingBox,
                        ShowLineNumbers = true,
                        Placement = Flex.Grow
                    }.As(out _listing)
                }
            };
        }
    }
}
