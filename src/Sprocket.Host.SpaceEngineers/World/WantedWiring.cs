using System;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// What the program wants wired right now: this button of this panel runs this sequence.
    /// </summary>
    public struct WantedWiring
    {
        /// <summary>The block whose toolbar it is, by entity id.</summary>
        public long PanelId;

        /// <summary>Which of the block's toolbars, from 1.</summary>
        public int Toolbar;

        /// <summary>Where in that toolbar's items, from 0, across its pages.</summary>
        public int Slot;

        /// <summary>The key the slot carries.</summary>
        public string Key;

        /// <summary>What the trigger named, so the block can be resolved again without a search.</summary>
        public string Reference;

        /// <summary>Which sequence on which one of its system, so reachability is reported for each one.</summary>
        public long Start;

        /// <summary>
        /// The signal slot's short name, or its name where it has none: a button panel's button is named so, and the caption
        /// under a toolbar slot's icon says it; null for a trigger written in the script.
        /// </summary>
        public string Name;

        /// <summary>Whether <paramref name="other" /> asks for the same key in the same slot of the same toolbar.</summary>
        public bool Same(WantedWiring other)
        {
            return PanelId == other.PanelId && Toolbar == other.Toolbar && Slot == other.Slot
                && string.Equals(Key, other.Key, StringComparison.Ordinal);
        }
    }
}
