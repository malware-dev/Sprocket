using System.Globalization;
using Lumel.Drawing;
using Lumel.Layout;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.LcdApps.StatusPanel
{
    /// <summary>
    /// The status panel of a construct with nothing wanting looking at: a check, that all systems are nominal, and how many
    /// logic systems there are; or, where it has no automation, that it has none and what to do about it.
    /// </summary>
    public partial class StatusCalm : VStack
    {
        /// <summary>A calm screen showing nothing yet.</summary>
        public StatusCalm()
        {
            CreateView();
        }

        /// <summary>
        /// Shows <paramref name="status" /> in <paramref name="look" />, leaving out, until it is no taller than
        /// <paramref name="high" /> laid <paramref name="wide" /> across, the count or what to do, then the check, then the
        /// words' size; answers whether it fits.
        /// </summary>
        public bool Show(IGraphics graphics, StatusLook look, ConstructStatus status, float high, float wide)
        {
            var size = look.Size;
            var any = status.Systems > 0;

            _check.Style = size.Check;
            _check.Base.Tint = look.Ink;
            _check.IsVisible = any;
            _underCheck.Placement = FixedSize.Height(size.UnderMark);
            _underCheck.IsVisible = any;

            _says.Style = any ? size.Name : size.Sentence;
            _says.Base.Color = any ? look.Ink : look.Quiet;
            _says.Text = any ? look.Texts.PanelAllNormal : look.Texts.PanelNothing;
            _underSays.Placement = FixedSize.Height(size.UnderLabel);
            _underSays.IsVisible = true;

            _count.Style = size.Label;
            _count.Base.Color = look.Quiet;
            _count.Text = status.Systems == 1
                ? look.Texts.PanelSystemsOne
                : Template.Fill(look.Texts.PanelSystems, "count", status.Systems.ToString(CultureInfo.InvariantCulture));
            _count.IsVisible = any;

            _fix.Style = size.Label;
            _fix.Base.Color = look.Quiet;
            _fix.Text = look.Texts.PanelNothingFix;
            _fix.IsVisible = !any;

            if (StatusLook.Tall(graphics, this, wide) > high)
                _count.IsVisible = _fix.IsVisible = _underSays.IsVisible = false;
            if (StatusLook.Tall(graphics, this, wide) > high)
                _check.IsVisible = _underCheck.IsVisible = false;
            if (StatusLook.Tall(graphics, this, wide) > high)
                _says.Style = size.Sentence;

            return StatusLook.Tall(graphics, this, wide) <= high;
        }
    }
}
