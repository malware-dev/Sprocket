using System;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Mods
{
    /// <summary>One listener to a block's mod members, which stops listening when disposed.</summary>
    public sealed class Listening : IDisposable
    {
        readonly Dictionary<long, List<Action>> _listening;
        readonly long _block;
        Action _told;

        /// <summary>The listener <paramref name="told" /> to the block <paramref name="block" />, held in <paramref name="listening" />.</summary>
        public Listening(Dictionary<long, List<Action>> listening, long block, Action told)
        {
            _listening = listening;
            _block = block;
            _told = told;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_told == null)
                return;

            List<Action> listeners;
            if (_listening.TryGetValue(_block, out listeners))
            {
                listeners.Remove(_told);
                if (listeners.Count == 0)
                    _listening.Remove(_block);
            }

            _told = null;
        }
    }
}
