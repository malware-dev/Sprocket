using System.Globalization;
using Lumel;
using Lumel.Drawing;
using Lumel.Layout;
using Sprocket.Host.SpaceEngineers.Features.Look;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.LcdApps.StatusPanel
{
    /// <summary>The status panel on a screen too short for cards: the icon and the heading in one half, and the counts, or the check and that all systems are nominal, in the other.</summary>
    public partial class StatusBar : VStack
    {
        /// <summary>A bar showing nothing yet.</summary>
        public StatusBar()
        {
            CreateView();
        }

        /// <summary>Lays <paramref name="status" /> out in <paramref name="look" /> across a screen <paramref name="room" /> big, kept <paramref name="edge" /> pixels in from its edges.</summary>
        public void Lay(IGraphics graphics, StatusLook look, ConstructStatus status, Vector2 room, float edge)
        {
            var inset = edge + 4f;
            var half = (room.X - inset * 2f) / 2f;
            var troubled = status.Heard && status.Troubled;
            var says = status.Systems > 0 ? look.Texts.PanelAllNormal : look.Texts.PanelNothing;

            Base.Padding = new Thickness(inset);
            Placement = FixedSize.Size(room.X, room.Y);

            var largest = look.Banner(room.Y - inset * 2f);
            var size = largest;
            for (var i = PanelSize.All.Count - 1; i >= 0; i--)
            {
                var one = PanelSize.All[i];
                if (one.Body > largest.Body || one.Body < look.Floor)
                    continue;

                size = one;
                if (Left(graphics, look, one, true) <= half && Right(graphics, look, status, troubled, says, one) <= half)
                    break;
            }

            _name.Spacing = size.Between;
            _icon.Placement = FixedSize.Size(size.BannerIcon, size.BannerIcon);
            _icon.IsVisible = Left(graphics, look, size, false) <= half;
            _heading.Style = size.Banner;
            _heading.Base.Color = look.Ink;
            _heading.Text = look.Texts.PanelHeading;
            _heading.IsVisible = Left(graphics, look, size, true) <= half;

            _right.Spacing = size.Between;
            _problems.Show(look, size, status.Problems, true, troubled);
            _warnings.Show(look, size, status.Warnings, true, troubled);
            _check.Style = size.BannerMark;
            _check.Base.Tint = look.Ink;
            _check.IsVisible = status.Heard && !troubled && status.Systems > 0;
            _says.Style = size.Banner;
            _says.Base.Color = status.Systems > 0 ? look.Ink : look.Quiet;
            _says.Text = says;
            _says.IsVisible = status.Heard && !troubled && Right(graphics, look, status, false, says, size) <= half;
        }

        /// <summary>How wide the heading's half is at <paramref name="size" />: the icon, and its word where <paramref name="word" />.</summary>
        static float Left(IGraphics graphics, StatusLook look, PanelSize size, bool word)
        {
            return size.BannerIcon + (word ? size.Between + look.Width(graphics, look.Texts.PanelHeading, size.Body) : 0f) + size.Body;
        }

        /// <summary>How wide the other half is at <paramref name="size" />: the counts, or the check and <paramref name="says" />.</summary>
        static float Right(IGraphics graphics, StatusLook look, ConstructStatus status, bool troubled, string says, PanelSize size)
        {
            if (!status.Heard)
                return 0f;

            if (!troubled)
                return status.Systems > 0 ? size.Body + size.Between + look.Width(graphics, says, size.Body) : look.Width(graphics, says, size.Body);

            var wide = 0f;
            if (status.Problems > 0)
                wide += size.Body + size.Beside + look.Width(graphics, status.Problems.ToString(CultureInfo.InvariantCulture), size.Body);
            if (status.Warnings > 0)
                wide += (wide > 0f ? size.Between : 0f) + size.Body + size.Beside
                    + look.Width(graphics, status.Warnings.ToString(CultureInfo.InvariantCulture), size.Body);

            return wide;
        }
    }
}
