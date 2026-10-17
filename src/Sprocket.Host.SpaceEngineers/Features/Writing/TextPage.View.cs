using Lumel;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Writing
{
    public sealed partial class TextPage
    {
        Label _count;
        EditBox _box;

        /// <summary>How many characters it holds, at the right, over a box that fills the rest of the page and moves both ways.</summary>
        protected override Element CreateView()
        {
            return new VStack
            {
                Spacing = 6f,
                Children =
                {
                    new HStack
                    {
                        Children =
                        {
                            new HStack { Placement = Flex.Grow },
                            new Label { Style = Styles.Status }.As(out _count)
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
                        Focusable = true,
                        Placement = Flex.Grow
                    }.As(out _box)
                }
            };
        }
    }
}
