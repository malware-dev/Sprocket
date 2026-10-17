using System;
using System.Collections.Generic;
using Lumel.Drawing;
using Lumel.Layout;
using Sprocket.Host.SpaceEngineers.Sequences;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Features.Blueprints
{
    /// <summary>A blueprint's slots, one under the next, with every slot's name in one column as wide as the widest of them needs.</summary>
    public class PartList : VStack
    {
        /// <summary>The widest the column gets.</summary>
        public const float Widest = 560f;

        /// <summary>
        /// Puts into <paramref name="into" /> the slots of <paramref name="system" /> that <paramref name="used" /> names, as they
        /// are listed: the block slots, then the signal slots, each in order of their names.
        /// </summary>
        public static void Listed(AutomationSystem system, IList<string> used, List<Part> into)
        {
            into.Clear();
            var parts = system.Parts;
            for (var i = 0; i < parts.Count; i++)
            {
                if (used.Contains(parts[i].Id))
                    into.Add(parts[i]);
            }

            into.Sort(ByKind);
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

        /// <summary>Block slots before signal slots, and each kind by name, letters compared without their case.</summary>
        static int ByKind(Part one, Part other)
        {
            if (one.IsSignal != other.IsSignal)
                return one.IsSignal ? 1 : -1;

            var named = string.Compare(one.Id, other.Id, StringComparison.OrdinalIgnoreCase);
            return named != 0 ? named : string.CompareOrdinal(one.Id, other.Id);
        }
    }
}
