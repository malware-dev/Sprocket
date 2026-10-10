using Sprocket.Host.SpaceEngineers.Features.Look;
using Lumel;
using Lumel.Controls;
using Lumel.Layout;
using Lumel.Styling;
using Lumel.Text;
using Lumel.Viewing;
using VRage.Game;

namespace Sprocket.Host.SpaceEngineers.Features.Writing
{
    public partial class Composer
    {
        Button _markup;
        Button _preview;
        EditBox _box;
        ScrollView _previewing;
        RichText _previewed;

        /// <summary>Makes the two buttons over the box, and the box and its preview in one room.</summary>
        void CreateView(float high, IStyle reads)
        {
            Spacing = 6f;

            Children.Add(new HStack
            {
                Spacing = 8f,
                Children =
                {
                    new Button { Style = Styles.Button }.As(out _markup),
                    new Button { Style = Styles.Button }.As(out _preview)
                }
            });

            Children.Add(new VStack
            {
                Children =
                {
                    new EditBox
                    {
                        Font = MyFontEnum.White,
                        FontSize = 20f,
                        Color = Palette.Ink,
                        CaretColor = Palette.Ink,
                        SelectionColor = Palette.Line,
                        HintColor = Palette.Faint,
                        Background = Palette.Page,
                        Edge = Palette.Line,
                        Padding = new Thickness(8f, 6f),
                        BarWidth = Sizes.Bar,
                        Thumb = Palette.Thumb,
                        HeldThumb = Palette.ThumbHeld,
                        LeastThumb = 24f,
                        Placement = Fixed.High(high)
                    }.As(out _box),
                    new ScrollView
                    {
                        Style = Styles.Preview,
                        IsVisible = false,
                        Placement = Fixed.High(high),
                        Content = new RichText { Style = reads }.As(out _previewed)
                    }.As(out _previewing)
                }
            });
        }
    }
}
