using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;

namespace Sprocket.Host.SpaceEngineers.Features.Blueprints
{
    /// <summary>What each installation of a blueprint still has to do about its slots, worked out from the slots and their blocks.</summary>
    /// <remarks>
    /// Nothing is stored: a slot an installation has not filled, while it holds blocks in slots nothing uses any more, was split
    /// or merged under it, and those blocks are what it had for it.
    /// </remarks>
    public static class SlotMarks
    {
        /// <summary>What <paramref name="instance" /> still has to do about <paramref name="part" />, of the slots <paramref name="used" />.</summary>
        public static SlotMark Of(Bindings bindings, AutomationSystem system, SystemInstance instance, Part part, IList<string> used)
        {
            var filled = instance.FilledWith(part.Id);
            if (filled == null || filled.Count == 0)
                return !part.IsSignal && HasSetAside(system, instance, used) ? SlotMark.Changed : SlotMark.New;

            for (var i = 0; i < filled.Count; i++)
            {
                if (bindings.Entity(filled[i]) == null)
                    return SlotMark.Lost;
            }

            return SlotMark.None;
        }

        /// <summary>How many of the slots <paramref name="used" /> <paramref name="instance" /> has to fill again after they were split or merged.</summary>
        public static int Changed(AutomationSystem system, SystemInstance instance, IList<string> used)
        {
            if (!HasSetAside(system, instance, used))
                return 0;

            var count = 0;
            var parts = system.Parts;
            for (var i = 0; i < parts.Count; i++)
            {
                if (parts[i].IsSignal || !used.Contains(parts[i].Id))
                    continue;

                var filled = instance.FilledWith(parts[i].Id);
                if (filled == null || filled.Count == 0)
                    count++;
            }

            return count;
        }

        /// <summary>Puts into <paramref name="into" /> the blocks <paramref name="instance" /> holds in slots nothing of <paramref name="used" /> names, each once.</summary>
        public static void SetAside(AutomationSystem system, SystemInstance instance, IList<string> used, List<string> into)
        {
            into.Clear();

            var parts = system.Parts;
            for (var i = 0; i < parts.Count; i++)
            {
                if (!IsHidden(parts[i], used))
                    continue;

                var filled = instance.FilledWith(parts[i].Id);
                for (var b = 0; filled != null && b < filled.Count; b++)
                {
                    if (!into.Contains(filled[b]))
                        into.Add(filled[b]);
                }
            }
        }

        /// <summary>Empties the slots nothing of <paramref name="used" /> names for every installation of <paramref name="system" /> that has no slot left to fill again.</summary>
        /// <remarks>
        /// Their blocks were kept to be offered back, and with every slot filled there is nothing left to offer them for. The
        /// installation the slots were worked out through keeps its blocks in a slot split under another as well, so that slot
        /// comes back whole where the split is undone, and lets go of them here too.
        /// </remarks>
        public static void Settle(AutomationSystem system, IList<string> used)
        {
            var instances = system.Instances;
            for (var n = 0; n < instances.Count; n++)
            {
                if (Changed(system, instances[n], used) > 0)
                    continue;

                var parts = system.Parts;
                for (var i = 0; i < parts.Count; i++)
                {
                    var filled = IsHidden(parts[i], used) ? instances[n].FilledWith(parts[i].Id) : null;
                    if (filled != null && filled.Count > 0)
                        instances[n].Fill(parts[i].Id, new string[0]);
                }
            }
        }

        /// <summary>Whether <paramref name="instance" /> holds blocks in a slot nothing of <paramref name="used" /> names.</summary>
        static bool HasSetAside(AutomationSystem system, SystemInstance instance, IList<string> used)
        {
            var parts = system.Parts;
            for (var i = 0; i < parts.Count; i++)
            {
                var filled = IsHidden(parts[i], used) ? instance.FilledWith(parts[i].Id) : null;
                if (filled != null && filled.Count > 0)
                    return true;
            }

            return false;
        }

        /// <summary>Whether <paramref name="part" /> is a block slot nothing of <paramref name="used" /> names.</summary>
        static bool IsHidden(Part part, IList<string> used)
        {
            return !part.IsSignal && !used.Contains(part.Id);
        }
    }
}
