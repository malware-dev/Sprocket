using Sprocket.Host.SpaceEngineers.Ui.Drawing;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui.Controls
{
    /// <summary>A hairline across a block, with room around it.</summary>
    /// <remarks>
    ///     The line is drawn rather than set as the background, which would fill the padding too and turn
    ///     the rule into a band.
    /// </remarks>
    public class Rule : Element, IPadded
    {
        Thickness _padding;
        /// <summary>Makes a line <paramref name="thickness" /> thick, in <paramref name="color" />.</summary>
        public Rule(float thickness, Color color)
        {
            Thickness = thickness;
            Color = color;
        }

        /// <summary>How thick the line is.</summary>
        public float Thickness { get; set; }

        /// <summary>The colour of the line.</summary>
        public Color Color { get; set; }

        /// <summary>Room held clear between the edge and the line.</summary>
        public Thickness Padding
        {
            get { return _padding; }
            set
            {
                if (_padding == value)
                    return;

                _padding = value;
                InvalidateMeasure();
            }
        }

        /// <inheritdoc />
        protected override Thickness Inset => _padding;

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            return new Vector2(available.X, Thickness);
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            g.Fill(
                new RectangleF(ContentBounds.X, ContentBounds.Y, ContentBounds.Width, Thickness),
                Color);
        }
    }
}