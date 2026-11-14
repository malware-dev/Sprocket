using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Members
{
    /// <summary>A flag read the other way round.</summary>
    public sealed class NegatedGauge : Reading
    {
        readonly Reading _flag;

        /// <summary>The flag <paramref name="flag" />, read the other way round.</summary>
        public NegatedGauge(Reading flag)
        {
            _flag = flag;
        }

        /// <inheritdoc />
        public override bool Remembers => _flag.Remembers;

        /// <inheritdoc />
        public override bool Holds(IMyTerminalBlock block, ReaderMemory seen)
        {
            return !_flag.Holds(block, seen);
        }
    }
}
