using Sprocket.Host.SpaceEngineers.Framework.Game.Grids;
using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Members
{
    /// <summary>An action with the options one step gave it.</summary>
    public sealed class GivenOwnAction<T, TOptions> : BoundAction where T : class
    {
        readonly TypedOwnAction<T, TOptions> _doing;
        readonly TOptions _options;

        /// <summary>The action <paramref name="doing" /> with <paramref name="options" />.</summary>
        public GivenOwnAction(TypedOwnAction<T, TOptions> doing, TOptions options)
        {
            _doing = doing;
            _options = options;
        }

        /// <inheritdoc />
        public override bool Acts => _doing.Does != null;

        /// <inheritdoc />
        public override bool IsForced => _doing.Forced != null && _doing.Forced(_options);

        /// <inheritdoc />
        public override void Do(IMyTerminalBlock block, ReaderMemory seen)
        {
            if (_doing.Does != null)
                _doing.Does((T)block, _options, seen);
        }

        /// <inheritdoc />
        public override bool Done(IMyTerminalBlock block, ReaderMemory seen)
        {
            return _doing.Finished != null && _doing.Finished((T)block, _options, seen);
        }

        /// <inheritdoc />
        public override BlockTrouble Trouble(IMyTerminalBlock block, ReaderMemory seen)
        {
            return IsForced ? BlockTrouble.None : Said(block, seen);
        }

        /// <inheritdoc />
        public override bool Strays(IMyTerminalBlock block, ReaderMemory seen)
        {
            return Said(block, seen).Exists;
        }

        BlockTrouble Said(IMyTerminalBlock block, ReaderMemory seen)
        {
            return _doing.Troubled?.Invoke((T)block, _options, seen) ?? BlockTrouble.None;
        }

        /// <inheritdoc />
        public override void Stop(IMyTerminalBlock block)
        {
            if (_doing.Stops != null)
                _doing.Stops((T)block);
        }

        /// <inheritdoc />
        public override int Expected(IMyTerminalBlock block)
        {
            return _doing.Takes?.Invoke((T)block, _options) ?? GridEntity.Unestimated;
        }
    }
}
