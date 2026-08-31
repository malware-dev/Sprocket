using System.Collections.Generic;
using Reactor.World;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;

// The statuses live in the Ingame namespaces, which cannot be imported whole: IMyDoor and friends
// are declared in both, and the mod-side ones are the ones with the events on them.
using DoorStatus = Sandbox.ModAPI.Ingame.DoorStatus;
using VentStatus = SpaceEngineers.Game.ModAPI.Ingame.VentStatus;
using Reactor.Host.SpaceEngineers.Presentation.Controls;
using Reactor.Host.SpaceEngineers.Presentation.Screen;
using Reactor.Host.SpaceEngineers.Presentation.Lists;
using Reactor.Host.SpaceEngineers.Presentation.Look;
using Reactor.Host.SpaceEngineers.Presentation.Picking;
using Reactor.Host.SpaceEngineers.Presentation.Sequences;
using Reactor.Host.SpaceEngineers.Terminal;

namespace Reactor.Host.SpaceEngineers.World
{
    /// <summary>
    /// What Reactor knows about one kind of block, over and above the floor every block stands on.
    /// </summary>
    /// <remarks>
    /// Keyed on the interface rather than on a block type id, which is what makes a modded door that
    /// implements <c>IMyDoor</c> inherit the whole door vocabulary without its author knowing Reactor
    /// exists. See docs/Catalogue.md.
    ///
    /// <b>The four-valued statuses are flattened to flags here.</b> A door is really Opening, Open, Closing
    /// or Closed, and saying so properly needs a reading that declares its own permitted values and a way to
    /// ask whether it is one of several - the first two needs in the catalogue. Until those exist, a flag
    /// per answer says the same things in the sentences an airlock actually uses.
    /// </remarks>
    interface IBlockExtension
    {
        bool Claims(IMyTerminalBlock block);

        void Describe(EntityKind kind);

        bool TryRead(IMyTerminalBlock block, string propertyId, out EntityValue value);

        bool TryInvoke(IMyTerminalBlock block, string actionId);

        /// <summary>
        /// This block's live state in the player's own words, or null to fall back to the floor.
        /// </summary>
        /// <remarks>
        /// "closing" beats "on" by a mile on a list of forty blocks, and it is the only thing a ship
        /// browser is for.
        /// </remarks>
        string StatusOf(IMyTerminalBlock block);

        /// <summary>
        /// Whether this block is doing something <em>right now</em>, which is not whether it works.
        /// </summary>
        /// <remarks>
        /// A row lights up on this, so answering it with "powered and switched on" lights every row on
        /// the ship and the light stops meaning anything. Only a thing in motion is busy.
        /// </remarks>
        bool IsBusy(IMyTerminalBlock block);

        /// <summary>
        /// Starts listening to a block, for anything the game will tell us rather than make us look.
        /// </summary>
        /// <remarks>
        /// Returns something to drop when the block goes, or null when there was nothing to hook. An event
        /// has to become an edge on a reading before core can use it, so what a listener does is hold a
        /// latch that the next tick reads and then clears.
        /// </remarks>
        IBlockListener Listen(IMyTerminalBlock block);
    }
}
