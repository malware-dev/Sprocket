using System;
using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>A reading that remembers between looks.</summary>
    public sealed class KeptReading<T> : Reading where T : class
    {
        readonly Func<T, ReaderMemory, bool> _reads;

        /// <summary>A reading that <paramref name="reads" /> a block with what it remembers of it.</summary>
        public KeptReading(Func<T, ReaderMemory, bool> reads)
        {
            _reads = reads;
        }

        /// <inheritdoc />
        public override bool Remembers => true;

        /// <inheritdoc />
        public override bool Holds(IMyTerminalBlock block, ReaderMemory seen)
        {
            return _reads((T)block, seen);
        }
    }
}
