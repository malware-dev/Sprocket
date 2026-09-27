using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui.Windows
{
    /// <summary>A window hanging from an element: under it where there is room, over it where there is not, and closed by a press anywhere else.</summary>
    /// <remarks>A press that closes it goes no further, so reaching past an open list is two presses rather than one that does two things.</remarks>
    public class Popup : Window
    {
        /// <summary>What it hangs from, or null to stand in the middle as any window does.</summary>
        public Element Anchor { get; set; }

        /// <summary>How far it hangs from what it hangs from.</summary>
        public float Gap { get; set; }

        /// <summary>As much of the area as there is on the side of the anchor with more of it, so what hangs open never runs off the area.</summary>
        public override Vector2 Room(RectangleF area)
        {
            if (Anchor == null)
                return base.Room(area);

            var from = Anchor.Bounds;
            var below = area.Y + area.Height - (from.Y + from.Height + Gap);
            var above = from.Y - Gap - area.Y;
            var tall = below > above ? below : above;

            return new Vector2(area.Width, tall > 0f ? tall : 0f);
        }

        /// <summary>Under the anchor, or over it where the area has no room below, and kept inside the area either way.</summary>
        public override RectangleF Place(RectangleF area, Vector2 size)
        {
            if (Anchor == null)
                return base.Place(area, size);

            var from = Anchor.Bounds;
            var x = from.X;
            var y = from.Y + from.Height + Gap;

            if (y + size.Y > area.Y + area.Height)
                y = from.Y - Gap - size.Y;

            if (y < area.Y)
                y = area.Y;

            if (x + size.X > area.X + area.Width)
                x = area.X + area.Width - size.X;

            if (x < area.X)
                x = area.X;

            return new RectangleF(x, y, size.X, size.Y);
        }
    }
}
