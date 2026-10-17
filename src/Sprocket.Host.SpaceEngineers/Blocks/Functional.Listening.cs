using System;
using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    partial class Functional
    {
        /// <summary>A switch being listened to, until disposed.</summary>
        sealed class Listening : IDisposable
        {
            readonly IMyFunctionalBlock _block;
            readonly Action _told;

            public Listening(IMyFunctionalBlock block, Action told)
            {
                _block = block;
                _told = told;
                _block.EnabledChanged += Switched;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _block.EnabledChanged -= Switched;
            }

            void Switched(IMyTerminalBlock block)
            {
                _told();
            }
        }
    }
}
