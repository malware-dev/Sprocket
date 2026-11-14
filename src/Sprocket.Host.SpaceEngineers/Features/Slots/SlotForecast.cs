using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Features.Slots
{
    /// <summary>Which slots a pick names, as the picker's heading shows it and as the edit made from the pick makes it.</summary>
    public sealed class SlotForecast
    {
        /// <summary>A pick naming <paramref name="matched" /> or, where that is null, changing <paramref name="changes" /> in place, or else making a slot.</summary>
        public SlotForecast(List<string> matched, Part changes, bool shared, bool signal, HostType type)
        {
            Matched = matched;
            Changes = changes;
            Shared = shared;
            Signal = signal;
            Type = type;
        }

        /// <summary>The slots the picks fill already, which the pick names; null where none do.</summary>
        public List<string> Matched { get; private set; }

        /// <summary>The slot the pick changes in place, the one the line it goes in place of names; null where it matches or makes one.</summary>
        public Part Changes { get; private set; }

        /// <summary>Whether a step or a trigger besides the line the pick goes in place of uses the slot it changes, which is asked about first.</summary>
        public bool Shared { get; private set; }

        /// <summary>Whether the pick is of what a toolbar sends, which a signal slot carries.</summary>
        public bool Signal { get; private set; }

        /// <summary>The type the host provides that what is chosen is a member of, which a slot holding it carries; null for blocks.</summary>
        public HostType Type { get; private set; }

        /// <summary>Whether the pick makes a new slot.</summary>
        public bool Makes => Matched == null && Changes == null;
    }
}
