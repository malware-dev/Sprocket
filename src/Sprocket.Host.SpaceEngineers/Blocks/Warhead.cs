using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A warhead: armed or not, its countdown, and whether it is counting down.</summary>
    /// <remarks>
    /// The game's own detonate is the one offered, since it does nothing unless the warhead is armed. At the end of a
    /// countdown the game explodes it armed or not, and it counts its countdown setting down in place. Armed is a state of
    /// the warhead, read and never set: the game's own pair of actions for its switch arm and disarm it.
    /// </remarks>
    public sealed class Warhead : Capability
    {
        // The warhead's properties-changed event comes on every frame of a countdown, so it says nothing worth hearing.
        const string Uncounted = "A warhead raises no event that says its countdown has started or stopped.";
        const string Unarmed = "A warhead raises no event that says it was armed or disarmed.";

        static readonly Being Counting = Being.Flag(Reading.Of<IMyWarhead>(warhead => warhead.IsCountingDown));
        static readonly Being Armed = Being.Flag(Reading.Of<IMyWarhead>(warhead => warhead.IsArmed));

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
                .WithValue("armed", "armed", Armed).Status().Looked(Unarmed)
                .Renaming("Safety_On", false, "arm")
                .Renaming("Safety_Off", false, "disarm")
                .Hiding("Safety")
                .Renaming("DetonationTime", true, "detonation time")
                .Measuring("DetonationTime", " s");
        }
    }
}
