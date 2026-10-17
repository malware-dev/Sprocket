using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;
using SpaceEngineers.Game.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A jukebox: music to turn up or down, which its switch pauses and resumes.</summary>
    public sealed class Jukebox : Capability
    {
        // The jukebox's block type, from the game's object builders.
        const string Type = "MyObjectBuilder_Jukebox";

        /// <summary>Whether <paramref name="block" /> is a jukebox, which its definition alone says.</summary>
        public static bool Is(IMyTerminalBlock block)
        {
            return block.BlockDefinition.TypeIdString == Type;
        }

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMySoundBlock && Is(block);
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.Hiding("PlaySound", "StopSound", "LoopableSlider", "IncreaseLoopableSlider", "DecreaseLoopableSlider")
                .Renaming("VolumeSlider", true, "volume")
                .Measuring("VolumeSlider", " %", 100)
                .Renaming("RangeSlider", true, "range")
                .Measuring("RangeSlider", " m");
        }
    }
}
