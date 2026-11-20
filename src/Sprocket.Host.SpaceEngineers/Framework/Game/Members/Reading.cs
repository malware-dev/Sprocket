using System;
using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Members
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
            return new PlainReading<T>(reads);
        }

        /// <summary>A reading of a <typeparamref name="T" /> that remembers between looks.</summary>
        public static Reading Remembering<T>(Func<T, ReaderMemory, bool> reads) where T : class
        {
            return new RememberingReading<T>(reads);
        }
    }
}
