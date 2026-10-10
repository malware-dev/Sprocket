using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A warhead: armed or not, its countdown, and whether it is counting down.</summary>
    /// <remarks>
    /// The game's own detonate is the one offered, since it does nothing unless the warhead is armed. At the end of a
    /// countdown the game explodes it armed or not, and it counts its countdown setting down in place.
    /// </remarks>
    public sealed class Warhead : Capability
    {
        // The warhead's properties-changed event comes on every frame of a countdown, so it says nothing worth hearing.
        const string Uncounted = "A warhead raises no event that says its countdown has started or stopped.";

        static readonly Being Counting = Being.Flag(Reading.Of<IMyWarhead>(warhead => warhead.IsCountingDown));

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyWarhead;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithValue("counting-down", "counting down", Counting).Looked(Uncounted)
                .Renaming("StartCountdown", false, "start countdown")
                .Renaming("StopCountdown", false, "stop countdown")
                .Renaming("Detonate", false, "detonate")
                .Renaming("Safety", true, "armed")
                .Renaming("Safety", false, "toggle armed")
                .Renaming("DetonationTime", true, "countdown");
        }
    }
}
