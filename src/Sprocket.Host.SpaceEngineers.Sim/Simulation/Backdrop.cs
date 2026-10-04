using System.Windows;
using System.Windows.Media;

namespace Sprocket.Sim.Simulation
{
    /// <summary>
    /// Something for the screen to sit in front of.
    /// </summary>
    /// <remarks>
    /// Scaffolding, and not a picture of anything. It exists so the screen has edges: on a flat colour
    /// there is no telling where Sprocket stops and the window begins, which is exactly the judgement
    /// somebody is looking at a screenshot to make.
    /// <para>
    /// It is <b>not</b> a claim about what is really behind. We are working from the assumption that
    /// Keen hands us a real screen, which means the game paints its own background and dims what is
    /// beyond it by the player's own setting. What this ought to draw, once the shape of that is known,
    /// is that frame with Sprocket inside it - a hull with our panel floating on it is left over from
    /// when the plan was to draw over live gameplay.
    /// </para>
    /// </remarks>
    public sealed class Backdrop : FrameworkElement
    {
        /// <inheritdoc />
        protected override void OnRender(DrawingContext context)
        {
            Paint(context, RenderSize);
        }

        /// <summary>Draws it into whatever is handed in - see <see cref="Surface.Paint" />.</summary>
        public void Paint(DrawingContext context, Size into)
        {
            double width = into.Width;
            double height = into.Height;
            if (width <= 0d || height <= 0d)
                return;

            var space = new LinearGradientBrush
            {
                StartPoint = new Point(0d, 0d),
                EndPoint = new Point(0d, 1d),
                GradientStops =
                {
                    new GradientStop(Color.FromRgb(9, 13, 20), 0d),
                    new GradientStop(Color.FromRgb(26, 36, 48), 0.62d),
                    new GradientStop(Color.FromRgb(64, 74, 82), 0.66d),
                    new GradientStop(Color.FromRgb(36, 40, 44), 1d)
                }
            };

            context.DrawRectangle(space, null, new Rect(0d, 0d, width, height));

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
                    context.DrawRectangle(plate, null, cell);

                context.DrawLine(seam, cell.TopLeft, cell.BottomLeft);
            }

            context.DrawLine(
                new Pen(new SolidColorBrush(Color.FromArgb(110, 210, 225, 240)), 1d),
                new Point(0d, top), new Point(width, top));
        }
    }
}
