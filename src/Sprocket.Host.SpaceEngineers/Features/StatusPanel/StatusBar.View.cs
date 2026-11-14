using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Lumel;
using Lumel.Images;
using Lumel.Layout;
using Lumel.Text;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Features.StatusPanel
{
    partial class StatusBar
    {
        HStack _name;
        Image _icon;
        Label _heading;
        StatusTally _problems;
        StatusTally _warnings;
        Glyph _check;
        Label _says;
        HStack _right;

        /// <summary>Makes the two halves: the icon and the heading, then the counts, or the check and its words.</summary>
        void CreateView()
        {
            Children.Add(new Grid
            {
                Columns = { GridLength.Flex(1f), GridLength.Flex(1f) },
                Rows = { GridLength.Flex(1f) },
                Placement = Flex.Grow,
                Cells =
                {
                    [0, 0] = new HStack
                    {
                        Align = Align.Left | Align.Center,
                        Children =
                        {
                            new Image { Source = StatusPicture.Icon(), Base = { Tint = Color.White }, Align = Align.Center }.As(out _icon),
                            new Label { Align = Align.Center }.As(out _heading)
                        }
                    }.As(out _name),
                    [0, 1] = new HStack
                    {
                        Align = Align.Right | Align.Center,
                        Children =
                        {
                            new StatusTally(StatusLevel.Problem).As(out _problems),
                            new StatusTally(StatusLevel.Warning).As(out _warnings),
                            new Glyph { Symbol = Symbols.Check, Align = Align.Center }.As(out _check),
                            new Label { Align = Align.Center }.As(out _says)
                        }
                    }.As(out _right)
                }
            });
        }
    }
}
