using System;
using Sandbox.ModAPI;
using VRage.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    partial class CombatBlock
    {
        /// <summary>A search being listened to, until disposed.</summary>
        sealed class Listening : IDisposable
        {
            readonly IMySearchEnemyComponent _search;
            readonly Action _told;

            public Listening(IMySearchEnemyComponent search, Action told)
            {
                _search = search;
                _told = told;
                _search.TargetChanged += Changed;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _search.TargetChanged -= Changed;
            }

            void Changed(IMyEntity was, IMyEntity now, bool locked)
            {
                _told();
            }
        }
    }
}
