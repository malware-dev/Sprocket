using System;
using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>One block told what this is to be, by one step.</summary>
    public sealed class OwnValueSetting : BlockOrder
    {
        readonly Action<IMyTerminalBlock, Literal> _write;
        readonly IMyTerminalBlock _block;
        readonly Literal _value;

        /// <summary>Sets <paramref name="block" /> to <paramref name="value" /> through <paramref name="write" />.</summary>
        public OwnValueSetting(Action<IMyTerminalBlock, Literal> write, IMyTerminalBlock block, Literal value)
        {
            _write = write;
            _block = block;
            _value = value;
        }

        /// <inheritdoc />
        public override void Give()
        {
            _write(_block, _value);
        }
    }
}
