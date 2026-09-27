using System;
using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Drawing;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Features.Sequences
{
    /// <summary>The handle a row is dragged by: six small squares, two across and three down.</summary>
    /// <remarks>
    /// A handle rather than the whole row, because the row is full of parts to press and "press anywhere that is not
    /// one" is a rule nobody can see. It takes the pointer itself while it can be carried, and says where it went.
    /// </remarks>
    public class Grip : Element
    {
        const int Across = 2;
        const int Down = 3;
        const float Square = 3f;
        const float Gap = 3f;

        /// <summary>A grip that cannot be carried yet, drawn in white.</summary>
        public Grip()
        {
            Color = Color.White;
        }

        /// <summary>Raised with where the pointer is when the grip is taken hold of.</summary>
        public event Action<Vector2> Taken;

        /// <summary>Raised with where the pointer is as the grip is carried.</summary>
        public event Action<Vector2> Carried;

        /// <summary>Raised with where the pointer is when the grip is let go of.</summary>
        public event Action<Vector2> Dropped;

        /// <summary>The color of its squares.</summary>
        public Color Color { get; set; }

        /// <summary>Whether it can be taken hold of now.</summary>
        public bool CanCarry { get; set; }

        /// <inheritdoc />
        protected override bool TakesPointer => CanCarry;

        /// <inheritdoc />
        public override void OnPointerDown(Vector2 point, int presses)
        {
            var taken = Taken;
            if (taken != null)
                taken(point);
        }

        /// <inheritdoc />
        public override void OnPointerMove(Vector2 point)
        {
            var carried = Carried;
            if (carried != null)
                carried(point);
        }

        /// <inheritdoc />
        public override void OnPointerUp(Vector2 point)
        {
            var dropped = Dropped;
            if (dropped != null)
                dropped(point);
        }

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            return new Vector2(Across * Square + (Across - 1) * Gap, Down * Square + (Down - 1) * Gap);
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            var width = Across * Square + (Across - 1) * Gap;
            var height = Down * Square + (Down - 1) * Gap;

            var left = ContentBounds.X + (ContentBounds.Width - width) * 0.5f;
            var top = ContentBounds.Y + (ContentBounds.Height - height) * 0.5f;

            for (var x = 0; x < Across; x++)
            for (var y = 0; y < Down; y++)
                g.Fill(new RectangleF(left + x * (Square + Gap), top + y * (Square + Gap), Square, Square), Color);
        }
    }
}
