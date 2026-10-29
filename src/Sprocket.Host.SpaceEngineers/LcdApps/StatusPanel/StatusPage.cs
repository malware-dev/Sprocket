using System.Collections.Generic;
using System.Globalization;
using Lumel;
using Lumel.Drawing;
using Lumel.Images;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.LcdApps.StatusPanel
{
    /// <summary>The status panel on a screen tall enough for a top line and cards.</summary>
    public partial class StatusPage : VStack
    {
        readonly List<StatusCard> _cards = new List<StatusCard>();
        readonly List<Glyph> _dotted = new List<Glyph>();
        readonly List<int> _pages = new List<int>();

        bool _whole;

        /// <summary>A page showing nothing yet.</summary>
        public StatusPage()
        {
            CreateView();
        }

        /// <summary>How many pages the screen last laid out takes turns between.</summary>
        public int Pages { get; private set; } = 1;

        /// <summary>
        /// Lays <paramref name="status" /> out in <paramref name="look" /> on a screen <paramref name="room" /> big, kept
        /// <paramref name="inset" /> pixels in from its edges, showing the page <paramref name="turn" /> places in, with the
        /// page <paramref name="from" /> places in beside it to slide from where the two differ.
        /// </summary>
        public void Lay(IGraphics graphics, StatusLook look, ConstructStatus status, Vector2 room, float inset, int turn, int from)
        {
            var size = look.Size;
            var wide = room.X - inset * 2f;
            var high = room.Y - inset * 2f;
            Base.Padding = new Thickness(inset);
            Placement = FixedSize.Size(room.X, room.Y);
            Base.Spacing = size.Between;
            Pages = 1;

            Top(graphics, look, status, wide);
            _dots.IsVisible = false;
            _pageOf.IsVisible = false;

            var under = high - size.Icon - Spacing;
            if (status.Heard && status.Troubled)
                Troubled(graphics, look, status, under - Spacing - size.Small, wide, inset, turn, from);
            else if (status.Heard)
            {
                // The top line goes last, where the calm screen does not fit under it even at its smallest.
                Middle(_calm, false);
                _top.IsVisible = _calm.Show(graphics, look, status, under, wide);
                if (!_top.IsVisible)
                    _calm.Show(graphics, look, status, high, wide);
            }
            else
                Middle(null, false);
        }

        /// <summary>The icon and the heading, with the counts beside them, or the construct's name where it fits.</summary>
        void Top(IGraphics graphics, StatusLook look, ConstructStatus status, float wide)
        {
            var size = look.Size;
            var troubled = status.Heard && status.Troubled;
            _top.IsVisible = true;
            _top.Base.Spacing = size.Between;
            _icon.Placement = FixedSize.Size(size.Icon, size.Icon);
            _heading.Style = size.Plain;
            _heading.Base.Color = look.Quiet;
            _heading.Text = look.Texts.PanelHeading;
            _problems.Show(look, size, status.Problems, false, troubled);
            _warnings.Show(look, size, status.Warnings, false, troubled);

            _construct.Style = size.Plain;
            _construct.Base.Color = look.Quiet;
            _construct.Text = status.Construct;
            _construct.IsVisible = !troubled && status.Construct.Length > 0
                && size.Icon + size.Between * 2f + look.Width(graphics, _heading.Text, size.Small) + look.Width(graphics, status.Construct, size.Small) <= wide;
        }

        /// <summary>
        /// The page <paramref name="turn" /> places in, of as many whole cards as fit <paramref name="high" />, each in an
        /// equal share of its column, with the page <paramref name="from" /> places in beside it where the two differ, far
        /// enough apart that each leaves the screen past <paramref name="inset" /> as the other fills it.
        /// </summary>
        void Troubled(IGraphics graphics, StatusLook look, ConstructStatus status, float high, float wide, float inset, int turn, int from)
        {
            var size = look.Size;
            var items = status.Items;
            var two = wide >= high * 1.6f && wide / size.Body >= 24f;
            var column = two ? (wide - size.Big) / 2f : wide;
            var columns = two ? 2 : 1;

            while (_cards.Count < items.Count)
                _cards.Add(new StatusCard());

            // Whole cards, filling a column, then the next, then a page: a column takes another card only where every card on
            // it keeps as much room again around it, so none is crowded.
            _pages.Clear();
            _pages.Add(0);
            var filled = 0f;
            var full = 0;
            for (var i = 0; i < items.Count; i++)
            {
                var card = _cards[i];
                card.Show(look, status, items[i]);
                card.Fit(graphics, column, high);
                var tall = StatusLook.Tall(graphics, card.Body, column);
                if (filled > 0f && filled + tall * 2f > high)
                {
                    filled = 0f;
                    full++;
                    if (full == columns)
                    {
                        _pages.Add(i);
                        full = 0;
                    }
                }

                filled += filled > 0f ? tall * 2f : tall;
            }

            Pages = _pages.Count;
            var at = Page(turn);
            var before = Page(from);

            _leaving.Empty();
            _coming.Empty();
            _coming.Fill(graphics, _cards, _pages[at], End(at, items.Count), two, size.Big, column, high);
            _leaving.IsVisible = before != at;
            if (_leaving.IsVisible)
                _leaving.Fill(graphics, _cards, _pages[before], End(before, items.Count), two, size.Big, column, high);

            _slide.Travel = wide + inset;
            Middle(_slide, true);
            Dots(look, at, wide);
        }

        /// <summary>Shows the pages as far slid as <paramref name="progress" /> says, from 0, the page before in place, to 1, the page shown.</summary>
        public void Slid(float progress)
        {
            _slide.Progress = progress;
        }

        /// <summary>Which page <paramref name="turn" /> places in.</summary>
        int Page(int turn)
        {
            return (turn % Pages + Pages) % Pages;
        }

        /// <summary>Where the page <paramref name="at" /> ends, of <paramref name="count" /> cards.</summary>
        int End(int at, int count)
        {
            return at + 1 < Pages ? _pages[at + 1] : count;
        }

        /// <summary>A dot for each page, the one at <paramref name="at" /> filled, or which it is in words where they do not fit <paramref name="wide" />.</summary>
        void Dots(StatusLook look, int at, float wide)
        {
            if (Pages < 2)
                return;

            var size = look.Size;
            if (Pages * (size.Dot + size.Between) > wide)
            {
                _pageOf.Style = size.Plain;
                _pageOf.Base.Color = look.Quiet;
                _pageOf.Text = Template.Fill(Template.Fill(look.Texts.PanelPage, "at", (at + 1).ToString(CultureInfo.InvariantCulture)), "count",
                    Pages.ToString(CultureInfo.InvariantCulture));
                _pageOf.IsVisible = true;
                return;
            }

            while (_dotted.Count < Pages)
                _dotted.Add(new Glyph());

            _dots.Base.Spacing = size.Between;
            _dots.Children.Clear();
            for (var i = 0; i < Pages; i++)
            {
                var dot = _dotted[i];
                dot.Style = size.Dots;
                dot.Symbol = i == at ? Symbols.Disc : Symbols.Ring;
                dot.Base.Tint = i == at ? look.Ink : look.Quiet;
                _dots.Children.Add(dot);
            }

            _dots.IsVisible = true;
        }

        /// <summary>Puts <paramref name="shown" /> in the middle, over its whole height where <paramref name="whole" /> and a little above its middle otherwise, in place of what was there.</summary>
        void Middle(Element shown, bool whole)
        {
            if (_whole)
                _middle.Cells[0, 0, 3, 1] = null;
            else
                _middle.Cells[1, 0] = null;

            _whole = whole;
            if (shown == null)
                return;

            if (whole)
                _middle.Cells[0, 0, 3, 1] = shown;
            else
                _middle.Cells[1, 0] = shown;
        }
    }
}
