using System;
using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    partial class Piston
    {
        /// <summary>A piston being listened to, until disposed.</summary>
        sealed class Listening : IDisposable
        {
            readonly IMyPistonBase _piston;
            readonly Action _told;

            public Listening(IMyPistonBase piston, Action told)
            {
                _piston = piston;
                _told = told;
                _piston.LimitReached += Reached;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _piston.LimitReached -= Reached;
            }

            void Reached(bool upper)
            {
                _told();
            }
        }
    }
}
