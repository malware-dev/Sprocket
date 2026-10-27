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
            Spacing = Gaps.Parts;

            Children.Add(new HStack
            {
                Spacing = Gaps.Parts,
                Children =
                {
                    new ColorPlane { Base = { Edge = Palette.Line }, Pale = Materials.Pale, Dark = Materials.Dark, Placement = FixedSize.Size(Wide - StripWide - 8f, PlaneHigh) }.As(out _plane),
                    new HueStrip { Base = { Edge = Palette.Line }, Hues = Materials.Hues, Placement = FixedSize.Size(StripWide, PlaneHigh) }.As(out _strip)
                }
            });
            Children.Add(new HStack
            {
                Spacing = Gaps.Parts,
                Children =
                {
                    new Swatch { Style = Styles.ColorSample, Placement = FixedSize.Size(ShownWide, ShownHigh) }.As(out _new),
                    new TabView
                    {
                        HeaderStyle = Styles.Tab,
                        Spacing = Gaps.Parts,
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
            // The paint colors and the colors chosen last, two lists, set further apart than the colors in either.
            Children.Add(new VStack
            {
                Spacing = Gaps.Items,
                Children = { Swatches().As(out _palette), Swatches().As(out _recent) }
            });
        }

        /// <summary>Three channels in a line, each a letter before the box it is typed in.</summary>
        static HStack Channels(string first, int firstMost, out NumberBox firstBox, string second, int secondMost, out NumberBox secondBox,
            string third, int thirdMost, out NumberBox thirdBox)
        {
            return new HStack
            {
                Spacing = Gaps.Parts,
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
                Spacing = Gaps.Within,
                Children = { new Label { Style = Styles.ValueLabel, Text = letter, Align = Align.Center }, box }
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
