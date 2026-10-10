using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A gyroscope: taken from the pilot or handed back, and how strongly it turns the ship.</summary>
    /// <remarks>
    /// The terminal's yaw, pitch and roll are in the gyroscope's own axes, which turn the ship a different way for every way
    /// a gyroscope is placed, and are left out.
    /// </remarks>
    public sealed class Gyroscope : Capability
    {
        static readonly Doing Releases = Doing.Of<IMyGyro>(gyro => gyro.GyroOverride = false);

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyGyro;
        }

        /// <inheritdoc />
        /// <remarks>
        /// Release does what the game's <c>Override_Off</c> does without naming it: an entry of Sprocket's that names one half
        /// of a switch's pair hides the switch itself, and overridden is listed. The game's pair is hidden behind that value.
        /// </remarks>
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithAction("release", "release", Releases, (string)null).In(Category.Flight)
                .Renaming("Override", true, "overridden")
                .Renaming("Override", false, "toggle override")
                .Renaming("Power", true, "strength")
                .Hiding("Yaw", "Pitch", "Roll", "SetYaw", "SetPitch", "SetRoll", "IncreaseYaw", "DecreaseYaw",
                    "IncreasePitch", "DecreasePitch", "IncreaseRoll", "DecreaseRoll");
        }
    }
}
