using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Members
{
    /// <summary>One of the block's switches whose setter flips it, told only where it does not already read as asked.</summary>
    public sealed class GameSwitchOrder : BlockOrder
    {
        readonly IMyTerminalBlock _block;
        readonly ITerminalProperty<bool> _property;
        readonly bool _value;

        /// <summary>The switch <paramref name="property" /> of <paramref name="block" />, to be set to <paramref name="value" />.</summary>
        public GameSwitchOrder(IMyTerminalBlock block, ITerminalProperty<bool> property, bool value)
        {
            _block = block;
            _property = property;
            _value = value;
        }

        /// <inheritdoc />
        public override void Give()
        {
            if (_property.GetValue(_block) != _value)
                _property.SetValue(_block, _value);
        }
    }
}
