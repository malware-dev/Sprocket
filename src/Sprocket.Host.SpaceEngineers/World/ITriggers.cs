using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>What a kind of block's toolbar slots mean as triggers: which run at one moment, which never run of themselves, and what each reads as.</summary>
    /// <remarks>
    /// Every toolbar slot is a trigger without anything saying so; a block with none of these offers each of its slots by where
    /// it is. This is for a block the game runs in a known way: a timer runs every slot when it goes off, so it offers one
    /// trigger rather than ninety that all mean the same.
    /// </remarks>
    interface ITriggers
    {
        /// <summary>Whether this is about <paramref name="block" />.</summary>
        bool Claims(IMyTerminalBlock block);

        /// <summary>Adds the triggers of <paramref name="block" /> to <paramref name="kind" />.</summary>
        void Describe(EntityKind kind, IMyTerminalBlock block);
    }
}
