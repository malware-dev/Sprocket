using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Drawing;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Features.Developing
{
    /// <summary>A box holding one element at the size it asks for, centered, with the box's middle and the element's own box drawn over it.</summary>
    /// <remarks>
    /// A developer tool for seeing where a glyph lands. The element is centered the way a stack centers a child, so what
    /// is drawn here is what a screen draws; the crosshair is the box's exact middle, and the outline is the room the
    /// element asked for. Ink centered on the crosshair is placed right; ink off it inside a centered outline is the
    /// font's; an outline off the crosshair is the layout's.
    /// </remarks>
    public class Plumb : ContentControl
    {
        /// <summary>The color of the box's middle.</summary>
        public Color Guide { get; set; }

        /// <summary>The color of the outline of the element's own box.</summary>
        public Color Room { get; set; }

        /// <inheritdoc />
        protected override void ArrangeContent(RectangleF bounds)
        {
            var content = Content;
            if (content == null || !content.IsVisible)
                return;

            var size = content.DesiredSize;
            content.Arrange(new RectangleF(
                bounds.X + (bounds.Width - size.X) * 0.5f,
                bounds.Y + (bounds.Height - size.Y) * 0.5f,
                size.X,
                size.Y));
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            base.DrawContent(g);

            var content = Content;
            if (content != null && content.IsVisible && Room.A > 0)
            {
                var room = content.Bounds;
                g.Fill(new RectangleF(room.X, room.Y, room.Width, 1f), Room);
                g.Fill(new RectangleF(room.X, room.Y + room.Height - 1f, room.Width, 1f), Room);
                g.Fill(new RectangleF(room.X, room.Y, 1f, room.Height), Room);
                g.Fill(new RectangleF(room.X + room.Width - 1f, room.Y, 1f, room.Height), Room);
            }

            if (Guide.A == 0)
                return;

            var box = ContentBounds;
            var middle = box.Center;
            g.Fill(new RectangleF(box.X, middle.Y - 0.5f, box.Width, 1f), Guide);
            g.Fill(new RectangleF(middle.X - 0.5f, box.Y, 1f, box.Height), Guide);
        }
    }
}
