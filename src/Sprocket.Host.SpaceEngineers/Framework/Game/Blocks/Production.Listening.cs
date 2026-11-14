using System;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    partial class Production
    {
        /// <summary>A production block being listened to, until disposed.</summary>
        sealed class Listening : IDisposable
        {
            readonly IMyProductionBlock _block;
            readonly IMyAssembler _assembler;
            readonly IMyInventory _input;
            readonly IMyInventory _output;
            readonly Action _told;

            public Listening(IMyProductionBlock block, Action told)
            {
                _block = block;
                _assembler = block as IMyAssembler;
                _input = block.InputInventory;
                _output = block.OutputInventory;
                _told = told;

                _block.StartedProducing += Changed;
                _block.StoppedProducing += Changed;
                _block.IsWorkingChanged += Worked;
                if (_input != null)
                    _input.OnVolumeChanged += Filled;
                if (_output != null)
                    _output.OnVolumeChanged += Filled;

                if (_assembler == null)
                    return;

                _assembler.CurrentProgressChanged += Assembled;
                _assembler.CurrentStateChanged += Assembled;
                _assembler.CurrentModeChanged += Assembled;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _block.StartedProducing -= Changed;
                _block.StoppedProducing -= Changed;
                _block.IsWorkingChanged -= Worked;
                if (_input != null)
                    _input.OnVolumeChanged -= Filled;
                if (_output != null)
                    _output.OnVolumeChanged -= Filled;

                if (_assembler == null)
                    return;

                _assembler.CurrentProgressChanged -= Assembled;
                _assembler.CurrentStateChanged -= Assembled;
                _assembler.CurrentModeChanged -= Assembled;
            }

            void Changed()
            {
                _told();
            }

            void Worked(IMyCubeBlock block)
            {
                _told();
            }

            void Filled(IMyInventory inventory, float before, float after)
            {
                _told();
            }

            void Assembled(IMyAssembler assembler)
            {
                _told();
            }
        }
    }
}
