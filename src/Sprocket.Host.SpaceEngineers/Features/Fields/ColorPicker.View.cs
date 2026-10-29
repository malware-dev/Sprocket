using Lumel;
using Lumel.Controls;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Fields
{
    partial class ColorPicker
    {
        ColorSpectrum _plane;
        HueSlider _strip;
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
            Base.Spacing = Gaps.Parts;

            Children.Add(new HStack
            {
                Base = { Spacing = Gaps.Parts },
                Children =
                {
                    new ColorSpectrum { Base = { Pale = Materials.Pale, Dark = Materials.Dark, Border = Palette.Line }, Placement = FixedSize.Size(Wide - StripWide - 8f, PlaneHigh) }.As(out _plane),
                    new HueSlider { Base = { Hues = Materials.Hues, Border = Palette.Line }, Placement = FixedSize.Size(StripWide, PlaneHigh) }.As(out _strip)
                }
            });
            Children.Add(new HStack
            {
                Base = { Spacing = Gaps.Parts },
                Children =
                {
                    new Swatch { Style = Styles.ColorSample, Placement = FixedSize.Size(ShownWide, ShownHigh) }.As(out _new),
                    new TabView
                    {
                        Base = { Spacing = Gaps.Parts },
                        Placement = Flex.Grow,
                        Tabs =
                        {
                            new Tab { Style = Styles.Tab, Header = "HSV", Content = Channels("H", 360, out _hueBox, "S", 100, out _saturationBox, "V", 100, out _valueBox) },
                            new Tab { Style = Styles.Tab, Header = "RGB", Content = Channels("R", 255, out _redBox, "G", 255, out _greenBox, "B", 255, out _blueBox) },
                            new Tab
                            {
                                Style = Styles.Tab,
                                Header = "Hex",
                                Content = new TextBox { Style = Styles.Value, Focusable = true, SelectsAll = true, Placement = FixedSize.Width(CodeWide) }.As(out _written)
                            }
                        }
                    }.As(out _modes)
                }
            });
            // The paint colors and the colors chosen last, two lists, set further apart than the colors in either.
            Children.Add(new VStack
            {
                Base = { Spacing = Gaps.Items },
                Children = { Swatches().As(out _palette), Swatches().As(out _recent) }
            });
        }

        /// <summary>Three channels in a line, each a letter before the box it is typed in.</summary>
        static HStack Channels(string first, int firstMost, out NumberBox firstBox, string second, int secondMost, out NumberBox secondBox,
            string third, int thirdMost, out NumberBox thirdBox)
        {
            return new HStack
            {
                Base = { Spacing = Gaps.Parts },
                Children =
                {
                    Lettered(first, Channel(firstMost).As(out firstBox)),
                    Lettered(second, Channel(secondMost).As(out secondBox)),
                    Lettered(third, Channel(thirdMost).As(out thirdBox))
                }
            };
        }

        /// <summary>A channel's letter, close before the box it is typed in.</summary>
        static HStack Lettered(string letter, NumberBox box)
        {
            return new HStack
            {
                Base = { Spacing = Gaps.Within },
                Children = { new Label { Style = Styles.ValueLabel, Text = letter, Align = Align.Center }, box }
            };
        }

        /// <summary>The box one channel is typed in, as a whole number from nought to <paramref name="most" />.</summary>
        static NumberBox Channel(int most)
        {
            var said = most.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return new NumberBox
            {
                Style = Styles.Number, Focusable = true, SelectsAll = true, IsInteger = true, Minimum = 0, Maximum = most, Widest = said,
                Placement = FixedSize.Width(ChannelWide)
            };
        }

        /// <summary>Rows of swatches in columns of their width, as many to a row as fill the picker's width exactly.</summary>
        static Grid Swatches()
        {
            var grid = new Grid { Base = { ColumnSpacing = Gap, RowSpacing = Gap } };
            for (var i = 0; i < Across; i++)
                grid.Columns.Add(SwatchSize);

            return grid;
        }
    }
}
