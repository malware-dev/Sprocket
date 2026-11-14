using System;
using SpaceEngineers.Game.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    partial class MergeBlock
    {
        /// <summary>A merge block being listened to, until disposed.</summary>
        sealed class Listening : IDisposable
        {
            readonly IMyShipMergeBlock _merge;
            readonly Action _told;

            public Listening(IMyShipMergeBlock merge, Action told)
            {
                _merge = merge;
                _told = told;
                _merge.MergeStateChanged += Changed;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _merge.MergeStateChanged -= Changed;
            }

            void Changed(IMyShipMergeBlock merge)
            {
                _told();
            }
        }
    }
}
