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
        PageSlide _slide;
        StatusColumns _leaving;
        StatusColumns _coming;
        HStack _dots;
        Label _pageOf;

        /// <summary>
        /// Makes the top line, the middle, which holds the calm screen a little above its middle or the pages of cards over
        /// its whole height, the one before sliding out as the next slides in, and the foot, which holds the page dots or
        /// which page this is.
        /// </summary>
        void CreateView()
        {
            Children.Add(new HStack
            {
                Children =
                {
                    new Image { Source = StatusPicture.Icon(), Base = { Tint = Color.White }, Align = Align.Center }.As(out _icon),
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

            _slide = new PageSlide
            {
                Leaving = new StatusColumns().As(out _leaving),
                Coming = new StatusColumns().As(out _coming)
            };

            Children.Add(new HStack { Align = Align.Center }.As(out _dots));
            Children.Add(new Label { Base = { TextAlignment = TextAlignment.Center } }.As(out _pageOf));
        }
    }
}
