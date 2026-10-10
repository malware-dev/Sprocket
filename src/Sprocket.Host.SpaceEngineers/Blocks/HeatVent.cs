using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;
using SpaceEngineers.Game.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A heat vent: a glow that shades between two colors as the grid's power use rises, automated like a light.</summary>
    /// <remarks>
    /// The glow it is drawing now is not offered: the game never writes its power level, and overwrites its current color
    /// from the two each time it updates the glow.
    /// </remarks>
    public sealed class HeatVent : Capability
    {
        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyHeatVent;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.Renaming("ColorMin", true, "color at low load")
                .Renaming("ColorMax", true, "color at full load")
                .Renaming("PowerDependency", true, "power dependency")
                .Renaming("Radius", true, "radius")
                .Renaming("Intensity", true, "intensity")
                .Renaming("Falloff", true, "falloff")
                .Renaming("Offset", true, "offset");
        }
    }
}
