using System;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;
using VRage.Game.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>How whole every block is: its integrity, any damage, and how far it is built.</summary>
    public sealed partial class BlockIntegrity : Capability
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
                .WithValue("build_level", "build level", Built).Percent().Announced().In(Category.Repair);
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
    }
}
