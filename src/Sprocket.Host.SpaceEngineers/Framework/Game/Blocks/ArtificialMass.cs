using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    /// <summary>An artificial mass, which artificial gravity pulls on while it is switched on: read for how heavy it is.</summary>
    public sealed class ArtificialMass : Capability
    {
        static readonly OwnValue Mass = OwnValue.Real<IMyArtificialMassBlock>(block => block.VirtualMass);

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyArtificialMassBlock;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithValue("mass", "mass", Mass).Measured(" kg").Announced().In(Category.Behavior);
        }
    }
}
