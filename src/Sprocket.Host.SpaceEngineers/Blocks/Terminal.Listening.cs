using System;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    partial class Terminal
    {
        /// <summary>A block being listened to, until disposed.</summary>
        sealed class Listening : IDisposable
        {
            readonly IMyTerminalBlock _block;
            readonly Action _told;

            public Listening(IMyTerminalBlock block, Action told)
            {
                _block = block;
                _told = told;
                _block.IsWorkingChanged += Turned;
                _block.CustomDataChanged += Written;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _block.IsWorkingChanged -= Turned;
                _block.CustomDataChanged -= Written;
            }

            void Turned(IMyCubeBlock block)
            {
                _told();
            }

            void Written(IMyTerminalBlock block)
            {
                _told();
            }
        }
    }
}
