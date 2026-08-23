using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Reactor.Sim.Simulation
{
    /// <summary>
    /// Something for the screen to be drawn over.
    /// </summary>
    /// <remarks>
    /// Scaffolding, and not a picture of anything. The screen-space overlay is drawn over the game, so a
    /// simulator that put it on a flat colour would be judging one of its most important decisions -
    /// how much of the world shows through - against a backdrop that cannot show it. Any lit, uneven
    /// surface answers the question; this one is a hull and a horizon because those are two shapes to
    /// draw and the point is only that the veil has something to veil.
    /// </remarks>
    public sealed class Backdrop : Control
    {
        public override void Render(DrawingContext context)
        {
            double width = Bounds.Width;
            double height = Bounds.Height;
            if (width <= 0d || height <= 0d)
                return;

            var space = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0d, 0d, RelativeUnit.Relative),
                EndPoint = new RelativePoint(0d, 1d, RelativeUnit.Relative),
                GradientStops =
                {
                    new GradientStop(Color.FromRgb(9, 13, 20), 0d),
                    new GradientStop(Color.FromRgb(26, 36, 48), 0.62d),
                    new GradientStop(Color.FromRgb(64, 74, 82), 0.66d),
                    new GradientStop(Color.FromRgb(36, 40, 44), 1d)
                }
            };

            context.FillRectangle(space, new Rect(0d, 0d, width, height));

            // Hull plating below the horizon, lit from the left, so the veil has edges to soften and
            // not only a wash to darken.
            var plate = new SolidColorBrush(Color.FromArgb(38, 190, 205, 220));
            var seam = new Pen(new SolidColorBrush(Color.FromArgb(60, 10, 14, 18)), 1d);

            double top = height * 0.66d;
            double step = width / 9d;

            for (int i = 0; i < 9; i++)
            {
                var cell = new Rect(i * step, top, step, height - top);

                if ((i & 1) == 0)
                    context.FillRectangle(plate, cell);

                context.DrawLine(seam, cell.TopLeft, cell.BottomLeft);
            }

            context.DrawLine(
                new Pen(new SolidColorBrush(Color.FromArgb(110, 210, 225, 240)), 1d),
                new Point(0d, top), new Point(width, top));
        }
    }
}
