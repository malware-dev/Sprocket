using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;
using SpaceEngineers.Game.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A jukebox: music to turn up or down, which its switch pauses and resumes.</summary>
    /// <remarks>
    /// The game gives it no interface of its own: to a mod it is a sound block, and only its definition says otherwise. Its
    /// playlist, and whether it is playing, live on its own class, which no mod can reach, so turning it up and down is
    /// all it offers of its own.
    /// </remarks>
    public sealed class Jukebox : Capability
    {
        /// <summary>The jukebox's block type, from the game's object builders.</summary>
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
        /// <remarks>
        /// The game hides playing and stopping a single sound from a jukebox's own terminal, since they work outside the
        /// playlist, and its loop time does nothing: it plays each track once.
        /// </remarks>
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.Hiding("PlaySound", "StopSound", "LoopableSlider", "IncreaseLoopableSlider", "DecreaseLoopableSlider")
                .Renaming("VolumeSlider", true, "volume")
                .Renaming("RangeSlider", true, "range");
        }
    }
}
