using Sprocket.Host.SpaceEngineers.Features.Look;
using Lumel;
using Lumel.Controls;
using Lumel.Layout;
using Lumel.Styling;
using Lumel.Text;
using Lumel.Viewing;

namespace Sprocket.Host.SpaceEngineers.Features.Writing
{
    public partial class Composer
    {
        Button _markup;
        Button _preview;
        TextArea _box;
        ScrollView _previewing;
        RichText _previewed;

        /// <summary>Makes the two buttons over the box, and the box and its preview in one room.</summary>
        void CreateView(float high, IStyle<RichText> reads)
        {
            Base.Spacing = Gaps.Within;

            Children.Add(new HStack
            {
                Base = { Spacing = Gaps.Parts },
                Children =
                {
                    new Button { Style = Styles.Button }.As(out _markup),
                    new Button { Style = Styles.Toggle }.As(out _preview)
                }
            });

            Children.Add(new VStack
            {
                Children =
                {
                    new TextArea
                    {
                        Style = Styles.WritingBox,
                        Placement = FixedSize.Height(high)
                    }.As(out _box),
                    new ScrollView
                    {
                        Style = Styles.Preview,
                        IsVisible = false,
                        Placement = FixedSize.Height(high),
                        Content = new RichText { Style = reads }.As(out _previewed)
                    }.As(out _previewing)
                }
            });
        }
    }
}
