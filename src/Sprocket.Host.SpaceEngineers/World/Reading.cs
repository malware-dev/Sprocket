using System;
using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>A flag read off a block: whether a door is open, whether a room has air.</summary>
    /// <remarks>Reached directly by whatever was compiled to read it, so a look is a cast and a delegate call.</remarks>
    public abstract class Reading
    {
        /// <summary>Whether this needs a <see cref="ReaderMemory" /> of its own for each block it reads.</summary>
        public abstract bool Remembers { get; }

        /// <summary>Whether <paramref name="block" /> reads true, remembering into <paramref name="seen" /> where this <see cref="Remembers" />.</summary>
        /// <remarks>Asked on the tick path. <paramref name="block" /> is always one this was declared for, so the cast in it never fails.</remarks>
        public abstract bool Holds(IMyTerminalBlock block, ReaderMemory seen);

        /// <summary>A reading of a <typeparamref name="T" />.</summary>
        public static Reading Of<T>(Func<T, bool> reads) where T : class
        {
            return new Plain<T>(reads);
        }

        /// <summary>A reading of a <typeparamref name="T" /> that remembers between looks.</summary>
        public static Reading Remembering<T>(Func<T, ReaderMemory, bool> reads) where T : class
        {
            return new Kept<T>(reads);
        }

        /// <summary>A reading that needs nothing remembered.</summary>
        sealed class Plain<T> : Reading where T : class
        {
            readonly Func<T, bool> _reads;

            public Plain(Func<T, bool> reads)
            {
                _reads = reads;
            }

            public override bool Remembers => false;

            public override bool Holds(IMyTerminalBlock block, ReaderMemory seen)
            {
                return _reads((T)block);
            }
        }

        /// <summary>A reading that remembers between looks.</summary>
        sealed class Kept<T> : Reading where T : class
        {
            readonly Func<T, ReaderMemory, bool> _reads;

            public Kept(Func<T, ReaderMemory, bool> reads)
            {
                _reads = reads;
            }

            public override bool Remembers => true;

            public override bool Holds(IMyTerminalBlock block, ReaderMemory seen)
            {
                return _reads((T)block, seen);
            }
        }
    }
}
