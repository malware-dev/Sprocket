using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Members
{
    /// <summary>One of the block's values, already the type it holds.</summary>
    public sealed class GameValueOrder<T> : BlockOrder
    {
        readonly IMyTerminalBlock _block;
        readonly ITerminalProperty<T> _property;
        readonly T _value;

        /// <summary>The value <paramref name="property" /> of <paramref name="block" />, to be set to <paramref name="value" />.</summary>
        public GameValueOrder(IMyTerminalBlock block, ITerminalProperty<T> property, T value)
        {
            _block = block;
            _property = property;
            _value = value;
        }

        /// <inheritdoc />
        public override void Give()
        {
            _property.SetValue(_block, _value);
        }
    }
}
