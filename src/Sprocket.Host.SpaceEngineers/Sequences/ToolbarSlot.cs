namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>One slot on one of a block's toolbars: which toolbar, which page and which slot, each counted from 1.</summary>
    /// <remarks>
    /// Where a signal is sent from, together with the block it is on. Which toolbar is the game's own order of a block's
    /// toolbars; the first is the one a player sets up by hand.
    /// </remarks>
    public sealed class ToolbarSlot
    {
        const string Spelling = "toolbar-";

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

        /// <summary>Whether this stands for many slots that run at one moment: page 0 is every page, slot 0 every slot.</summary>
        /// <remarks>A timer runs every slot when it goes off, and a sensor the first slot of every page when it detects something.</remarks>
        public bool IsMany => Page == 0 || Slot == 0;

        /// <summary>The id of the trigger this slot, or these slots, are on its block: <c>toolbar-1-1-4</c>, or <c>toolbar-1-0-1</c> for many.</summary>
        /// <remarks>What the picker offers and answers with; the language writes one slot itself, never this.</remarks>
        public string EventId => Spelling + Toolbar + "-" + Page + "-" + Slot;

        /// <summary>Whether <paramref name="other" /> is the same slot.</summary>
        public bool Same(ToolbarSlot other)
        {
            return other != null && Toolbar == other.Toolbar && Page == other.Page && Slot == other.Slot;
        }

        /// <summary>Whether the one slot <paramref name="other" /> is among those this stands for.</summary>
        public bool Covers(ToolbarSlot other)
        {
            return other != null && Toolbar == other.Toolbar && (Page == 0 || Page == other.Page) && (Slot == 0 || Slot == other.Slot);
        }

        /// <summary>The slot, or slots, a trigger's id names, or null where <paramref name="eventId" /> names none.</summary>
        public static ToolbarSlot Of(string eventId)
        {
            if (string.IsNullOrEmpty(eventId) || !eventId.StartsWith(Spelling, System.StringComparison.Ordinal))
                return null;

            var parts = eventId.Substring(Spelling.Length).Split('-');
            int toolbar, page, slot;
            if (parts.Length != 3 || !int.TryParse(parts[0], out toolbar) || !int.TryParse(parts[1], out page)
                || !int.TryParse(parts[2], out slot) || toolbar < 1 || page < 0 || slot < 0)
                return null;

            return new ToolbarSlot(toolbar, page, slot);
        }
    }
}
