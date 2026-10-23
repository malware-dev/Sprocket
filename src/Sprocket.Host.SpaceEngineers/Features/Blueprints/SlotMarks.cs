using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;

namespace Sprocket.Host.SpaceEngineers.Features.Blueprints
{
    /// <summary>What each installation of a blueprint still has to do about its slots, worked out from the slots and their blocks.</summary>
    public static class SlotMarks
    {
        /// <summary>What <paramref name="instance" /> still has to do about <paramref name="part" />.</summary>
        public static SlotMark Of(Bindings bindings, SystemInstance instance, Part part)
        {
            var filled = instance.FilledWith(part.Id);
            if (filled == null || filled.Count == 0)
                return SlotMark.New;

            for (var i = 0; i < filled.Count; i++)
            {
                if (bindings.Entity(filled[i]) == null)
                    return SlotMark.Lost;
            }

            return Misfits(bindings, instance, part) ? SlotMark.Changed : SlotMark.None;
        }

        /// <summary>How many of the slots <paramref name="used" /> <paramref name="instance" /> has to fill again, where their kind changed and a block in them no longer fits it.</summary>
        public static int Changed(Bindings bindings, AutomationSystem system, SystemInstance instance, IList<string> used)
        {
            var count = 0;
            var parts = system.Parts;
            for (var i = 0; i < parts.Count; i++)
            {
                if (parts[i].IsSignal || !used.Contains(parts[i].Id))
                    continue;

                if (Misfits(bindings, instance, parts[i]))
                    count++;
            }

            return count;
        }

        /// <summary>Whether a block <paramref name="instance" /> fills the block slot <paramref name="part" /> with is of a kind the slot's type no longer takes.</summary>
        static bool Misfits(Bindings bindings, SystemInstance instance, Part part)
        {
            if (part.IsSignal || part.Holds.Length != 0)
                return false;

            var filled = instance.FilledWith(part.Id);
            for (var i = 0; filled != null && i < filled.Count; i++)
            {
                var entity = bindings.Entity(filled[i]);
                if (entity != null && !part.Fits(entity.Kind.Lineage))
                    return true;
            }

            return false;
        }
    }
}
