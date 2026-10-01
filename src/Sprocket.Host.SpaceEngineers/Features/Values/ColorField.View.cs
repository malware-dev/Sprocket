using Lumel;
using Lumel.Controls;
using Lumel.Layout;
using Lumel.Text;
using VRageMath;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Values
{
    partial class ColorField
    {
        Flow _palette;
        Slider _hue;
        Slider _saturation;
        Slider _value;
        Swatch _chosen;
        TextBox _written;

        /// <summary>Makes the player's colors, the three sliders beside the color they make, and the box it is written in.</summary>
        void CreateView()
        {
            Spacing = 8f;

            Children.Add(new Flow { Spacing = 4f, LineSpacing = 4f }.As(out _palette));
            Children.Add(new HStack
            {
                Spacing = 8f,
                Children =
                {
                    new Grid
                    {
                        Placement = Flex.Grow,
                        ColumnSpacing = 8f,
                        RowSpacing = 8f,
                        Columns = { 20f, Length.Flex(1) },
                        Cells =
                        {
                            [0, 0] = new Label { Style = Styles.ValueLabel, Text = "H" },
                            [0, 1] = new Slider { Track = Symbols.Hue, TrackTint = Color.White, GripColor = Palette.Ink }.As(out _hue),
                            [1, 0] = new Label { Style = Styles.ValueLabel, Text = "S" },
                            [1, 1] = new Slider { TrackTint = Palette.Well, GripColor = Palette.Ink }.As(out _saturation),
                            [2, 0] = new Label { Style = Styles.ValueLabel, Text = "V" },
                            [2, 1] = new Slider { TrackTint = Palette.Well, GripColor = Palette.Ink }.As(out _value)
                        }
                    },
                    new Swatch { Edge = Palette.Line, Placement = Fixed.Size(96f, 88f) }.As(out _chosen)
                }
            });
            Children.Add(new TextBox { Style = Styles.Value, Focusable = true, Placement = Fixed.Wide(148f) }.As(out _written));
        }
    }
}
