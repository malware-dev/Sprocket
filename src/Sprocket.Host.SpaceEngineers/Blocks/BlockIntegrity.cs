using System;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;
using VRage.Game.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>How whole every block is: its integrity, any damage, and how far it is built.</summary>
    public sealed class BlockIntegrity : Capability
    {
        static readonly Reading Hurt = Reading.Of<IMyTerminalBlock>(block => block.SlimBlock.CurrentDamage > 0);

        static readonly OwnValue Integrity = OwnValue.Number<IMyTerminalBlock>(block => Whole(block.SlimBlock));

        static readonly OwnValue Built = OwnValue.Number<IMyTerminalBlock>(block => block.SlimBlock.BuildLevelRatio * 100d);

        static readonly OwnValue Damaged = OwnValue.Flag(Hurt);

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return true;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithValue("integrity", "integrity", Integrity).Percent().Announced().In(Category.Repair)
                .WithValue("damaged", "damaged", Damaged).Announced().In(Category.Repair)
                .WithValue("build-level", "build level", Built).Percent().Announced().In(Category.Repair);
        }

        /// <summary>Told as the block is hit, welded or ground, which its grid announces for every block on it.</summary>
        public override IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            return new Listening(block, told);
        }

        /// <summary>The block's integrity as the HUD gives it, from 0 to 100.</summary>
        static double Whole(IMySlimBlock slim)
        {
            return slim.MaxIntegrity <= 0f ? 0d : (slim.BuildIntegrity - slim.CurrentDamage) / slim.MaxIntegrity * 100d;
        }

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
