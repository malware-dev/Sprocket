using VRageMath;
using Reactor.Host.SpaceEngineers.Drawing;
using Reactor.Host.SpaceEngineers.Layout;

namespace Reactor.Host.SpaceEngineers.Controls
{
    /// <summary>A hairline across a block, with room around it.</summary>
    /// <remarks>
    /// The line is drawn rather than being the element's background, so that padding puts space around it
    /// instead of thickening it. The layout has no margins - padding is the only spacing there is - and a
    /// background fills the whole box including that padding, which turns a one-pixel rule into a band.
    /// </remarks>
    public class Rule : Element
    {
        public Rule(float thickness, Color color)
        {
            Thickness = thickness;
            Color = color;
        }

        public float Thickness { get; set; }

        public Color Color { get; set; }

        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            return new Vector2(available.X, Thickness);
        }

        protected override void DrawContent(IGraphics g)
        {
            g.DrawSprite(Solid,
                new RectangleF(ContentBounds.X, ContentBounds.Y, ContentBounds.Width, Thickness),
                Color, 0f);
        }
    }
}
