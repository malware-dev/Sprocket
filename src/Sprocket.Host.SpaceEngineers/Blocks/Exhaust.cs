using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>An exhaust pipe: smoke or flame that follows how much power the grid uses.</summary>
    /// <remarks>
    /// Switching it on and off is every functional block's. Its own start and stop of the effects act only on the machine
    /// that calls them, never on a dedicated server, and the game starts them again itself, so neither is offered.
    /// </remarks>
    public sealed class Exhaust : Capability
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
