using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    /// <summary>A gyroscope: taken from the pilot or handed back, and how strongly it turns the ship.</summary>
    public sealed class Gyroscope : Capability
    {
        static readonly OwnAction Releases = OwnAction.Of<IMyGyro>(gyro => gyro.GyroOverride = false);

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyGyro;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithAction("release", "release", Releases, (string)null).In(Category.Flight)
                .Renaming("Override", true, "overriding").Stating("Override")
                .Renaming("Override", false, "toggle overriding")
                .Renaming("Power", true, "strength")
                .Measuring("Power", " %", 100)
                .Hiding("Yaw", "Pitch", "Roll", "SetYaw", "SetPitch", "SetRoll", "IncreaseYaw", "DecreaseYaw",
                    "IncreasePitch", "DecreasePitch", "IncreaseRoll", "DecreaseRoll");
        }
    }
}
