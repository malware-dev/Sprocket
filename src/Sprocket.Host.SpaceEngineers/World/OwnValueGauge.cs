using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>A value of a block read as it reads now, with the options one test gave it.</summary>
    public sealed class OwnValueGauge<T, TOptions> : Gauge where T : class
    {
        readonly TypedOwnValue<T, TOptions> _being;
        readonly TOptions _options;
        readonly T _block;
        readonly ReaderMemory _seen;

        /// <summary>The value <paramref name="being" /> of <paramref name="block" /> with <paramref name="options" />, remembering between looks in <paramref name="seen" />.</summary>
        public OwnValueGauge(TypedOwnValue<T, TOptions> being, TOptions options, IMyTerminalBlock block, ReaderMemory seen)
        {
            _being = being;
            _options = options;
            _block = (T)block;
            _seen = seen;
        }

        /// <inheritdoc />
        public override ValueKind Kind => _being.Kind == ValueKind.Name ? ValueKind.Integer : _being.Kind;

        /// <inheritdoc />
        public override Literal Read()
        {
            return _being.Read(_block, _options, _seen);
        }
    }
}
