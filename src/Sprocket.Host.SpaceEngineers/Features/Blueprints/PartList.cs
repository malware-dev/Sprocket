using Lumel.Drawing;
using Lumel.Layout;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Features.Blueprints
{
    /// <summary>A blueprint's slots, one under the next, with every slot's name in one column as wide as the widest of them needs.</summary>
    /// <remarks>
    /// A description counts towards the column's width up to <see cref="Widest" />, and wraps past it. A slot being written
    /// takes the whole of <see cref="Widest" />, for its boxes.
    /// </remarks>
    public class PartList : VStack
    {
        /// <summary>The widest the column gets.</summary>
        public const float Widest = 560f;

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            var wide = 0f;
            for (var i = 0; i < Children.Count; i++)
            {
                var row = Children[i] as PartRow;
                if (row == null || !row.IsVisible)
                    continue;

                var asks = row.Editing ? Widest : row.Asks(g, new Vector2(Widest, available.Y));
                if (asks > wide)
                    wide = asks;
            }

            for (var i = 0; i < Children.Count; i++)
            {
                var row = Children[i] as PartRow;
                if (row != null)
                    row.Column = wide;
            }

            return base.MeasureContent(g, available);
        }
    }
}
