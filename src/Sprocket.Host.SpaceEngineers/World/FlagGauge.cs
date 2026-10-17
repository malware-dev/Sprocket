using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>A flag of a block read as it reads now.</summary>
    public sealed class FlagGauge : Gauge
    {
        readonly Reading _flag;
        readonly IMyTerminalBlock _block;
        readonly ReaderMemory _seen;

        /// <summary>The flag <paramref name="flag" /> of <paramref name="block" />, remembering between looks in <paramref name="seen" />.</summary>
        public FlagGauge(Reading flag, IMyTerminalBlock block, ReaderMemory seen)
        {
            _flag = flag;
            _block = block;
            _seen = seen;
        }

        /// <inheritdoc />
        public override ValueKind Kind => ValueKind.Flag;

        /// <inheritdoc />
        public override Literal Read()
        {
            return Literal.Of(_flag.Holds(_block, _seen));
        }
    }
}
