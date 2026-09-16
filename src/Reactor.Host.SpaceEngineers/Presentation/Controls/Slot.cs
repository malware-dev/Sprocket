using VRageMath;
using Reactor.Host.SpaceEngineers.Presentation.Graphics;
using Reactor.Host.SpaceEngineers.Presentation.Ui;
using Reactor.Host.SpaceEngineers.Presentation.Look;

namespace Reactor.Host.SpaceEngineers.Presentation.Controls
{
    /// <summary>
    /// A fillable part of a sentence.
    /// </summary>
    /// <remarks>
    /// The only thing on screen that changes what a sequence means, so nothing else may borrow this
    /// look. The line under it is what distinguishes it from a row at a glance, and it is drawn rather
    /// than being a border because the seam has no borders - a rectangle of colour is all either target
    /// can be asked for.
    /// </remarks>
    public class Slot : Label
    {
        public Slot()
        {
            Scale = Lettering.Body;
            Color = Palette.Ink;
            Background = Palette.SlotFill;
            Highlight = Palette.ControlHot;
            Padding = new Thickness(Space.ChipX, Space.ChipY);
            Underline = Palette.SlotEdge;

            // A slot is one thing named, and a wrapped one would stop reading as a single chip.
            Wraps = false;
        }

        /// <summary>The colour of the line along the bottom.</summary>
        public Color Underline { get; set; }

        protected override void DrawContent(IGraphics g)
        {
            const float thickness = 2f;

            g.DrawSprite(Solid,
                new RectangleF(Bounds.X, Bounds.Y + Bounds.Height - thickness, Bounds.Width, thickness),
                Underline, 0f);

            base.DrawContent(g);
        }
    }
}
