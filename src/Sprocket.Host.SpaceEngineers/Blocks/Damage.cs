using System;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;
using VRage.Game.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>How whole every block is: its integrity, any damage, whether it is broken, and how far it is built.</summary>
    /// <remarks>
    /// Broken is the block's own flag rather than a mark on integrity: the line is each block's critical component, which a
    /// player cannot see.
    /// </remarks>
    public sealed class Damage : Capability
    {
        static readonly Reading Hurt = Reading.Of<IMyTerminalBlock>(block => block.SlimBlock.CurrentDamage > 0);

        static readonly Reading Lost = Reading.Of<IMyTerminalBlock>(block => !block.IsFunctional);

        static readonly Being Integrity = Being.Number<IMyTerminalBlock>(block => Whole(block.SlimBlock));

        static readonly Being Built = Being.Number<IMyTerminalBlock>(block => block.SlimBlock.BuildLevelRatio * 100d);

        static readonly Being Damaged = Being.Flag(Hurt);

        static readonly Being Broken = Being.Flag(Lost);

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return true;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithValue("integrity", "integrity", Integrity).Announced()
                .WithValue("damaged", "damaged", Damaged).Announced()
                .WithValue("broken", "broken", Broken).Announced()
                .WithChange("breaks", "breaks", "broken", Literal.Of(true))
                .Reading("is broken", "is not broken").Announced()
                .WithChange("gets-repaired", "gets repaired", "broken", Literal.Of(false))
                .Reading("is not broken", "is broken").Announced()
                .WithValue("build-level", "build level", Built).Announced();
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
        /// <remarks>
        /// One subscription per block watched, so every hit or weld on a grid calls each watched block on it once, which
        /// compares one reference and returns. Moved to the grid the block is on as it changes grid, as a split moves it.
        /// </remarks>
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
