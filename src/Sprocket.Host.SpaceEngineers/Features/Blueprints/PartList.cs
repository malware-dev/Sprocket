using System.Collections.Generic;
using Lumel.Controls;
using Lumel.Drawing;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Sequences;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Features.Blueprints
{
    /// <summary>
    /// A blueprint's slots, one under the next in the system's order, each run of slots under one category headed by its name,
    /// with every slot's name in one column as wide as the widest of them needs.
    /// </summary>
    /// <remarks>Where the slots can be arranged, a slot is carried by its grip and a category by its own, with its slots.</remarks>
    public class PartList : ReorderList
    {
        /// <summary>The widest the column gets.</summary>
        public const float Widest = 560f;

        /// <summary>
        /// Puts into <paramref name="parts" /> every slot of <paramref name="system" />, in its order, and into
        /// <paramref name="rows" /> the same slots with a <see cref="CategoryHeading" /> before each run of them under one category;
        /// a category with no slot is headed too, where it stands, where <paramref name="empties" />.
        /// </summary>
        public static void Listed(AutomationSystem system, List<Part> parts, List<object> rows, bool empties)
        {
            parts.Clear();
            rows.Clear();
            var all = system.Parts;
            var previous = "";
            for (var i = 0; i < all.Count; i++)
            {
                var part = all[i];
                var starts = part.Category.Length > 0 && part.Category != previous;
                if (empties && (starts || part.Category.Length == 0))
                    Empty(system, part, rows);

                if (starts)
                    rows.Add(CategoryHeading.Over(part));

                parts.Add(part);
                rows.Add(part);
                previous = part.Category;
            }

            if (!empties)
                return;

            // Under every slot: those standing there, and any whose slot is gone or now stands inside a run.
            foreach (var empty in system.EmptyCategories)
            {
                if (!rows.Contains(CategoryHeading.Of(empty)))
                    rows.Add(CategoryHeading.Of(empty));
            }
        }

        /// <summary>Adds to <paramref name="rows" /> the heading of every empty category of <paramref name="system" /> standing above <paramref name="part" />.</summary>
        static void Empty(AutomationSystem system, Part part, List<object> rows)
        {
            foreach (var empty in system.EmptyCategories)
            {
                if (empty.Above == part)
                    rows.Add(CategoryHeading.Of(empty));
            }
        }

        /// <summary>Draws a spine down from each category's grip to the middle of its last slot, of the rows standing in the list or of those being dragged.</summary>
        protected override void DrawBeside(IGraphics g, bool dragged)
        {
            for (var i = 0; i < Children.Count; i++)
            {
                var heading = Children[i] as CategoryRow;
                if (heading == null || Dragged(i) != dragged)
                    continue;

                PartRow last = null;
                for (var j = i + 1; j < Children.Count; j++)
                {
                    var slot = Children[j] as PartRow;
                    if (slot == null || !slot.Indented || Dragged(j) != dragged)
                        break;

                    last = slot;
                }

                if (last == null)
                    continue;

                var grip = ((IReorderable)heading).DragHandle.Bounds;
                var x = grip.X + grip.Width * 0.5f;
                var top = heading.Bounds.Y + heading.Bounds.Height + heading.RenderTransform.Translate.Y;
                var middle = last.Bounds.Y + last.Bounds.Height * 0.5f + last.RenderTransform.Translate.Y;
                TreeLine.Turning(g, x, top, middle, Sizes.Spine, Sizes.SpineTurn, Palette.Line);
            }
        }

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            var wide = 0f;
            foreach (var child in Children)
            {
                var row = child as PartRow;
                if (row == null || !row.IsVisible)
                    continue;

                var asks = row.Asks(g, new Vector2(Widest, available.Y));
                if (asks > wide)
                    wide = asks;
            }

            foreach (var child in Children)
            {
                var row = child as PartRow;
                if (row != null)
                    row.Column = wide;
            }

            return base.MeasureContent(g, available);
        }
    }
}
