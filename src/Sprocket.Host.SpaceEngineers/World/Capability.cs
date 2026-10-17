using System;
using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>One thing a block can be - a door, a switch, a store of gas - and everything Sprocket knows about being it.</summary>
    /// <remarks>
    /// A block is every capability that claims it, and its kind is described by each in turn, so a cryo chamber can be a
    /// seat and a store and a switch at once. Claimed by the interface the game gives the block rather than by a block
    /// type id, which is what makes a modded door that implements <c>IMyDoor</c> a door without its author knowing
    /// Sprocket exists.
    /// <para>
    /// <b>Holds nothing about any one block.</b> What a member remembers of a block between looks belongs to whatever
    /// reads it - see <see cref="ReaderMemory" /> - so one capability serves every block of every construct.
    /// </para>
    /// </remarks>
    public abstract class Capability
    {
        /// <summary>Whether <paramref name="block" /> is this.</summary>
        public abstract bool Claims(IMyTerminalBlock block);

        /// <summary>Adds to <paramref name="kind" /> what being this lets a block do, be, and have happen to it.</summary>
        /// <remarks>
        /// Handed the block as well as the kind, because a kind is kept per definition and some definitions differ in
        /// what they offer - a four-button panel has four buttons to name and a one-button panel has one. The block is a
        /// representative of its definition and nothing more: anything read from it here is read once, for every block of
        /// that type.
        /// </remarks>
        public abstract void Describe(EntityKind kind, IMyTerminalBlock block);

        /// <summary>Tells <paramref name="told" /> each time <paramref name="block" /> may have changed, until the answer is disposed; null where it says nothing.</summary>
        /// <remarks>
        /// What it says prompts a look and is never taken as the moment itself: the look reads the block, and a block that
        /// announces a change that did not happen costs one look. Marked on each moment it covers - see
        /// <see cref="EntityKind.Announced" />. Told on the game's main thread, from the block's own update.
        /// </remarks>
        public virtual IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            return null;
        }

        /// <summary>What <paramref name="block" /> is doing, or what is stopping it, or <see cref="Status.None" /> where this has nothing to say.</summary>
        /// <remarks>The label beside the block in a list of blocks: "closing" beats "on" by a mile on a list of forty.</remarks>
        public virtual Status StatusOf(IMyTerminalBlock block)
        {
            return Status.None;
        }

        /// <summary>Whether <paramref name="block" /> is doing something <em>right now</em>, which is not whether it works.</summary>
        /// <remarks>
        /// A row lights up on this, so answering it with "powered and switched on" lights every row on the ship and the
        /// light stops meaning anything. Only a thing in motion is busy.
        /// </remarks>
        public virtual bool IsBusy(IMyTerminalBlock block)
        {
            return false;
        }
    }
}
