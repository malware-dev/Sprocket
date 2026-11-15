using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Arranging;
using System.Collections.Generic;
using Lumel.Drawing;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Features.Slots
{
    /// <summary>
    /// A blueprint's slots, one under the next in the system's order, each run of slots under one category headed by its name,
    /// with every slot's name in one column as wide as the widest of them needs.
    /// </summary>
    /// <remarks>Where the slots can be arranged, a slot is carried by its grip and a category by its own, with its slots.</remarks>
    public class PartList : CategoryList
    {
        /// <summary>The widest the column gets.</summary>
        public const float Widest = 560f;

        /// <summary>
        /// Puts into <paramref name="parts" /> every slot of <paramref name="system" />, in its order, and into
        /// <paramref name="rows" /> the same slots with a <see cref="CategoryHeading" /> before each run of them under one category;
        /// a category with no slot is headed too, where it stands, where <paramref name="empties" />; and a <see cref="CategoryEnd" /> after
        /// each category where <paramref name="ends" />.
        /// </summary>
        public static void Listed(AutomationSystem system, List<Part> parts, List<object> rows, bool empties, bool ends)
        {
            Listed(system.SlotList, rows, empties, ends);
            parts.Clear();
            foreach (var row in rows)
            {
                var part = row as Part;
                if (part != null)
                    parts.Add(part);
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
