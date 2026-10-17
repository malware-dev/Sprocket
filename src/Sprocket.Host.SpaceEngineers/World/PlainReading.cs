using System;
using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>A reading that needs nothing remembered.</summary>
    public sealed class PlainReading<T> : Reading where T : class
    {
        readonly Func<T, bool> _reads;

        /// <summary>A reading that <paramref name="reads" /> a block.</summary>
        public PlainReading(Func<T, bool> reads)
        {
            _reads = reads;
        }

        /// <inheritdoc />
        public override bool Remembers => false;

        /// <inheritdoc />
        public override bool Holds(IMyTerminalBlock block, ReaderMemory seen)
        {
            return _reads((T)block);
        }
    }
}
