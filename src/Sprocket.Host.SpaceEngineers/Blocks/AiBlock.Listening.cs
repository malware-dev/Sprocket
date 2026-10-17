using System;
using Sandbox.Game.EntityComponents;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    partial class AiBlock
    {
        /// <summary>An AI component being listened to, until disposed.</summary>
        sealed class Listening : IDisposable
        {
            readonly MyAiBlockComponent _ai;
            readonly Action _told;

            public Listening(MyAiBlockComponent ai, Action told)
            {
                _ai = ai;
                _told = told;
                _ai.OnActivatedChanged += told;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _ai.OnActivatedChanged -= _told;
            }
        }
    }
}
