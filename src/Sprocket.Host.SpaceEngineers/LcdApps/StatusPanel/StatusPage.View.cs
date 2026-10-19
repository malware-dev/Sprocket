using Lumel;
using Lumel.Images;
using Lumel.Layout;
using Lumel.Text;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.LcdApps.StatusPanel
{
    partial class StatusPage
    {
        HStack _top;
        Image _icon;
        Label _heading;
        Label _construct;
        StatusTally _problems;
        StatusTally _warnings;
        Grid _middle;
        StatusCalm _calm;
        HStack _columns;
        VStack _first;
        VStack _second;
        HStack _dots;
        Label _pageOf;

        /// <summary>
        /// Makes the top line, the middle, which holds the calm screen a little above its middle or the columns of cards over
        /// its whole height, and the foot, which holds the page dots or which page this is.
        /// </summary>
        void CreateView()
        {
            Children.Add(new HStack
            {
                Children =
                {
                    new Image { Source = StatusPicture.Icon(), Tint = Color.White, Align = Align.Center }.As(out _icon),
                    new Label { Placement = Flex.Grow, Align = Align.Center }.As(out _heading),
                    new Label { Align = Align.Center }.As(out _construct),
                    new StatusTally(StatusLevel.Problem).As(out _problems),
                    new StatusTally(StatusLevel.Warning).As(out _warnings)
                }
            }.As(out _top));

            Children.Add(new Grid
            {
                Columns = { GridLength.Flex(1f) },
                Rows = { GridLength.Flex(2f), GridLength.Auto, GridLength.Flex(3f) },
                Placement = Flex.Grow,
                Cells =
                {
                    [1, 0] = new StatusCalm().As(out _calm)
                }
            }.As(out _middle));

            _columns = new HStack
            {
                Children =
                {
                    new VStack { Align = Align.Tall, Placement = Flex.Grow }.As(out _first),
                    new VStack { Align = Align.Tall, Placement = Flex.Grow }.As(out _second)
                }
            };

            Children.Add(new HStack { Align = Align.Center }.As(out _dots));
            Children.Add(new Label { Centres = true }.As(out _pageOf));
        }
    }
}
