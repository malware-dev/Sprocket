using System;
using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>One thing a block can be - a door, a switch, a store of gas - and everything Sprocket knows about being it.</summary>
    /// <remarks>Holds nothing about any one block: one capability serves every block of every construct.</remarks>
    public abstract class Capability
    {
        /// <summary>Whether <paramref name="block" /> is this.</summary>
        public abstract bool Claims(IMyTerminalBlock block);

        /// <summary>Adds to <paramref name="kind" /> what being this lets a block do, be, and have happen to it.</summary>
        /// <remarks>The block stands for its definition: anything read from it here is read once, for every block of that type.</remarks>
        public abstract void Describe(EntityKind kind, IMyTerminalBlock block);

        /// <summary>Tells <paramref name="told" /> each time <paramref name="block" /> may have changed, until the answer is disposed; null where it says nothing.</summary>
        /// <remarks>Each telling prompts a look, and is never taken as the moment itself. Told on the game's main thread, from the block's own update.</remarks>
        public virtual IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            return null;
        }

        /// <summary>What <paramref name="block" /> is doing, or what is stopping it, or <see cref="Status.None" /> where this has nothing to say.</summary>
        public virtual Status StatusOf(IMyTerminalBlock block)
        {
            return Status.None;
        }

        /// <summary>Whether <paramref name="block" /> is doing something <em>right now</em>, which is not whether it works.</summary>
        /// <remarks>Only a thing in motion is busy.</remarks>
        public virtual bool IsBusy(IMyTerminalBlock block)
        {
            return false;
        }
    }
}
