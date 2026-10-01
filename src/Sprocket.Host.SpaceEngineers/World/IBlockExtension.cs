using System.Collections.Generic;
using Sandbox.ModAPI;

// The statuses live in the Ingame namespaces, which cannot be imported whole: IMyDoor and friends
// are declared in both, and the mod-side ones are the ones with the events on them.

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// What Sprocket knows about one kind of block, over and above the floor every block stands on.
    /// </summary>
    /// <remarks>
    /// Keyed on the interface rather than on a block type id, which is what makes a modded door that
    /// implements <c>IMyDoor</c> inherit the whole door vocabulary without its author knowing Sprocket
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

        /// <summary>
        /// Whether this block is arranged to raise that event <b>into this key</b>.
        /// </summary>
        /// <remarks>
        /// True for anything with nothing to arrange. A button panel is the exception: its slot has
        /// to hold Sprocket's action <i>and</i> the key of the thing that should run, because a slot
        /// pointed at a different sequence is wired and useless to this one. Somebody who cleared or
        /// re-pointed it has said something, and that is a thing to report rather than to undo.
        /// <para>
        /// Reading a toolbar is not free, so this is asked on a cadence and never on a tick.
        /// </para>
        /// </remarks>
        bool Raises(IMyTerminalBlock block, string eventId, string key);

        /// <summary>
        /// Says what this sort of block can do, be, and have happen to it.
        /// </summary>
        /// <remarks>
        /// Handed the block as well as the kind, because a kind is cached per definition and some
        /// definitions differ in what they offer - a four-button panel has four buttons to name and
        /// a one-button panel has one. The block is a representative of its definition and nothing
        /// more: anything read from it here is read once, for every block of that type.
        /// </remarks>
        void Describe(EntityKind kind, IMyTerminalBlock block);

        bool TryRead(IMyTerminalBlock block, string propertyId, out EntityValue value);

        bool TryInvoke(IMyTerminalBlock block, string actionId, IList<Literal> values);

        /// <summary>
        /// What this block is doing, or what is stopping it, or <see cref="Status.None" /> to fall back to the floor.
        /// </summary>
        /// <remarks>
        /// "closing" beats "on" by a mile on a list of forty blocks, and it is the only thing a ship
        /// browser is for.
        /// </remarks>
        Status StatusOf(IMyTerminalBlock block);

        /// <summary>
        /// Why this block will not finish what it was told to do, or null while it might.
        /// </summary>
        /// <remarks>
        /// Asked of a block a sequence is held waiting on, so what it answers is read by somebody
        /// looking at a sequence that has stopped. Nothing generic can say this: the reason a block
        /// cannot finish is knowledge about that kind of block and lives with the rest of it.
        /// <para>
        /// <paramref name="stateId" /> is the state the step is waiting for, and the question is
        /// about that rather than about the block. One block serves several sequences and they do
        /// not want the same thing of it: an airlock's vent is asked to empty a room by one and to
        /// fill it by another, and what is wrong is a different answer for each of them.
        /// </para>
        /// </remarks>
        Trouble TroubleWith(IMyTerminalBlock block, string stateId);

        /// <summary>
        /// Whether this block is doing something <em>right now</em>, which is not whether it works.
        /// </summary>
        /// <remarks>
        /// A row lights up on this, so answering it with "powered and switched on" lights every row on
        /// the ship and the light stops meaning anything. Only a thing in motion is busy.
        /// </remarks>
        bool IsBusy(IMyTerminalBlock block);

        /// <summary>
        /// Calls off what this action started, for a block the sequence was still waiting on.
        /// </summary>
        /// <remarks>
        /// Every way a run ends comes through here for everything it had in flight: switched off,
        /// stopped by hand, taken over by something else, or at fault. A rotor left turning is the
        /// case this exists for - a stop that leaves a machine moving is not a stop - and what
        /// calling it off means is the block's own to say. A door that was opening goes on opening,
        /// because a door half closed on somebody is worse than a door left open.
        /// </remarks>
        void Stop(IMyTerminalBlock block, string actionId);

        /// <summary>
        /// How many ticks the block should take to finish what this action started, or
        /// <see cref="GridEntity.Unestimated" /> where it cannot say.
        /// </summary>
        /// <remarks>
        /// Decides how often a wait for it is looked at, never whether it has arrived: a wrong answer
        /// costs a few ticks either way and nothing else.
        /// </remarks>
        int Expected(IMyTerminalBlock block, string actionId, IList<Literal> values);
    }
}
