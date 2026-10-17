using Lumel.Drawing;
using VRageMath;

namespace Lumel.Controls
{
    /// <summary>A button striped like a hazard marking, its words on a plate in the middle: one that is meant to stand out.</summary>
    public class HazardButton : Button
    {
        /// <summary>The ground the stripes run across.</summary>
        public Color Ground { get; set; } = new Color(255, 190, 0);

        /// <summary>The stripes.</summary>
        public Color Stripe { get; set; } = new Color(18, 18, 18);

        /// <summary>How wide each stripe is.</summary>
        public float StripeWidth { get; set; } = 8f;

        /// <summary>How far one stripe is from the next.</summary>
        public float Period { get; set; } = 18f;

        /// <summary>How much of the stripes shows around the plate, top and bottom.</summary>
        public float Border { get; set; } = 5f;

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            var bounds = Bounds;
            g.PushClip(bounds);
            g.Fill(bounds, Ground);

            // Stripes at forty-five degrees, long enough to cross the whole height at any place along the button.
            var tall = bounds.Height * 1.8f;
            for (var x = bounds.X - bounds.Height; x < bounds.X + bounds.Width + bounds.Height; x += Period)
                g.Fill(new RectangleF(x, bounds.Y + (bounds.Height - tall) * 0.5f, StripeWidth, tall), Stripe, MathHelper.PiOver4);

            var pad = Padding;
            var left = bounds.X + pad.Left - 8f;
            var width = bounds.Width - pad.Horizontal + 16f;
            g.Fill(new RectangleF(left, bounds.Y + Border, width, bounds.Height - 2f * Border), Background);
            g.PopClip();

            base.DrawContent(g);
        }
    }
}
