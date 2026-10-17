using System;
using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    partial class PathRecorder
    {
        /// <summary>A recorder being listened to, until disposed.</summary>
        sealed class Listening : IDisposable
        {
            readonly IMyPathRecorderComponent _recorder;
            readonly Action _told;

            public Listening(IMyPathRecorderComponent recorder, Action told)
            {
                _recorder = recorder;
                _told = told;
                _recorder.IsPlayingChanged += Changed;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _recorder.IsPlayingChanged -= Changed;
            }

            void Changed(IMyPathRecorderComponent recorder)
            {
                _told();
            }
        }
    }
}
