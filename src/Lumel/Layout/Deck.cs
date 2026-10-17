using System;
using Lumel.Drawing;
using VRageMath;

namespace Lumel.Layout
{
    /// <summary>Children in one place, one over another like a deck of cards, with only the one on top shown.</summary>
    /// <remarks>
    /// The deck is as large as its largest card, so turning to another card never changes its size. Only the card on top
    /// is drawn, pressed or reached by the keyboard.
    /// </remarks>
    public class Deck : Panel
    {
        Element _top;

        /// <summary>The card shown, one of the children; the first where none is chosen or the chosen one has left.</summary>
        public Element Top
        {
            get { return _top != null && _top.Parent == this ? _top : Children.Count > 0 ? Children[0] : null; }
            set
            {
                if (ReferenceEquals(_top, value))
                    return;

                _top = value;
                if (value != null)
                    value.Restyle();
            }
        }

        /// <inheritdoc />
        public override int VisualChildCount => Top == null ? 0 : 1;

        /// <summary>Each card stretches over the whole deck.</summary>
        protected override Align Aligns => Align.Fill;

        /// <inheritdoc />
        public override Element GetVisualChild(int index)
        {
            return Top;
        }

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            var size = Vector2.Zero;
            foreach (var child in Children)
            {
                var sized = child.Placement as ISized;
                child.Measure(g, new Vector2(Given(sized, true, available.X), Given(sized, false, available.Y)));
                size.X = Math.Max(size.X, Given(sized, true, child.DesiredSize.X));
                size.Y = Math.Max(size.Y, Given(sized, false, child.DesiredSize.Y));
            }

            return size;
        }

        /// <inheritdoc />
        protected override void ArrangeContent(RectangleF bounds)
        {
            foreach (var child in Children)
            {
                var sized = child.Placement as ISized;
                float x;
                float width;
                float y;
                float height;
                Span(child, true, bounds.X, bounds.Width, sized?.Width ?? float.NaN, child.DesiredSize.X, out x, out width);
                Span(child, false, bounds.Y, bounds.Height, sized?.Height ?? float.NaN, child.DesiredSize.Y, out y, out height);
                child.Arrange(new RectangleF(x, y, width, height));
            }
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            var top = Top;
            if (top != null)
                top.Draw(g);
        }

        /// <inheritdoc />
        protected override Element UnderContent(Vector2 point)
        {
            var top = Top;
            return top?.Under(point);
        }

        /// <summary>Refuses a child whose placement is not <see cref="ISized" />.</summary>
        protected override void Admit(Element child, IPlacement placement)
        {
            if (placement != null && !(placement is ISized))
                throw new InvalidOperationException(GetType().Name + " cannot place a child by " + placement.GetType().Name + ".");
        }

        /// <summary>The length the placement fixes on one axis, or <paramref name="otherwise" /> where it fixes none.</summary>
        static float Given(ISized sized, bool across, float otherwise)
        {
            if (sized == null)
                return otherwise;

            var given = across ? sized.Width : sized.Height;
            return float.IsNaN(given) ? otherwise : given;
        }
    }
}
