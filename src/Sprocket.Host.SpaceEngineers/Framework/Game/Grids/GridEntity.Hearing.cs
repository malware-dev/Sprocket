using System;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Grids
{
    partial class GridEntity
    {
        /// <summary>A listener to this block, until disposed.</summary>
        sealed class Hearing : IDisposable
        {
            readonly GridEntity _entity;
            readonly Action _told;
            readonly List<IDisposable> _listening;

            public Hearing(GridEntity entity, Action told, List<IDisposable> listening)
            {
                _entity = entity;
                _told = told;
                _listening = listening;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _entity._hearing.Remove(_told);
                foreach (var listener in _listening)
                    listener.Dispose();
            }
        }
    }
}
