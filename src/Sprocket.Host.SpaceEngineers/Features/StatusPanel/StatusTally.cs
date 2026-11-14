using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using System.Globalization;
using Lumel.Layout;

namespace Sprocket.Host.SpaceEngineers.Features.StatusPanel
{
    /// <summary>A mark on the status panel, and beside it how many installations it stands for.</summary>
    public partial class StatusTally : HStack
    {
        readonly StatusLevel _level;

        /// <summary>The tally of installations at <paramref name="level" />.</summary>
        public StatusTally(StatusLevel level)
        {
            _level = level;
            CreateView();
            _mark.Symbol = StatusLook.Mark(level);
        }

        /// <summary>Shows <paramref name="count" /> in <paramref name="look" />, in a bar's size where <paramref name="banner" />, where <paramref name="shown" /> and there are any.</summary>
        public void Show(StatusLook look, PanelSize size, int count, bool banner, bool shown)
        {
            IsVisible = shown && count > 0;
            Base.Spacing = size.Beside;
            _mark.Style = banner ? size.BannerMark : size.Count;
            _mark.Base.Tint = look.Shade(_level);
            _count.Style = banner ? size.Banner : size.Plain;
            _count.Base.Color = look.Ink;
            _count.Text = count.ToString(CultureInfo.InvariantCulture);
        }
    }
}
