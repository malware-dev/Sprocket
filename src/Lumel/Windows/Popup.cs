using VRageMath;

namespace Lumel.Windows
{
    /// <summary>A window hanging from an element, on the side of it <see cref="Side" /> says, and closed by a press anywhere else.</summary>
    /// <remarks>A press that closes it goes no further.</remarks>
    public class Popup : Window
    {
        /// <summary>What it hangs from, or null to stand in the middle as any window does.</summary>
        public Element Anchor { get; set; }

        /// <summary>How far it hangs from what it hangs from.</summary>
        public float Gap { get; set; }

        /// <summary>Which side of its anchor it opens on: below it unless told otherwise.</summary>
        public PopupSide Side { get; set; }

        /// <summary>Whether, hanging below its anchor, it is never narrower than the anchor, as a drop-down's list is never narrower than its line.</summary>
        public bool Flush { get; set; }

        /// <summary>Hangs it open over the screen or window its anchor is on; nothing happens while the anchor is on neither.</summary>
        public void Open()
        {
            var layer = Anchor?.Windows;
            layer?.Open(this);
        }

        /// <summary>Closes it where it is open, and opens it where it is not.</summary>
        public void Toggle()
        {
            if (IsOpen)
                Close();
            else
                Open();
        }

        /// <summary>
        /// Beside its anchor, as much of the area as there is on the side of it with more; above or below it, the whole height
        /// of the area, so it is measured as tall as it would be and <see cref="Place" /> decides where that fits.
        /// </summary>
        public override Vector2 Room(RectangleF area)
        {
            if (Anchor == null)
                return base.Room(area);

            if (Side != PopupSide.Beside)
                return new Vector2(area.Width, area.Height);

            var from = Anchor.Bounds;
            var right = area.X + area.Width - (from.X + from.Width + Gap);
            var left = from.X - Gap - area.X;
            var wide = right > left ? right : left;

            return new Vector2(wide > 0f ? wide : 0f, area.Height);
        }

        /// <summary>
        /// Below its anchor where all of it fits there, otherwise above it where all of it fits there, otherwise on the side with
        /// more room and no taller than that room; never over the anchor itself. Beside its anchor, on its right where it fits
        /// and its left otherwise, kept inside the area. Below a line, as wide as the line where it is <see cref="Flush" />.
        /// </summary>
        /// <remarks>Where it is cut shorter than it asks, what it holds scrolls only if that is a scroll view.</remarks>
        public override RectangleF Place(RectangleF area, Vector2 size)
        {
            if (Anchor == null)
                return base.Place(area, size);

            var from = Anchor.Bounds;
            if (Side == PopupSide.Beside)
                return Beside(area, from, size);

            if (Flush && size.X < from.Width)
                size.X = from.Width;

            var below = area.Y + area.Height - (from.Y + from.Height + Gap);
            var above = from.Y - Gap - area.Y;
            bool over;
            if (size.Y <= below)
                over = false;
            else if (size.Y <= above)
                over = true;
            else
            {
                over = above > below;
                var room = over ? above : below;
                size.Y = room > 0f ? room : 0f;
            }

            var y = over ? from.Y - Gap - size.Y : from.Y + from.Height + Gap;
            return new RectangleF(Across(area, from.X, size.X), y, size.X, size.Y);
        }

        /// <summary>Where it stands at <paramref name="size" /> beside <paramref name="from" />, kept inside <paramref name="area" />.</summary>
        RectangleF Beside(RectangleF area, RectangleF from, Vector2 size)
        {
            var x = from.X + from.Width + Gap;
            if (x + size.X > area.X + area.Width)
                x = from.X - Gap - size.X;

            var y = from.Y + from.Height * 0.5f - Lead();
            if (y + size.Y > area.Y + area.Height)
                y = area.Y + area.Height - size.Y;

            if (y < area.Y)
                y = area.Y;

            return new RectangleF(Across(area, x, size.X), y, size.X, size.Y);
        }

        /// <summary><paramref name="x" />, moved in where something <paramref name="wide" /> standing there would run off <paramref name="area" />.</summary>
        static float Across(RectangleF area, float x, float wide)
        {
            if (x + wide > area.X + area.Width)
                x = area.X + area.Width - wide;

            return x < area.X ? area.X : x;
        }

        /// <summary>How far down from its top the middle of its first entry stands, which is what is set level with the middle of the anchor.</summary>
        /// <remarks>The first thing in what it holds, where that holds several, and otherwise what it holds as a whole.</remarks>
        float Lead()
        {
            var content = Content;
            if (content == null)
                return 0f;

            var first = content.VisualChildCount > 0 ? content.GetVisualChild(0) : content;
            return Padding.Top + first.DesiredSize.Y * 0.5f;
        }
    }
}
