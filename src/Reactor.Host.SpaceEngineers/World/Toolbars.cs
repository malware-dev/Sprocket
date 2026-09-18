using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using TerminalActionParameter = Sandbox.ModAPI.Ingame.TerminalActionParameter;

namespace Reactor.Host.SpaceEngineers.World
{
    /// <summary>
    /// How a toolbar is reached, said once.
    /// </summary>
    /// <remarks>
    /// One statement of how Reactor's wiring is found, read and written, so the next change to that
    /// answer is one edit rather than a hunt. What does the work is <see cref="ISlots" />.
    /// <para>
    /// <b>Static because the block vocabulary is.</b> An extension describing a button panel is
    /// reached through a static registry with no instance to hand anything to, so a toolbar is found
    /// the same way. The game's own API is a static for the same reason, and this holds a reference to
    /// an ordinary object rather than being one.
    /// </para>
    /// </remarks>
    public static class Toolbars
    {
        static ISlots _slots = new GameSlots();

        /// <summary>
        /// Puts something else behind every toolbar read and write.
        /// </summary>
        /// <remarks>
        /// For the harness, which answers these calls over toolbars it makes up. The game's own
        /// conveniences cannot be answered by anything but the game - they reach past
        /// <see cref="IMyToolbar" /> into its concrete item classes - so this is what lets a button
        /// on a made-up rig be tested at all.
        /// </remarks>
        public static void Use(ISlots slots)
        {
            _slots = slots ?? new GameSlots();
        }

        /// <summary>The toolbar a block's own actions live on, or null where it has none.</summary>
        public static IMyToolbar Of(IMyTerminalBlock block)
        {
            return _slots.Of(block);
        }

        /// <summary>What a slot's item was given to run with, appended to <paramref name="into" />.</summary>
        public static void Parameters(IMyToolbar toolbar, int slot, List<TerminalActionParameter> into)
        {
            _slots.Parameters(toolbar, slot, into);
        }

        /// <summary>Puts an item in a slot, and answers whether the write took.</summary>
        public static bool Write(IMyToolbar toolbar, int slot, MyToolbarItemInfo item,
            List<TerminalActionParameter> parameters)
        {
            return _slots.Write(toolbar, slot, item, parameters);
        }

        /// <summary>Empties a slot, and answers whether that took.</summary>
        public static bool Clear(IMyToolbar toolbar, int slot)
        {
            return _slots.Clear(toolbar, slot);
        }

        /// <summary>
        /// Whether a block has an action of the game's own by that name.
        /// </summary>
        /// <remarks>
        /// Asked before a slot is written with one, because the name Reactor holds is data and the
        /// block is the authority on what it offers. A subtype that does not have it costs a Reactor
        /// trigger rather than a slot that looks wired and does nothing.
        /// </remarks>
        public static bool Offers(IMyTerminalBlock block, string actionId)
        {
            return block != null && !string.IsNullOrEmpty(actionId)
                && _slots.Offers(block, actionId);
        }

        /// <summary>Whether the block a slot names is still anywhere in the world.</summary>
        public static bool Exists(long entityId)
        {
            return entityId != 0 && _slots.Exists(entityId);
        }
    }
}
