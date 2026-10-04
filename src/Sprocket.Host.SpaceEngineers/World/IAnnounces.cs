using System;
using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// A kind of block that says when what Sprocket reads of it may have changed.
    /// </summary>
    /// <remarks>
    /// What it says prompts a look and is never taken as the moment itself: the look reads the block, and a block that
    /// announces a change that did not happen costs one look. Implemented alongside <see cref="IBlockExtension" /> by the
    /// extension that knows which of the game's own events the block raises, and marked on each moment it covers - see
    /// <see cref="EntityKind.Announced" />.
    /// </remarks>
    public interface IAnnounces
    {
        /// <summary>Tells <paramref name="told" /> each time <paramref name="block" /> may have changed, until the answer is disposed.</summary>
        /// <remarks>Told on the game's main thread, from the block's own update, where nothing else of Sprocket's is running.</remarks>
        IDisposable Listen(IMyTerminalBlock block, Action told);
    }
}
