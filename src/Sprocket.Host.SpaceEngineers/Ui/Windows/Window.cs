using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui.Windows
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

        /// <summary>What a modal window paints over everything beneath it.</summary>
        public Color Backdrop { get; set; }

        /// <summary>Where the window stands in <paramref name="area" /> at <paramref name="size" />: in the middle, unless a kind of window says otherwise.</summary>
        public virtual RectangleF Place(RectangleF area, Vector2 size)
        {
            return new RectangleF(area.X + (area.Width - size.X) * 0.5f, area.Y + (area.Height - size.Y) * 0.5f, size.X,
                size.Y);
        }
    }
}
