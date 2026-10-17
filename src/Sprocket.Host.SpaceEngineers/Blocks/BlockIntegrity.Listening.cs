using System;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    partial class BlockIntegrity
    {
        /// <summary>A block's grid being listened to for that block, until disposed.</summary>
        /// <remarks>Every hit or weld on a grid calls each watched block on it once, at the cost of one reference comparison.</remarks>
        sealed class Listening : IDisposable
        {
            readonly IMyTerminalBlock _block;
            readonly Action _told;
            IMyCubeGrid _grid;

            public Listening(IMyTerminalBlock block, Action told)
            {
                _block = block;
                _told = told;
                _block.CubeGridChanged += Moved;
                Hear(_block.CubeGrid);
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _block.CubeGridChanged -= Moved;
                Hear(null);
            }

            void Hear(IMyCubeGrid grid)
            {
                if (_grid != null)
                {
                    _grid.OnBlockIntegrityChanged -= Changed;
                    _grid.OnGridBlockDamaged -= Hit;
                }

                _grid = grid;
                if (_grid == null)
                    return;

                _grid.OnBlockIntegrityChanged += Changed;
                _grid.OnGridBlockDamaged += Hit;
            }

            void Moved(IMyCubeGrid grid)
            {
                Hear(_block.CubeGrid);
                _told();
            }

            void Changed(IMySlimBlock slim)
            {
                if (ReferenceEquals(slim.FatBlock, _block))
                    _told();
            }

            void Hit(IMySlimBlock slim, float damage, MyHitInfo? hit, long attacker)
            {
                if (ReferenceEquals(slim.FatBlock, _block))
                    _told();
            }
        }
    }
}
