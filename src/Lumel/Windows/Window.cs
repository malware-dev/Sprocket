using Lumel.Drawing;
using VRageMath;

namespace Lumel.Windows
{
    /// <summary>Something that floats over the screen in a window layer, holding one element and placed by the layer rather than by a container.</summary>
    /// <remarks>
    ///     A modal window takes all input for as long as it is open: nothing beneath it answers, and the layer
    ///     paints <see cref="Backdrop" /> over everything beneath it first.
    /// </remarks>
    public class Window : ContentControl
    {
        /// <summary>Whether nothing beneath the window takes input while it is open.</summary>
        public bool IsModal { get; set; }

        /// <summary>Whether it is open in a layer.</summary>
        public bool IsOpen => Layer != null;

        /// <summary>What a modal window paints over everything beneath it.</summary>
        public Color Backdrop { get; set; }

        /// <summary>What it casts on what lies beneath it, or null for nothing.</summary>
        public Shadow Shadow { get; set; }

        /// <summary>Takes it away from the layer it is open in, if it is open.</summary>
        public void Close()
        {
            var layer = Layer;
            if (layer != null)
                layer.Close(this);
        }

        /// <summary>The room <paramref name="area" /> gives the window to be measured in: all of it, unless a kind of window says otherwise.</summary>
        public virtual Vector2 Room(RectangleF area)
        {
            return new Vector2(area.Width, area.Height);
        }

        /// <summary>Draws whatever lies under the window and outside it before the window itself: its <see cref="Shadow" />, unless a kind of window says otherwise.</summary>
        public virtual void DrawBehind(IGraphics g)
        {
            var shadow = Shadow;
            if (shadow != null)
                shadow.Draw(g, Bounds);
        }

        /// <summary>Where the window stands in <paramref name="area" /> at <paramref name="size" />: in the middle, unless a kind of window says otherwise.</summary>
        public virtual RectangleF Place(RectangleF area, Vector2 size)
        {
            return new RectangleF(area.X + (area.Width - size.X) * 0.5f, area.Y + (area.Height - size.Y) * 0.5f, size.X,
                size.Y);
        }
    }
}
