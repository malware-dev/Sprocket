using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>One of Sprocket's own actions as one step asks for it: its behavior, with the step's options already read.</summary>
    /// <remarks>
    /// Made when the step is compiled, once for every block of the step that does the same thing, so nothing a step was
    /// given is read again while it runs. See <see cref="OwnAction.Bind" />.
    /// </remarks>
    public abstract class BoundAction
    {
        /// <summary>Whether this does anything itself, rather than leaving the act to the game's own action.</summary>
        public abstract bool Acts { get; }

        /// <summary>Tells <paramref name="block" /> to do it.</summary>
        public abstract void Do(IMyTerminalBlock block, ReaderMemory seen);

        /// <summary>Whether <paramref name="block" /> has done what it was told, though what the action waits for does not hold.</summary>
        /// <remarks>Asked on the cadence troubles are asked on, never on a tick.</remarks>
        public abstract bool Done(IMyTerminalBlock block, ReaderMemory seen);

        /// <summary>Why <paramref name="block" /> will not finish, or none while it might.</summary>
        /// <remarks>Asked of a block a run is held waiting on, on a cadence and never on a tick.</remarks>
        public abstract BlockTrouble Trouble(IMyTerminalBlock block, ReaderMemory seen);

        /// <summary>Calls off what this started on <paramref name="block" />.</summary>
        public abstract void Stop(IMyTerminalBlock block);

        /// <summary>How many ticks <paramref name="block" /> should take to finish, or <see cref="GridEntity.Unestimated" />.</summary>
        public abstract int Expected(IMyTerminalBlock block);
    }
}
