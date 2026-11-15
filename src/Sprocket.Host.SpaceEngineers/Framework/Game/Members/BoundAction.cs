using System.Collections.Generic;
using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Members
{
    /// <summary>One of Sprocket's own actions as one step asks for it: its behavior, with the step's options already read.</summary>
    /// <remarks>Made when the step is compiled, and never while it runs.</remarks>
    public abstract class BoundAction
    {
        /// <summary>Whether this does anything itself, rather than leaving the act to the game's own action.</summary>
        public abstract bool Acts { get; }

        /// <summary>Whether it is told every block of its step together, as the step starts, before it is done on any of them.</summary>
        public virtual bool Begins => false;

        /// <summary>Tells it <paramref name="blocks" />, every block its step does it on, as the step starts and before <see cref="Do" />.</summary>
        public virtual void Begin(IList<IMyTerminalBlock> blocks)
        {
        }

        /// <summary>Tells <paramref name="block" /> to do it.</summary>
        public abstract void Do(IMyTerminalBlock block, ReaderMemory seen);

        /// <summary>Whether <paramref name="block" /> has done what it was told, though what the action waits for does not hold.</summary>
        /// <remarks>Asked on the cadence troubles are asked on, never on a tick.</remarks>
        public abstract bool Done(IMyTerminalBlock block, ReaderMemory seen);

        /// <summary>Why <paramref name="block" /> will not finish, or none while it might.</summary>
        /// <remarks>Asked of a block a run is held waiting on, on a cadence and never on a tick.</remarks>
        public abstract BlockTrouble Trouble(IMyTerminalBlock block, ReaderMemory seen);

        /// <summary>Whether this is forced: the block is kept at it until it gets there, and going the wrong way is no trouble to a run.</summary>
        public virtual bool IsForced => false;

        /// <summary>Whether <paramref name="block" />, told this by force, has been sent another way.</summary>
        public virtual bool Strays(IMyTerminalBlock block, ReaderMemory seen)
        {
            return false;
        }

        /// <summary>Calls off what this started on <paramref name="block" />.</summary>
        public abstract void Stop(IMyTerminalBlock block);

        /// <summary>How many ticks <paramref name="block" /> should take to finish, or <see cref="Grids.GridEntity.Unestimated" />.</summary>
        public abstract int Expected(IMyTerminalBlock block);
    }
}
