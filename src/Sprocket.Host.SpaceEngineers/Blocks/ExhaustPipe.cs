using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>An exhaust pipe: smoke or flame that follows how much power the grid uses.</summary>
    public sealed class ExhaustPipe : Capability
    {
        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyExhaustBlock;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.Renaming("EffectsCombo", true, "effect")
                .Renaming("PowerDependency", true, "power dependency");
        }
    }
}
