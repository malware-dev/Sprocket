using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Lumel;
using Lumel.Layout;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Writing
{
    public sealed partial class TextPage
    {
        Label _count;
        TextArea _box;

        /// <summary>How many characters it holds, at the right, over a box that fills the rest of the page and moves both ways.</summary>
        protected override Element CreateView()
        {
            return new VStack
            {
                Base = { Spacing = Gaps.Within },
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
                    new TextArea
                    {
                        Style = Styles.ListingBox,
                        ShowLineNumbers = true,
                        Focusable = true,
                        Placement = Flex.Grow
                    }.As(out _box)
                }
            };
        }
    }
}
