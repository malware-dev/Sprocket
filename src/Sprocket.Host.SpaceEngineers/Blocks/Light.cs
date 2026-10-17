using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>An interior light, spotlight, rotating warning light or searchlight, which a sequence colors, dims and makes blink.</summary>
    public sealed class Light : Capability
    {
        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyLightingBlock || block is IMySearchlight;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.Renaming("Color", true, "color")
                .Renaming("Intensity", true, "intensity")
                .Renaming("Radius", true, "radius")
                .Measuring("Radius", " m")
                .Renaming("Offset", true, "offset")
                .Measuring("Offset", " m")
                .Renaming("Blink Interval", true, "blink interval")
                .Measuring("Blink Interval", " s")
                .Renaming("Blink Lenght", true, "blink length")
                .Measuring("Blink Lenght", " %")
                .Renaming("Blink Offset", true, "blink offset")
                .Measuring("Blink Offset", " %");

            if (block is IMyInteriorLight)
                kind.Renaming("Falloff", true, "falloff");
            else
                kind.Hiding("Falloff", "IncreaseFalloff", "DecreaseFalloff");

            if (!(block is IMyReflectorLight))
                return;

            if (Turns(block))
                kind.Renaming("RotationSpeed", true, "rotation speed")
                    .Measuring("RotationSpeed", " rpm");
            else
                kind.Hiding("RotationSpeed", "IncreaseRotationSpeed", "DecreaseRotationSpeed");
        }

        /// <summary>Whether the spotlight is one of the two rotating warning lights, the only ones with a part that turns.</summary>
        /// <remarks>The subtypes of the game's Decorative Pack 2.</remarks>
        static bool Turns(IMyTerminalBlock block)
        {
            var subtype = block.BlockDefinition.SubtypeName;
            return subtype == "RotatingLightLarge" || subtype == "RotatingLightSmall";
        }
    }
}
