namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>One slot on one of a block's toolbars: which toolbar, which page and which slot, each counted from 1.</summary>
    /// <remarks>
    /// What answers a trigger, together with the block it is on. Which toolbar is the game's own order of a block's
    /// toolbars; the first is the one a player sets up by hand.
    /// </remarks>
    public sealed class ToolbarSlot
    {
        /// <summary>Slot <paramref name="slot" /> of page <paramref name="page" /> of toolbar <paramref name="toolbar" />, each counted from 1.</summary>
        public ToolbarSlot(int toolbar, int page, int slot)
        {
            Toolbar = toolbar;
            Page = page;
            Slot = slot;
        }

        /// <summary>Which of the block's toolbars, from 1.</summary>
        public int Toolbar { get; private set; }

        /// <summary>Which page of that toolbar, from 1.</summary>
        public int Page { get; private set; }

        /// <summary>Which slot on that page, from 1.</summary>
        public int Slot { get; private set; }

        /// <summary>Whether <paramref name="other" /> is the same slot.</summary>
        public bool Same(ToolbarSlot other)
        {
            return other != null && Toolbar == other.Toolbar && Page == other.Page && Slot == other.Slot;
        }
    }
}
