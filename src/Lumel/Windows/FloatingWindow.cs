using System;
using VRageMath;

namespace Lumel.Windows
{
    /// <summary>A window that stays open beside what it floats over, where the player has moved it.</summary>
    /// <remarks>
    ///     It keeps the place it was moved to, in UI units, and is put back on the screen whenever that place would leave any
    ///     of it off: a remembered place outlives a change of screen, and a screen of another shape can be narrower.
    /// </remarks>
    public class FloatingWindow : Window
    {
        /// <summary>Raised when a move by its <see cref="TitleBar" /> ends, with <see cref="Position" /> where it was left.</summary>
        public event Action Moved;

        /// <summary>Where the window's top-left corner was moved to, or null where it stands where <see cref="Home" /> puts it.</summary>
        public Vector2? Position { get; set; }

        /// <summary>The whole screen, which the window is kept inside, or empty to keep it inside the area it is placed in; the layer it is open in sets it.</summary>
        public RectangleF Screen { get; set; }

        /// <inheritdoc />
        /// <remarks>Where it was moved to, or where <see cref="Home" /> puts it, and then back inside the screen.</remarks>
        public override RectangleF Place(RectangleF area, Vector2 size)
        {
            var at = Position ?? Home(area, size);
            var screen = Screen.Width > 0f && Screen.Height > 0f ? Screen : area;

            if (at.X + size.X > screen.X + screen.Width)
                at.X = screen.X + screen.Width - size.X;

            if (at.Y + size.Y > screen.Y + screen.Height)
                at.Y = screen.Y + screen.Height - size.Y;

            if (at.X < screen.X)
                at.X = screen.X;

            if (at.Y < screen.Y)
                at.Y = screen.Y;

            return new RectangleF(at.X, at.Y, size.X, size.Y);
        }

        /// <summary>Says a move has ended where the window now stands, its <see cref="Position" /> first brought back inside the screen.</summary>
        public void Settle()
        {
            if (Position.HasValue && Screen.Width > 0f && Screen.Height > 0f)
            {
                var kept = Place(Screen, new Vector2(Bounds.Width, Bounds.Height));
                Position = new Vector2(kept.X, kept.Y);
            }

            var moved = Moved;
            if (moved != null)
                moved();
        }

        /// <summary>Where the window's top-left corner stands in <paramref name="area" /> before it has been moved: in the middle, unless a kind of floating window says otherwise.</summary>
        protected virtual Vector2 Home(RectangleF area, Vector2 size)
        {
            return new Vector2(area.X + (area.Width - size.X) * 0.5f, area.Y + (area.Height - size.Y) * 0.5f);
        }
    }
}
