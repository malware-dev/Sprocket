using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>An artificial mass, which artificial gravity pulls on while it is switched on: read for how heavy it is.</summary>
    /// <remarks>
    /// Its switch is its control. The space ball is a mass whose weight can be set, and is a block of its own; see
    /// <see cref="SpaceBall" />.
    /// </remarks>
    public sealed class ArtificialMass : Capability
    {
        static readonly OwnValue Mass = OwnValue.Number<IMyArtificialMassBlock>(block => block.VirtualMass);

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyArtificialMassBlock;
        }

        /// <inheritdoc />
        /// <remarks>Its mass is fixed by the block, so nothing need announce it.</remarks>
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithValue("mass", "mass", Mass).Measured(" kg").Announced().In(Category.Behaviour);
        }
    }
}
