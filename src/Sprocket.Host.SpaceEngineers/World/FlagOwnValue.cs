using System;
using System.Collections.Generic;
using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>A flag read by a <see cref="Reading" />.</summary>
    public sealed class FlagOwnValue : OwnValue
    {
        readonly Reading _flag;

        /// <summary>A value that is the flag <paramref name="flag" /> reads.</summary>
        public FlagOwnValue(Reading flag)
        {
            _flag = flag;
        }

        /// <inheritdoc />
        public override ValueKind Kind => ValueKind.Flag;

        /// <inheritdoc />
        public override IReadOnlyList<string> Names => Array.Empty<string>();

        /// <inheritdoc />
        public override bool Remembers => _flag.Remembers;

        /// <inheritdoc />
        public override Gauge Gauge(IMyTerminalBlock block, IList<Literal> arguments, ReaderMemory seen)
        {
            return new FlagGauge(_flag, block, seen);
        }

        /// <inheritdoc />
        public override Reading Is(Literal becomes)
        {
            return becomes.Flag ? _flag : new NegatedGauge(_flag);
        }
    }
}
