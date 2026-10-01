using Sprocket.Host.SpaceEngineers.Features.Look;
using Lumel;
using Lumel.Controls;
using Lumel.Layout;
using Lumel.Styling;
using Lumel.Text;
using Lumel.Viewing;
using Lumel.Windows;
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
        Popup _guide;
        VStack _examplesList;
        Label _guideNote;

        /// <summary>Makes the two buttons over the box, the box and its preview in one room, and the guide that hangs from its button.</summary>
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

            _guide = new Popup
            {
                Style = Styles.Menu,
                Anchor = _markup,
                Gap = 4f,
                Placement = Fixed.Wide(480f),
                Content = new VStack
                {
                    Spacing = 6f,
                    Children =
                    {
                        new VStack { Spacing = 6f }.As(out _examplesList),
                        new Label { Style = Styles.Detail, Wraps = true }.As(out _guideNote)
                    }
                }
            };
        }

        /// <summary>One mark as it is typed, beside what it makes.</summary>
        static Element Example(string written)
        {
            return new HStack
            {
                Spacing = 12f,
                Children =
                {
                    new Label { Style = Styles.Markup, Text = written, Placement = Fixed.Wide(190f) },
                    new RichText { Style = Styles.Example, Document = Markdown.Parse(written), Placement = Flex.Grow }
                }
            };
        }
    }
}
