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

        /// <summary>As much of the area as there is on the side of the anchor with more of it, so what hangs open never runs off the area.</summary>
        public override Vector2 Room(RectangleF area)
        {
            if (Anchor == null)
                return base.Room(area);

            var from = Anchor.Bounds;
            if (Side == PopupSide.Beside)
            {
                var right = area.X + area.Width - (from.X + from.Width + Gap);
                var left = from.X - Gap - area.X;
                var wide = right > left ? right : left;

                return new Vector2(wide > 0f ? wide : 0f, area.Height);
            }

            var below = area.Y + area.Height - (from.Y + from.Height + Gap);
            var above = from.Y - Gap - area.Y;
            var tall = below > above ? below : above;

            return new Vector2(area.Width, tall > 0f ? tall : 0f);
        }

        /// <summary>On its side of the anchor, or the opposite side where the area has no room for it there, and kept inside the area either way; below a line, as wide as the line where it is <see cref="Flush" />.</summary>
        public override RectangleF Place(RectangleF area, Vector2 size)
        {
            if (Anchor == null)
                return base.Place(area, size);

            var from = Anchor.Bounds;
            float x;
            float y;

            if (Side == PopupSide.Beside)
            {
                x = from.X + from.Width + Gap;
                if (x + size.X > area.X + area.Width)
                    x = from.X - Gap - size.X;

                y = from.Y + from.Height * 0.5f - Lead();
            }
            else
            {
                if (Flush && size.X < from.Width)
                    size.X = from.Width;

                x = from.X;
                y = from.Y + from.Height + Gap;
                if (y + size.Y > area.Y + area.Height)
                    y = from.Y - Gap - size.Y;
            }

            if (y + size.Y > area.Y + area.Height)
                y = area.Y + area.Height - size.Y;

            if (y < area.Y)
                y = area.Y;

            if (x + size.X > area.X + area.Width)
                x = area.X + area.Width - size.X;

            if (x < area.X)
                x = area.X;

            return new RectangleF(x, y, size.X, size.Y);
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
