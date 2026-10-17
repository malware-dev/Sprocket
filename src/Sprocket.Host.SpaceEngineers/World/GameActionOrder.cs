using System.Collections.Generic;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using VRage.Collections;
using TerminalActionParameter = Sandbox.ModAPI.Ingame.TerminalActionParameter;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>One of the block's actions, with its values already the types it takes.</summary>
    public sealed class GameActionOrder : BlockOrder
    {
        readonly IMyTerminalBlock _block;
        readonly ITerminalAction _action;
        readonly List<TerminalActionParameter> _given;

        /// <summary>The action <paramref name="action" /> of <paramref name="block" />, given <paramref name="given" />.</summary>
        public GameActionOrder(IMyTerminalBlock block, ITerminalAction action, List<TerminalActionParameter> given)
        {
            _block = block;
            _action = action;
            _given = given;
        }

        /// <inheritdoc />
        public override void Give()
        {
            if (_given == null)
                _action.Apply(_block);
            else
                _action.Apply(_block, new ListReader<TerminalActionParameter>(_given));
        }
    }
}
