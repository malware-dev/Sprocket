using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using System.Text;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Members
{
    /// <summary>
    /// One of the block's text values.
    /// </summary>
    /// <remarks>Allocates a builder each time it is given: the block keeps the builder it is handed.</remarks>
    public sealed class GameTextOrder : BlockOrder
    {
        readonly IMyTerminalBlock _block;
        readonly ITerminalProperty<StringBuilder> _property;
        readonly string _text;

        /// <summary>The text value <paramref name="property" /> of <paramref name="block" />, to be set to <paramref name="text" />.</summary>
        public GameTextOrder(IMyTerminalBlock block, ITerminalProperty<StringBuilder> property, string text)
        {
            _block = block;
            _property = property;
            _text = text;
        }

        /// <inheritdoc />
        public override void Give()
        {
            _property.SetValue(_block, new StringBuilder(_text));
        }
    }
}
