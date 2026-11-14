using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    /// <summary>A heat vent: a glow that shades between two colors as the grid's power use rises, automated like a light.</summary>
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
            kind.Renaming("ColorMin", true, "color at minimal load")
                .Renaming("ColorMax", true, "color at maximal load")
                .Renaming("PowerDependency", true, "power dependency")
                .Renaming("Radius", true, "radius")
                .Measuring("Radius", " m")
                .Renaming("Intensity", true, "intensity")
                .Renaming("Falloff", true, "falloff")
                .Renaming("Offset", true, "offset")
                .Measuring("Offset", " m");
        }
    }
}
