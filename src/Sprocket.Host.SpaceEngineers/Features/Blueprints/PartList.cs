using System.Collections.Generic;
using Lumel.Controls;
using Lumel.Drawing;
using Sprocket.Host.SpaceEngineers.Sequences;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Features.Blueprints
{
    /// <summary>
    /// A blueprint's slots, one under the next, the ones under no category first and then each category's name over its own,
    /// with every slot's name in one column as wide as the widest of them needs.
    /// </summary>
    /// <remarks>Where the slots can be arranged, a slot is carried by its grip and a category by its own, with its slots.</remarks>
    public class PartList : CarryList
    {
        /// <summary>The widest the column gets.</summary>
        public const float Widest = 560f;

        /// <summary>
        /// Puts into <paramref name="parts" /> the slots of <paramref name="system" /> that <paramref name="used" /> names, as they
        /// are listed, and into <paramref name="rows" /> the same slots with the name of each category before its own; a category
        /// with no slot listed is named too where <paramref name="empties" />.
        /// </summary>
        /// <remarks>The slots stand in the order of the system, and the categories in theirs; a slot under no category the system has stands under none.</remarks>
        public static void Listed(AutomationSystem system, IList<string> used, List<Part> parts, List<object> rows, bool empties)
        {
            parts.Clear();
            rows.Clear();
            var all = system.Parts;
            var categories = system.Categories;
            for (var i = 0; i < all.Count; i++)
            {
                if (used.Contains(all[i].Id) && !categories.Contains(all[i].Category))
                    Add(all[i], parts, rows);
            }

            for (var c = 0; c < categories.Count; c++)
            {
                var heading = rows.Count;
                for (var i = 0; i < all.Count; i++)
                {
                    if (used.Contains(all[i].Id) && all[i].Category == categories[c])
                        Add(all[i], parts, rows);
                }

                if (empties || rows.Count > heading)
                    rows.Insert(heading, categories[c]);
            }
        }

        static void Add(Part part, List<Part> parts, List<object> rows)
        {
            parts.Add(part);
            rows.Add(part);
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
