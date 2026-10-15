using Lumel.Drawing;
using VRageMath;

namespace Lumel.Layout
{
    /// <summary>Children in one place, each drawn over the one before: a deck with every card face up.</summary>
    /// <remarks>
    /// Sized as a deck is, by its largest child, and each child placed in the whole of it by its own <see cref="Element.Align" />,
    /// so a small one pinned to a corner lies over the large one under it. A press reaches the uppermost child under it.
    /// </remarks>
    public class Layers : Deck
    {
        /// <inheritdoc />
        public override int VisualChildCount => Children.Count;

        /// <inheritdoc />
        public override Element GetVisualChild(int index)
        {
            return Children[index];
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            for (var i = 0; i < Children.Count; i++)
                Children[i].Draw(g);
        }

        /// <inheritdoc />
        protected override Element UnderContent(Vector2 point)
        {
            for (var i = Children.Count - 1; i >= 0; i--)
            {
                var under = Children[i].Under(point);
                if (under != null)
                    return under;
            }

            return null;
        }
    }
}
