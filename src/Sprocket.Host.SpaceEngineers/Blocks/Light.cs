using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>
    /// A light: the interior light, the spotlight and the rotating warning light, and the searchlight, each a sequence colors,
    /// dims and makes blink.
    /// </summary>
    /// <remarks>
    /// Every one of them is the block's own values in Sprocket's words. Blinking has no switch of its own: a blink interval
    /// of 0 is a steady light, so the one value starts and stops a warning.
    /// </remarks>
    public sealed class Light : Capability
    {
        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyLightingBlock || block is IMySearchlight;
        }

        /// <inheritdoc />
        /// <remarks>
        /// Only an interior light has a falloff: a spotlight and a searchlight carry the value and do nothing with it, and their
        /// own terminal hides it. Only the two rotating warning lights turn; every other spotlight carries a rotation speed it
        /// hides as well.
        /// </remarks>
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.Renaming("Color", true, "color")
                .Renaming("Intensity", true, "intensity")
                .Renaming("Radius", true, "radius")
                .Renaming("Offset", true, "offset")
                .Renaming("Blink Interval", true, "blink interval")
                .Renaming("Blink Lenght", true, "blink length")
                .Renaming("Blink Offset", true, "blink offset");

            if (block is IMyInteriorLight)
                kind.Renaming("Falloff", true, "falloff");
            else
                kind.Hiding("Falloff", "IncreaseFalloff", "DecreaseFalloff");

            if (!(block is IMyReflectorLight))
                return;

            if (Turns(block))
                kind.Renaming("RotationSpeed", true, "rotation speed");
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
