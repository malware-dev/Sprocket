using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Lumel;
using Lumel.Controls;
using Lumel.Images;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using Lumel.Viewing;
using Lumel.Windows;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Writing
{
    public partial class GuideWindow
    {
        RichText _title;
        Glyph _close;
        RichText _body;

        /// <summary>The bar it is moved by, holding the guide's name and the close cross, over the guide itself.</summary>
        void CreateView()
        {
            Style = Styles.GuideWindow;
            Placement = FixedSize.Size(Wide, High);

            Content = new VStack
            {
                Children =
                {
                    new TitleBar
                    {
                        Window = this,
                        Content = new HStack
                        {
                            Base = { Spacing = Gaps.Parts, Background = Palette.Chrome, Padding = new Thickness(12f, 8f) },
                            Children =
                            {
                                new RichText { Style = Styles.Tip, Placement = Flex.Grow }.As(out _title),
                                new Glyph { Symbol = Symbols.Close, Style = Styles.Icon, Placement = FixedSize.Size(32f, 32f), Command = new RelayCommand(Close) }.As(out _close)
                            }
                        }
                    },
                    new Separator(),
                    new ScrollView
                    {
                        Style = Styles.ScrollView,
                        Placement = Flex.Grow,
                        Content = new RichText { Style = Styles.Reading }.As(out _body)
                    }
                }
            };
        }
    }
}
