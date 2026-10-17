using System;
using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    partial class DefensiveCombat
    {
        /// <summary>A defensive block being listened to, until disposed.</summary>
        sealed class Listening : IDisposable
        {
            readonly IMyDefensiveCombatBlock _block;
            readonly Action _told;

            public Listening(IMyDefensiveCombatBlock block, Action told)
            {
                _block = block;
                _told = told;
                _block.IsFleeingChanged += Changed;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _block.IsFleeingChanged -= Changed;
            }

            void Changed(IMyDefensiveCombatBlock block, bool fleeing)
            {
                _told();
            }
        }
    }
}
