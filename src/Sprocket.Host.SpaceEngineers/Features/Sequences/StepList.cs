using System;
using Lumel.Controls;
using Lumel.Drawing;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Sequences
{
    /// <summary>A sequence's steps, one row each, any of which can be carried by its grip to another place in the list.</summary>
    /// <remarks>A row that opens an if carries the rows of the if with it, as one block, and every if draws a spine down its rows.</remarks>
    public class StepList : CarryList
    {
        /// <summary>An empty list.</summary>
        public StepList()
        {
            Spacing = 0f;
        }

        /// <summary>Draws the spines of the rows standing in the list, or of the rows being carried.</summary>
        protected override void DrawBeside(IGraphics g, bool carried)
        {
            var reach = Spacing * 0.5f;
            var floor = carried ? ((StepRow)Children[CarryingAt]).Depth : 0;
            StepRow above = null;
            var aboveBottom = 0f;

            for (var i = 0; i < Children.Count; i++)
            {
                if (Carried(i) != carried)
                    continue;

                var row = (StepRow)Children[i];
                var next = Next(i, carried);
                var shift = row.RenderTransform.Translate.Y;
                var top = row.Bounds.Y + shift - reach;
                var bottom = row.Bounds.Y + row.Bounds.Height + shift + reach;
                var through = next == null ? 0 : Math.Min(row.Depth, next.Depth);
                var joined = above == null ? 0 : Math.Min(above.Depth, row.Depth);

                for (var level = floor; level < row.Depth; level++)
                {
                    var x = row.SpineX(level);
                    var from = level < joined ? Math.Min(top, aboveBottom) : top;
                    if (level >= through)
                        Spine.Turning(g, x, from, row.SpineAt + shift, Sizes.Spine, Sizes.SpineTurn, Palette.Line);
                    else
                        Spine.Down(g, x, from, bottom, Sizes.Spine, Palette.Line);
                }

                above = row;
                aboveBottom = bottom;
            }
        }

        /// <summary>The next row after <paramref name="index" /> that is carried, or that is not, as <paramref name="carried" /> says; null for none.</summary>
        StepRow Next(int index, bool carried)
        {
            for (var i = index + 1; i < Children.Count; i++)
            {
                if (Carried(i) == carried)
                    return (StepRow)Children[i];
            }

            return null;
        }
    }
}
