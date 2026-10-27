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
        EditBox _listing;

        /// <summary>What is wrong with the text beside the button that applies it, over the script on a listing that fills the rest of the page and moves both ways.</summary>
        protected override Element CreateView()
        {
            return new VStack
            {
                Spacing = 6f,
                Children =
                {
                    new HStack
                    {
                        Spacing = 12f,
                        Children =
                        {
                            new Label { Style = Styles.ProblemLine, Wraps = true, IsVisible = false, Placement = Flex.Grow }.As(out _problem),
                            new HStack { Placement = Flex.Grow }.As(out _spacer),
                            new Button { Style = Styles.Primary }.As(out _apply)
                        }
                    },
                    new EditBox
                    {
                        Style = Styles.ListingBox,
                        Numbered = true,
                        Placement = Flex.Grow
                    }.As(out _listing)
                }
            };
        }
    }
}
