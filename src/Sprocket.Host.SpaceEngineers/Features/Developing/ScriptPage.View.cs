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
        /// <remarks>
        /// Deliberately plain: the program's own text on the page's own ground, in a face whose characters are all one width,
        /// because the indent of a step under its sequence only lines up that way. Numbered, because a listing is talked about
        /// by line number: a reader says which line it could not read.
        /// </remarks>
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
                            new Label { Style = Styles.Problem, Wraps = true, IsVisible = false, Placement = Flex.Grow }.As(out _problem),
                            new HStack { Placement = Flex.Grow }.As(out _spacer),
                            new Button { Style = Styles.Primary }.As(out _apply)
                        }
                    },
                    new EditBox
                    {
                        Font = TextSizes.Mono,
                        FontSize = 20f,
                        Color = Palette.Ink,
                        CaretColor = Palette.Ink,
                        SelectionColor = Palette.Line,
                        Numbered = true,
                        NumberColor = Palette.Dim,
                        Padding = new Thickness(20f, 8f, 8f, 8f),
                        BarWidth = Sizes.Bar,
                        Thumb = Palette.Thumb,
                        HeldThumb = Palette.ThumbHeld,
                        LeastThumb = 32f,
                        Placement = Flex.Grow
                    }.As(out _listing)
                }
            };
        }
    }
}
