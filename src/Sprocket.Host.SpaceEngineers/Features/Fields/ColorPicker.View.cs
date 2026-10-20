using Lumel;
using Lumel.Controls;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Fields
{
    partial class ColorPicker
    {
        ColorPlane _plane;
        HueStrip _strip;
        Swatch _new;
        TabView _modes;
        NumberBox _hueBox;
        NumberBox _saturationBox;
        NumberBox _valueBox;
        NumberBox _redBox;
        NumberBox _greenBox;
        NumberBox _blueBox;
        TextBox _written;
        Grid _palette;
        Grid _recent;

        /// <summary>
        /// Makes the square of saturation and value beside the strip of hues; under them the color being made, beside a tab
        /// each for typing it by hue, saturation and value, by red, green and blue, or as a code; and under those the player's
        /// paint colors and the colors they chose last.
        /// </summary>
        void CreateView()
        {
            Spacing = 8f;

            Children.Add(new HStack
            {
                Spacing = 8f,
                Children =
                {
                    new ColorPlane { Edge = Palette.Line, Pale = Materials.Pale, Dark = Materials.Dark, Placement = FixedSize.Size(Wide - StripWide - 8f, PlaneHigh) }.As(out _plane),
                    new HueStrip { Edge = Palette.Line, Hues = Materials.Hues, Placement = FixedSize.Size(StripWide, PlaneHigh) }.As(out _strip)
                }
            });
            Children.Add(new HStack
            {
                Spacing = 8f,
                Children =
                {
                    new Swatch { Edge = Palette.Line, Placement = FixedSize.Size(ShownWide, ShownHigh) }.As(out _new),
                    new TabView
                    {
                        HeaderStyle = Styles.Tab,
                        Spacing = 8f,
                        Placement = Flex.Grow,
                        Tabs =
                        {
                            new Tab { Header = "HSV", Content = Channels("H", 360, out _hueBox, "S", 100, out _saturationBox, "V", 100, out _valueBox) },
                            new Tab { Header = "RGB", Content = Channels("R", 255, out _redBox, "G", 255, out _greenBox, "B", 255, out _blueBox) },
                            new Tab
                            {
                                Header = "Hex",
                                Content = new TextBox { Style = Styles.Value, Focusable = true, SelectsAll = true, Placement = FixedSize.Wide(CodeWide) }.As(out _written)
                            }
                        }
                    }.As(out _modes)
                }
            });
            Children.Add(Swatches().As(out _palette));
            // Set further off than the paint colors are from each other, so the row that fills itself reads as a row apart.
            Children.Add(Swatches().As(out _recent));
            _recent.Padding = new Thickness(0f, 8f, 0f, 0f);
        }

        /// <summary>Three channels in a line, each a letter before the box it is typed in.</summary>
        static HStack Channels(string first, int firstMost, out NumberBox firstBox, string second, int secondMost, out NumberBox secondBox,
            string third, int thirdMost, out NumberBox thirdBox)
        {
            return new HStack
            {
                Spacing = 4f,
                Children =
                {
                    new Label { Style = Styles.ValueLabel, Text = first, Align = Align.Center },
                    Channel(firstMost).As(out firstBox),
                    new Label { Style = Styles.ValueLabel, Text = second, Align = Align.Center, Padding = new Thickness(8f, 0f, 0f, 0f) },
                    Channel(secondMost).As(out secondBox),
                    new Label { Style = Styles.ValueLabel, Text = third, Align = Align.Center, Padding = new Thickness(8f, 0f, 0f, 0f) },
                    Channel(thirdMost).As(out thirdBox)
                }
            };
        }

        /// <summary>The box one channel is typed in, as a whole number from nought to <paramref name="most" />.</summary>
        static NumberBox Channel(int most)
        {
            var said = most.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return new NumberBox
            {
                Style = Styles.Value, Focusable = true, SelectsAll = true, Whole = true, Least = 0, Most = most, Widest = said,
                Placement = FixedSize.Wide(ChannelWide)
            };
        }

        /// <summary>Rows of swatches in columns of their width, as many to a row as fill the picker's width exactly.</summary>
        static Grid Swatches()
        {
            var grid = new Grid { ColumnSpacing = Gap, RowSpacing = Gap };
            for (var i = 0; i < Across; i++)
                grid.Columns.Add(SwatchSize);

            return grid;
        }
    }
}
